using Godot;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The exchange's counters on our scene (owner, 2026-10-16, variant A of four mock-ups; docs/ui/battle-screen.md,
/// "The exchange's counters"), after Sengoku Rance's (docs/sengoku/04-screen.md §5, §9.5): each side's troops and HP
/// at the scene's foot, the player's at the left and the enemy's at the right, rising as the shots start, counting
/// down in Sengoku Rance's steps — every <see cref="Step"/> the value due a step later — with each digit that changes
/// falling away, holding the result and sinking when the blow is over. The digits are drawn in white and black into a
/// viewport of their own and coloured there by a shader, white to gold for the troops and white to mint for the HP.
/// </summary>
internal sealed partial class ExchangeCountersView : Control
{
    /// <summary>Sengoku Rance's beats: the rise and the sink, the first step's delay, a step (seconds).</summary>
    public const double Rise = 0.25, Sink = 0.25, Delay = 0.1, Step = 0.2;

    /// <summary>How long the counters run (Option → Battle, 1.3 s by default).</summary>
    public static double RunSeconds => BattleScreenSettings.Get(BattleScreenSettings.CountSeconds);

    // The layout in layout pixels of the 1024×410 scene: each side's counters at its foot, straight on the field (owner,
    // 2026-10-16: no band behind them), TROOPS over the troops' digits, HP and its digits beside them towards the middle;
    // above the order strip, which stands at the scene's foot (owner, 2026-10-16).
    private const float Edge = 12, Pad = 16, Foot = 36, Gap = 18;
    // Oswald (owner, 2026-10-16, of sixteen fonts): its digits stand 0.83 of the size, so 51 for the troops' 42-pixel digits.
    private const int TroopSize = 51, HpSize = 30, LabelSize = 14;
    private const float TroopCell = 30, HpCell = 19, TroopCap = 42, HpCap = 25, LabelGap = 9;
    private const float FlyDrop = 1.4f, FlyDrift = 10, FlyDriftMore = 14;

    private static readonly Color LabelGold = new(0.886f, 0.722f, 0.298f), LabelMint = new(0.62f, 0.86f, 0.62f);

    /// <summary>A digit's fill in the digits' viewport: red becomes the troops' gold, blue the HP's mint.</summary>
    private static readonly Color TroopInk = new(1, 0, 0), HpInk = new(0, 0, 1);

    private const string ShaderCode = """
        shader_type canvas_item;
        uniform vec2 troops_band;       // the troops' digits' top and baseline, in UV
        uniform vec2 hp_band;
        vec3 ramp(float t, vec3 a, vec3 b, vec3 c) {
            return t < 0.15 ? a : t < 0.62 ? mix(a, b, (t - 0.15) / 0.47) : mix(b, c, (t - 0.62) / 0.38);
        }
        void fragment() {
            vec4 c = texture(TEXTURE, UV);
            vec3 ink = c.a > 0.0 ? min(c.rgb / c.a, vec3(1.0)) : vec3(0.0);
            float tt = clamp((UV.y - troops_band.x) / (troops_band.y - troops_band.x), 0.0, 1.0);
            float th = clamp((UV.y - hp_band.x) / (hp_band.y - hp_band.x), 0.0, 1.0);
            vec3 gold = ramp(tt, vec3(1.0, 0.992, 0.949), vec3(0.953, 0.804, 0.384), vec3(0.659, 0.439, 0.102));
            vec3 mint = ramp(th, vec3(0.957, 1.0, 0.957), vec3(0.561, 0.863, 0.561), vec3(0.184, 0.478, 0.227));
            COLOR = vec4(ink.r * gold + ink.b * mint, c.a);
        }
        """;

    private readonly Font _digitsFont, _labelFont;
    private readonly Control _labels;
    private readonly SubViewport _port;
    private readonly Control _digits;
    private readonly TextureRect _picture;
    private readonly ShaderMaterial _material;
    private float _scale = 1;

    // The phone held landscape (docs/ui/phone.md, §17, LB2x): the counters leave the scene's picture for its blurred sides,
    // each beside its army — ours right-aligned left of the picture, the enemy's left of its right edge —, their figures
    // 5.9 mm high, smaller together when a side's would not fit its room.
    private (float Left, float Right)? _sharp;  // the picture's edges in the band (layout pixels); null off the phone
    private float _k = 1, _labelK = 1;          // the digits' and the labels' sizes over the PC's

    private ExchangeCounters? _values;
    private int _seen = -1;
    private double _time;                       // seconds since the shots started
    private double _sinking = -1;               // seconds since the blow ended; -1 while it stands
    private bool _instant;
    private readonly Dictionary<(bool Player, bool Troops), int> _shown = [];
    private readonly List<Flyer> _flyers = [];
    private readonly Random _random = new(1);

    /// <summary>A digit falling away: drops, drifts outward, turns half a turn and fades (Sengoku Rance's, 0.2 s).</summary>
    private sealed record Flyer(string Digit, Vector2 At, bool Troops, float Outward, float Drift, double Born);

    public ExchangeCountersView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        var oswald = new FontFile { Data = Godot.FileAccess.GetFileAsBytes("res://fonts/Oswald-VariableFont_wght.ttf") };
        _digitsFont = Weight(oswald, 700);
        _labelFont = Weight(oswald, 600, spacing: 3);
        _labels = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _labels.Draw += DrawLabels;
        AddChild(_labels);
        _port = new SubViewport { TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible };
        _digits = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _digits.Draw += DrawDigits;
        _port.AddChild(_digits);
        AddChild(_port);
        _material = new ShaderMaterial { Shader = new Shader { Code = ShaderCode } };
        _picture = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore, Texture = _port.GetTexture(), Material = _material,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,                 // its size is the scene's, not the port's texture's
        };
        AddChild(_picture);
    }

    private static FontVariation Weight(Font font, int weight, int spacing = 0)
    {
        return new FontVariation
        {
            BaseFont = font,
            SpacingGlyph = spacing,
            VariationOpentype = new Godot.Collections.Dictionary
            {
                [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = weight,
            },
        };
    }

    /// <summary>
    /// Lays the counters over the scene, <paramref name="scale"/> window pixels to a layout pixel; on the phone over the
    /// scene's whole band with the picture's edges <paramref name="sharp"/> in it (layout pixels).
    /// </summary>
    public void Layout(Vector2 size, float scale, (float Left, float Right)? sharp = null)
    {
        _scale = scale;
        _sharp = sharp;
        if (sharp is null)
            (_k, _labelK) = (1, 1);
        else if (_values is not null)
            FitPhone(_values);                                      // a turn to landscape mid-exchange: the sides' sizes now
        Size = size;
        _labels.Size = size;
        _port.Size = new Vector2I(Math.Max(1, (int)Math.Ceiling(size.X)), Math.Max(1, (int)Math.Ceiling(size.Y)));
        _picture.Size = size;
        _digits.Size = _port.Size;
        Redraw();
    }

    /// <summary>
    /// The exchange's counters now (<see cref="BattlePresenter.ExchangeCounters"/>) and how many exchanges have started
    /// (<see cref="BattlePresenter.EffectsStarted"/>): a new start raises them, their end sinks them;
    /// <paramref name="instant"/> (skipping, <c>--fast</c>): the result at once, no rise, no falling digits.
    /// </summary>
    public void Show(ExchangeCounters? counters, int started, bool instant)
    {
        if (counters is not null && started != _seen)
        {
            (_values, _seen, _time, _sinking, _instant) = (counters, started, 0, -1, instant);
            if (_sharp is not null)
                FitPhone(counters);
            _shown.Clear();
            _flyers.Clear();
            Visible = true;
        }
        else if (counters is null && _values is not null && _sinking < 0)
        {
            _sinking = instant || _instant ? Sink : 0;
            _flyers.Clear();
        }
        if (instant)
            _instant = true;
    }

    public override void _Process(double delta)
    {
        if (_values is null)
            return;
        _time += delta;
        if (_sinking >= 0)
        {
            _sinking += delta;
            if (_sinking >= Sink)
            {
                (_values, Visible) = (null, false);
                return;
            }
        }
        Redraw();
    }

    /// <summary>A counter's value now: Sengoku Rance's steps, the result at once while instant or sinking.</summary>
    private int Value(ExchangeCount count)
    {
        if (_instant || _sinking >= 0 || count.From == count.To)
            return count.To;
        return (int)Math.Round(count.From + (count.To - count.From) * Share(_time));
    }

    /// <summary>
    /// How far the counters have run <paramref name="time"/> seconds after the shots started (0 to 1): from
    /// <see cref="Delay"/> on, every <see cref="Step"/> the share due a step later, over <see cref="RunSeconds"/>.
    /// The fighters' cards run by it too, so they show the same numbers.
    /// </summary>
    public static double Share(double time)
    {
        if (time < Delay)
            return 0;
        double run = RunSeconds;
        if (run <= 0)
            return 1;
        int k = (int)Math.Floor((time - Delay) / Step) + 1;
        return Math.Min(1, k * Step / run);
    }

    /// <summary>How far the counters stand up (0 below the foot, 1 in place).</summary>
    private float Up()
    {
        if (_instant)
            return _sinking >= 0 ? 0 : 1;
        static float Ease(double t) => 1 - (float)Math.Pow(1 - Math.Clamp(t, 0, 1), 3);
        float up = Ease(_time / Rise);
        return _sinking >= 0 ? Math.Min(up, 1 - Ease(_sinking / Sink)) : up;
    }

    /// <summary>One side's pieces in layout pixels: the labels' and digits' places, the baseline.</summary>
    private readonly record struct SideLayout(float TroopsX, int TroopCells, float HpLabelX, float HpX, int HpCells, float Baseline);

    private static int Digits(int value) => Math.Max(1, value.ToString().Length);

    /// <summary>The counters' baseline in the band, before they rise (layout pixels).</summary>
    private float Baseline => _sharp is null ? 410 - Foot - 10 : Size.Y / _scale - PhoneBattle.Layout(PhoneBattle.CounterFoot);

    /// <summary>A side's counters' width at the sizes now.</summary>
    private float Content(bool player, ExchangeCounters c)
    {
        var (troops, hp) = player ? (c.PlayerTroops, c.PlayerHp) : (c.EnemyTroops, c.EnemyHp);
        bool hasTroops = player ? c.PlayerHasTroops : c.EnemyHasTroops;
        float hpLabel = _labelFont.GetStringSize("HP", HorizontalAlignment.Left, -1, (int)Math.Round(LabelSize * _labelK)).X + 4;
        return (hasTroops ? Digits(troops.From) * TroopCell * _k + Gap * _k : 0) + hpLabel + Digits(hp.From) * HpCell * _k;
    }

    /// <summary>The phone's sizes for <paramref name="c"/>: LB2x's, smaller together when a side would not fit its room.</summary>
    private void FitPhone(ExchangeCounters c)
    {
        float left = _sharp!.Value.Left;
        _k = PhoneBattle.Layout(PhoneBattle.CounterTroops) / TroopSize;
        _labelK = PhoneBattle.Layout(PhoneBattle.CounterLabel) / LabelSize;
        float room = left - PhoneBattle.Layout(PhoneBattle.CounterGap + PhoneBattle.CounterEdge);
        float widest = Math.Max(Content(true, c), Content(false, c));
        if (widest > room)
            _k *= room / widest;
    }

    private SideLayout LayoutSide(bool player, ExchangeCounters c, float offset)
    {
        var (troops, hp) = player ? (c.PlayerTroops, c.PlayerHp) : (c.EnemyTroops, c.EnemyHp);
        bool hasTroops = player ? c.PlayerHasTroops : c.EnemyHasTroops;
        int troopCells = hasTroops ? Digits(troops.From) : 0, hpCells = Digits(hp.From);
        float hpLabel = _labelFont.GetStringSize("HP", HorizontalAlignment.Left, -1, (int)Math.Round(LabelSize * _labelK)).X + 4;
        float troopsW = troopCells * TroopCell * _k, hpW = hpLabel + hpCells * HpCell * _k;
        float content = troopsW + (hasTroops ? Gap * _k : 0) + hpW;
        float w = Size.X / _scale;
        float baseline = Baseline + offset;
        if (_sharp is var (sharpLeft, sharpRight) && sharpRight > sharpLeft)
        {
            float gap = PhoneBattle.Layout(PhoneBattle.CounterGap);
            if (player)                                                 // the troops, then the HP, ending left of the picture
            {
                float x = sharpLeft - gap - content;
                float hpX = x + troopsW + (hasTroops ? Gap * _k : 0);
                return new SideLayout(x, troopCells, hpX, hpX + hpLabel, hpCells, baseline);
            }
            float start = sharpRight + gap;                             // the HP, then the troops, right of the picture
            return new SideLayout(start + content - troopsW, troopCells, start, start + hpLabel, hpCells, baseline);
        }
        if (player)
        {
            float x = Edge + Pad;
            float hpX = x + troopsW + (hasTroops ? Gap * _k : 0);
            return new SideLayout(x, troopCells, hpX, hpX + hpLabel, hpCells, baseline);
        }
        float right = w - Edge - Pad;
        float hpStart = right - content;
        return new SideLayout(right - troopsW, troopCells, hpStart, hpStart + hpLabel, hpCells, baseline);
    }

    private void Redraw()
    {
        _labels.QueueRedraw();
        _digits.QueueRedraw();
        if (_values is null)
            return;
        float offset = (1 - Up()) * 130;
        float s = _scale, h = Math.Max(1, _port.Size.Y);
        float baseline = (Baseline + offset) * s;
        _material.SetShaderParameter("troops_band", new Vector2((baseline - TroopCap * _k * s) / h, baseline / h));
        _material.SetShaderParameter("hp_band", new Vector2((baseline - HpCap * _k * s) / h, baseline / h));
    }

    private void DrawLabels()
    {
        if (_values is not { } c)
            return;
        float s = _scale, offset = (1 - Up()) * 130;
        foreach (bool player in new[] { true, false })
        {
            var l = LayoutSide(player, c, offset);
            int ls = (int)Math.Round(LabelSize * _labelK * s);
            if (l.TroopCells > 0)
            {
                float labelY = (l.Baseline - (TroopCap + LabelGap) * _k) * s;
                float width = l.TroopCells * TroopCell * _k * s;
                Label(new Vector2(l.TroopsX * s, labelY), "TROOPS", player ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    width, ls, LabelGold);
            }
            Label(new Vector2(l.HpLabelX * s, (l.Baseline - 4) * s), "HP", HorizontalAlignment.Left, -1, ls, LabelMint);
        }
    }

    private void Label(Vector2 at, string text, HorizontalAlignment align, float width, int size, Color colour)
    {
        int outline = Math.Max(1, (int)Math.Round(3 * _scale));
        _labels.DrawStringOutline(_labelFont, at, text, align, width, size, outline, new Color(0, 0, 0, 0.85f));
        _labels.DrawString(_labelFont, at, text, align, width, size, colour);
    }

    private void DrawDigits()
    {
        if (_values is not { } c)
            return;
        float s = _scale, offset = (1 - Up()) * 130;
        bool falling = !_instant && _sinking < 0;
        foreach (bool player in new[] { true, false })
        {
            var l = LayoutSide(player, c, offset);
            var (troops, hp) = player ? (c.PlayerTroops, c.PlayerHp) : (c.EnemyTroops, c.EnemyHp);
            if (l.TroopCells > 0)
                Number(player, true, Value(troops), l.TroopsX, l.TroopCells, TroopCell * _k, TroopSize * _k, l.Baseline, offset, falling);
            Number(player, false, Value(hp), l.HpX, l.HpCells, HpCell * _k, HpSize * _k, l.Baseline, offset, falling);
        }
        // The digits falling away (Sengoku Rance's: 68 px down, ±20 out, half a turn, 0.2 s; ease in).
        _flyers.RemoveAll(f => _time - f.Born >= Step);
        foreach (var f in _flyers)
        {
            float p = (float)((_time - f.Born) / Step), e = p * p;
            float size = (f.Troops ? TroopSize : HpSize) * _k, cap = (f.Troops ? TroopCap : HpCap) * _k;
            var centre = (f.At + new Vector2(f.Outward * f.Drift * e, FlyDrop * cap * e + offset - cap / 2)) * s;
            _digits.DrawSetTransform(centre, f.Outward * Mathf.Pi * e);
            Digit(new Vector2(0, cap / 2 * s), f.Digit, (int)Math.Round(size * s), f.Troops ? TroopInk : HpInk, 1 - p);
        }
        _digits.DrawSetTransform(Vector2.Zero);
    }

    /// <summary>A counter's digits right-aligned in its cells; each digit that changes on the way down falls away.</summary>
    private void Number(bool player, bool troops, int value, float x, int cells, float cell, float size, float baseline, float offset,
        bool falling)
    {
        string text = value.ToString();
        var key = (player, troops);
        string? before = _shown.TryGetValue(key, out int old) ? old.ToString() : null;
        _shown[key] = value;
        float s = _scale;
        for (int i = 0; i < cells; i++)
        {
            int fromRight = cells - 1 - i;
            string digit = fromRight < text.Length ? text[text.Length - 1 - fromRight].ToString() : "";
            float cx = x + (i + 0.5f) * cell;
            if (falling && before is not null && value < old)
            {
                string was = fromRight < before.Length ? before[before.Length - 1 - fromRight].ToString() : "";
                if (was != "" && was != digit)
                    _flyers.Add(new Flyer(was, new Vector2(cx, baseline - offset), troops, player ? -1 : 1,
                        FlyDrift + (float)_random.NextDouble() * FlyDriftMore, _time));
            }
            if (digit != "")
                Digit(new Vector2(cx * s, baseline * s), digit, (int)Math.Round(size * s), troops ? TroopInk : HpInk, 1);
        }
    }

    /// <summary>One digit centred on <paramref name="at"/> (its baseline), with its outline and a shadow below.</summary>
    private void Digit(Vector2 at, string digit, int size, Color ink, float alpha)
    {
        float w = _digitsFont.GetStringSize(digit, HorizontalAlignment.Left, -1, size).X;
        var pos = at - new Vector2(w / 2, 0);
        int outline = Math.Max(2, (int)Math.Round(size * 0.09f));
        _digits.DrawStringOutline(_digitsFont, pos + new Vector2(0, size * 0.05f), digit, HorizontalAlignment.Left, -1, size,
            outline + 2, new Color(0, 0, 0, 0.6f * alpha));                                         // the shadow
        _digits.DrawStringOutline(_digitsFont, pos, digit, HorizontalAlignment.Left, -1, size, outline, new Color(0, 0, 0, alpha));
        _digits.DrawString(_digitsFont, pos, digit, HorizontalAlignment.Left, -1, size, new Color(ink, alpha));
    }
}
