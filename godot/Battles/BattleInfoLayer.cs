using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The battle's info boxes (<c>WAO_SUB</c> 143/153) on our battle screen (docs/ui/messages.md, §5 step 5; the owner's
/// mock-ups K3 and K1, 2026-10-18/19), drawn from <see cref="BattleInfoBox"/>: a defence's "DEF +n" badge flashing on the
/// unit's card with its sentence on a small plate; a fall's stamp across the dimmed card; the end as one results plate — the
/// reason as its title, each army's damages, the EXP, a row for each level-up —; every other box as our narration panel in
/// the scene's middle. The click's triangle while the player's key is awaited. It takes no input: the screen's click answers
/// the script's wait (<see cref="GameScreen"/>). Laid out in the 1280×800 layout's pixels, scaled with the screen.
/// </summary>
internal sealed partial class BattleInfoLayer : Control
{
    private const float SceneCentreX = 640, SceneMiddleY = 190, PanelMaxText = 860, PanelPitch = 40;
    private const int PanelSize = 30, PlateSize = 24;
    private const float PlateWidth = 620;

    // A defence's badge: it rises, holds, then fades (owner, 2026-10-19: answered at once, as the order's label flashes).
    private const double BadgeRise = 0.15, BadgeHold = 1.2, BadgeFade = 0.45;

    private static readonly Color Accent = Color.Color8(242, 199, 92);
    private static readonly Color BadgeFill = Color.Color8(52, 92, 150, 245);
    private static readonly Color StampInk = Color.Color8(206, 44, 32, 235);

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(500);
    private readonly FontVariation _semi = Lacquer.Cinzel(600);
    private readonly FontVariation _bold = Lacquer.Cinzel(700);
    private FontVariation _gothic = null!;
    private BattleInfoBox? _box;
    private bool _waiting;
    private (BattleInfoBox Box, ulong From)? _flash;
    private float _k = 1;

    public BattleInfoLayer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 3;                                                     // over the cards, the spotlight and the acting frame
    }

    /// <summary>Option → Fonts: the boxes' sentences in the game's font, bold as the surface's text.</summary>
    public void SetFont(Font font)
    {
        _gothic = new FontVariation { BaseFont = font, VariationEmbolden = 0.6f };
        QueueRedraw();
    }

    public void Layout(float scale)
    {
        _k = scale;
        QueueRedraw();
    }

    /// <summary>The box at its key wait (null: none, or one answered at once) and whether the player's key is awaited.</summary>
    public void Show(BattleInfoBox? box, bool waiting)
    {
        if (box is { AnsweredAtOnce: true })
            box = null;
        if (box == _box && waiting == _waiting)
            return;
        (_box, _waiting) = (box, waiting);
        if (box is not null)
            _flash = null;                                              // a box the player answers: no badge fading under it
        QueueRedraw();
    }

    /// <summary>A defence's badge flashes on its card (its wait is answered at once).</summary>
    public void Flash(BattleInfoBox defence)
    {
        _flash = (defence, Time.GetTicksMsec());
        QueueRedraw();
    }

    /// <summary>Whether a badge is still flashing.</summary>
    public bool Flashing => _flash is not null;

    public override void _Process(double delta)
    {
        if (_flash is { } f && (Time.GetTicksMsec() - f.From) / 1000.0 >= BadgeRise + BadgeHold + BadgeFade)
            _flash = null;
        if (_flash is not null || (_box is not null && _waiting))
            QueueRedraw();                                              // the badge fades, the triangle moves
        else if (_drawnFlash)
            QueueRedraw();
    }

    private bool _drawnFlash;

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        _drawnFlash = _flash is not null;
        if (_flash is { } flash && flash.Box.Place is { } at)
            DrawDefence(flash.Box, BattleBoardView.CardArea(at), Alpha(flash.From));
        switch (_box)
        {
            case { Kind: InfoBoxKind.Fall, Place: { } place }:
                DrawFall(_box, BattleBoardView.CardArea(place));
                break;
            case { Kind: InfoBoxKind.Results, Results: { } results }:
                DrawResults(results);
                break;
            case { Kind: InfoBoxKind.Panel }:
                DrawPanel(_box.Sentences);
                break;
        }
    }

    private static float Alpha(ulong from)
    {
        double t = (Time.GetTicksMsec() - from) / 1000.0;
        double a = t < BadgeRise ? t / BadgeRise : t < BadgeRise + BadgeHold ? 1 : 1 - (t - BadgeRise - BadgeHold) / BadgeFade;
        return (float)Math.Clamp(a, 0, 1);
    }

    // ---------------------------------------------------------------- K1: the narration panel

    /// <summary>Our narration panel in the scene's middle (K1): the sentences wrapped to the panel's width, the triangle.</summary>
    private void DrawPanel(IReadOnlyList<string> sentences)
    {
        var lines = sentences.SelectMany(s => Wrap(s, PanelMaxText)).ToList();
        if (lines.Count == 0)
            lines.Add("");
        float w = lines.Max(l => _gothic.GetStringSize(l, fontSize: PanelSize).X) + 80, h = 40 + lines.Count * PanelPitch + 20;
        var box = new Rect2(SceneCentreX - w / 2, SceneMiddleY - h / 2, w, h);
        Lacquer.DrawPanel(this, box, _k, MessageWindowView.NarrationFill);
        for (int i = 0; i < lines.Count; i++)
            DrawString(_gothic, new Vector2(box.Position.X + 40, box.Position.Y + 26 + i * PanelPitch + PanelSize * 0.85f), lines[i],
                fontSize: PanelSize, modulate: MessageWindowView.NarrationText);
        if (_waiting)
            Lacquer.DrawTriangle(this, new Vector2(box.End.X - 24, box.End.Y - 20), _k);
    }

    /// <summary>A sentence broken at spaces into lines no wider than <paramref name="width"/>.</summary>
    private IEnumerable<string> Wrap(string sentence, float width)
    {
        var line = "";
        foreach (var word in sentence.Split(' '))
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && _gothic.GetStringSize(next, fontSize: PanelSize).X > width)
            {
                yield return line;
                next = word;
            }
            line = next;
        }
        if (line.Length > 0)
            yield return line;
    }

    // ---------------------------------------------------------------- K3: the cards' events

    /// <summary>K3a: the card lit, a "DEF +n" badge over its portrait, the sentence on a small plate above the card.</summary>
    private void DrawDefence(BattleInfoBox box, Rect2 card, float alpha)
    {
        var tint = new Color(1, 1, 1, alpha);
        for (int i = 6; i > 0; i--)
            DrawRect(card.Grow(i), new Color(Accent, (30 + 20 * (6 - i)) / 255f * alpha), false, Lacquer.Line(1, _k));
        DrawRect(card, new Color(Accent, alpha), false, Lacquer.Line(2, _k));
        var badge = new Rect2(card.Position.X + 70, card.Position.Y + 10, 92, 30);
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = BadgeFill * tint, BorderColor = Accent * tint, CornerDetail = 8,
            CornerRadiusTopLeft = 15, CornerRadiusTopRight = 15, CornerRadiusBottomLeft = 15, CornerRadiusBottomRight = 15,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
        }, badge);
        Centred(_bold, badge, box.Mark, 18, Colors.White * tint);
        if (box.Sentences is not [var sentence, ..])
            return;
        float w = _gothic.GetStringSize(sentence, fontSize: PlateSize).X + 40;
        var plate = new Rect2(card.Position.X, card.Position.Y - 52, w, 40);
        Lacquer.DrawPanel(this, plate, _k, MessageWindowView.NarrationFill * tint);
        DrawString(_gothic, new Vector2(plate.Position.X + 20, Lacquer.Baseline(plate, _gothic, PlateSize)), sentence, fontSize: PlateSize,
            modulate: MessageWindowView.NarrationText * tint);
    }

    /// <summary>K3b: the card dimmed, a red stamp across it tilted a little, the click's triangle at its corner.</summary>
    private void DrawFall(BattleInfoBox box, Rect2 card)
    {
        DrawRect(card, new Color(0, 0, 0, 0.47f));
        var centre = card.GetCenter();
        DrawSetTransform(centre * _k, Mathf.DegToRad(-8), new Vector2(_k, _k));
        float w = Math.Max(250, _bold.GetStringSize(box.Mark, fontSize: 30).X + 50);
        var stamp = new Rect2(-w / 2, -31, w, 62);
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = Colors.Transparent, BorderColor = StampInk, CornerDetail = 6,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            BorderWidthLeft = 4, BorderWidthTop = 4, BorderWidthRight = 4, BorderWidthBottom = 4, DrawCenter = false,
        }, stamp);
        Centred(_bold, stamp, box.Mark, 30, StampInk);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_waiting)
            Lacquer.DrawTriangle(this, new Vector2(card.End.X - 18, card.End.Y - 16), _k);
    }

    // ---------------------------------------------------------------- K3c/K3d: the results plate

    /// <summary>
    /// The end as one plate over the dimmed scene: the reason as its title in the brush face and its sentence, each army's
    /// damages, the EXP, then a row for each level-up ("LEVEL UP", the unit, its gains).
    /// </summary>
    private void DrawResults(BattleResults r)
    {
        DrawRect(new Rect2(128, 0, 1024, 410), new Color(0, 0, 0, 0.43f));
        float h = 250 + (r.LevelUps.Count == 0 ? 0 : 30 + 44 * r.LevelUps.Count);
        var plate = new Rect2(SceneCentreX - PlateWidth / 2, Math.Max(8, 200 - h / 2), PlateWidth, h);
        Lacquer.DrawPanel(this, plate, _k);
        float x = plate.Position.X, y = plate.Position.Y, cx = SceneCentreX;
        Middle(_brush, cx + 1, y + 52, r.Title, 32, Colors.Black);
        Middle(_brush, cx, y + 50, r.Title, 32, Lacquer.Name);
        if (r.Reason.Length > 0)
            Middle(_cinzel, cx, y + 80, Fit(_cinzel, r.Reason, 18, PlateWidth - 60), 18, Lacquer.Subtitle);
        Lacquer.DrawDivider(this, x + 60, plate.End.X - 60, y + 100, _k);
        foreach (var (army, damages, ax) in new[] { (r.PlayerArmy, r.PlayerDamages, cx - 150), (r.EnemyArmy, r.EnemyDamages, cx + 150) })
        {
            Middle(_semi, ax, y + 138, Fit(_semi, army.ToUpperInvariant(), 20, 260), 20, Lacquer.Label);
            Middle(_bold, ax, y + 180, damages.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), 38, Lacquer.Minus);
            Middle(_cinzel, ax, y + 200, "damages", 16, Lacquer.Quiet);
        }
        DrawLine(new Vector2(cx, y + 118), new Vector2(cx, y + 200), Lacquer.DimGold, Lacquer.Line(1, _k));
        Middle(_semi, cx, y + 234, r.Exp + " EXP obtained", 22, Lacquer.Name);
        if (r.LevelUps.Count > 0)
        {
            Lacquer.DrawDivider(this, x + 60, plate.End.X - 60, y + 254, _k);
            for (int i = 0; i < r.LevelUps.Count; i++)
            {
                var up = r.LevelUps[i];
                var row = new Rect2(x + 40, y + 268 + i * 44, PlateWidth - 80, 36);
                Lacquer.DrawRowBox(this, row, false, _k);
                float baseline = Lacquer.Baseline(row, _semi, 18);
                DrawString(_bold, new Vector2(row.Position.X + 20, baseline), "LEVEL UP", fontSize: 15, modulate: Accent);
                DrawString(_semi, new Vector2(row.Position.X + 120, baseline), up.Unit, fontSize: 19, modulate: Lacquer.Name);
                float gx = Math.Max(row.Position.X + 240, row.Position.X + 120 + _semi.GetStringSize(up.Unit, fontSize: 19).X + 20);
                string gains = string.Join("  ·  ", up.Gains);
                DrawString(_cinzel, new Vector2(gx, baseline), Fit(_cinzel, gains, 16, row.End.X - 14 - gx), fontSize: 16, modulate: Lacquer.Value);
            }
        }
        if (_waiting)
            Lacquer.DrawTriangle(this, new Vector2(plate.End.X - 26, plate.End.Y - 22), _k);
    }

    /// <summary>A text cut with "…" to fit <paramref name="width"/>.</summary>
    private static string Fit(Font font, string text, int size, float width)
    {
        if (font.GetStringSize(text, fontSize: size).X <= width)
            return text;
        while (text.Length > 1 && font.GetStringSize(text + "…", fontSize: size).X > width)
            text = text[..^1];
        return text.TrimEnd() + "…";
    }

    /// <summary>A text centred on <paramref name="cx"/> at the baseline <paramref name="baseline"/>.</summary>
    private void Middle(Font font, float cx, float baseline, string text, int size, Color colour) =>
        DrawString(font, new Vector2(cx - font.GetStringSize(text, fontSize: size).X / 2, baseline), text, fontSize: size, modulate: colour);

    /// <summary>A text centred in <paramref name="rect"/>.</summary>
    private void Centred(Font font, Rect2 rect, string text, int size, Color colour) =>
        DrawString(font, new Vector2(rect.GetCenter().X - font.GetStringSize(text, fontSize: size).X / 2, Lacquer.Baseline(rect, font, size)),
            text, fontSize: size, modulate: colour);
}
