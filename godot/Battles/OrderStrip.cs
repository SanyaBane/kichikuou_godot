using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The round's order on our battle screen (<see cref="RoundOrder.Chips"/>), a chip per unit, that moves
/// on its own while the game goes on (owner, 2026-10-12): when the order changes (damage re-sorts the
/// waiting units, 01-flow.md §6), a unit that jumps ahead rises a row (over the strip, into the scene: owner,
/// 2026-10-16), slides to its new place while the chips it passes make room, and drops into it; the rest slide. A change that comes while chips still
/// move makes them finish at once (<see cref="HurrySeconds"/>), then the next one starts. While the
/// message skip runs the chips stand at their places at once. While a battle's opening stands the strip is held back;
/// after its click the chips fly in from the right one after another (<see cref="Hold"/>; Sengoku Rance's flags).
/// </summary>
internal sealed partial class OrderStrip : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    /// <summary>How long a change of the order moves (Option → Battle); 0 places the chips at once.</summary>
    private static double Seconds => BattleScreenSettings.Get(BattleScreenSettings.StripSeconds);

    /// <summary>How fast a move under way finishes when the next change comes.</summary>
    private static double HurrySeconds => Math.Max(0.001, BattleScreenSettings.Get(BattleScreenSettings.StripHurrySeconds));

    // The phases of a jump, as parts of the move: up, across, down.
    private static double DownEnd => BattleScreenSettings.Get(BattleScreenSettings.StripDropShare);
    private static double AcrossEnd => 1 - DownEnd;

    private readonly Dictionary<(BattleSide, int), Chip> _chips = [];
    private IReadOnlyList<OrderChip> _target = [];              // the order the chips go to (or stand at)
    private IReadOnlyList<OrderChip>? _next;                    // the order that came while they move
    private double _t = 1;                                      // the move's progress, 1 when standing
    private double _speed = 1;                                  // parts of the move per Seconds
    private float _scale = 1;
    private Vector2 _laidOut;                                   // the size the places were computed for
    private bool _held;                                         // hidden while the battle's opening stands
    private double _fly = -1;                                   // seconds into the fly-in; -1 when not flying

    /// <summary>How far apart the chips start flying in (Option → Battle, "Strip: fly in"); 0 places them at once.</summary>
    private static double FlySeconds => BattleScreenSettings.Get(BattleScreenSettings.StripFlySeconds);

    /// <summary>
    /// How fast a chip flies in, in layout pixels a second: Sengoku Rance's 0.6 px/ms on its 800-wide screen, ×1.6 on ours
    /// (docs/sengoku/04-screen.md §4), at its 0.25 s apart; as much slower or faster as the setting is.
    /// </summary>
    private const double FlySpeed = 960, FlyApart = 0.25;

    private static readonly Color PlayerColour = new(0.27f, 0.46f, 0.86f);
    private static readonly Color EnemyColour = new(0.82f, 0.3f, 0.3f);

    private sealed class Chip
    {
        public required PanelContainer Panel;
        public required StyleBoxFlat Box;
        public required Label Label;
        public Vector2 From, To;
        public bool Jumps, Leaving, Arriving;
    }

    public OrderStrip()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 1;                                             // a jumping chip passes over the names and the cards
    }

    /// <summary>Option → Display: the chips styled again in the palette's colours.</summary>
    public void ApplyPalette()
    {
        foreach (var chip in _target)
            Restyle(chip);
    }

    /// <summary>The screen's scale (1 in a 1280×800 window): the chips are placed again at once.</summary>
    public void SetScale(float scale)
    {
        if (scale == _scale)
            return;
        _scale = scale;
        Snap();
    }

    /// <summary>
    /// The order now. A new order or a new turn starts a move (or, while one is under way, hurries it and
    /// follows it); <paramref name="instant"/> puts every chip at its place at once.
    /// </summary>
    public void Show(IReadOnlyList<OrderChip> chips, bool instant)
    {
        bool moved = !chips.Select(Key).SequenceEqual(_target.Select(Key));
        bool newTurn = Acting(chips) != Acting(_target);
        foreach (var chip in chips)
        {
            if (_chips.ContainsKey(Key(chip)))
                Restyle(chip);
        }
        if (instant || Seconds <= 0)
        {
            _next = null;
            _target = chips;
            Snap();
            return;
        }
        if (_fly >= 0)
        {
            _target = chips;                                    // they fly to the new places
            Sync(chips);
            return;
        }
        if (_t < 1)
        {
            if (moved || newTurn)
                _speed = Math.Max(_speed, (1 - _t) * Seconds / HurrySeconds);
            if (moved || _next is not null)
                _next = chips;                                  // after the move under way (the latest order wins)
            else
                _target = chips;                                // the styles only
            return;
        }
        if (moved)
            Start(chips);
        else
            _target = chips;
    }

    /// <summary>Whether the chips are flying in after the opening's click (<see cref="Hold"/>).</summary>
    public bool Flying => _fly >= 0;

    private static (BattleSide, int) Key(OrderChip chip) => (chip.Side, chip.Record);

    private static (BattleSide, int)? Acting(IReadOnlyList<OrderChip> chips) =>
        chips.FirstOrDefault(c => c.State == ChipState.Acting) is { } a ? Key(a) : null;

    /// <summary>
    /// <paramref name="held"/>: the strip is hidden (a battle's opening stands). Let go, the chips fly in from the right
    /// one after another, the first first (owner, 2026-10-15; Sengoku Rance's flags, docs/sengoku/04-screen.md §4), or
    /// stand at their places at once when <paramref name="instant"/>.
    /// </summary>
    public void Hold(bool held, bool instant)
    {
        if (held == _held)
            return;
        _held = held;
        Visible = !held;
        if (held)
        {
            _fly = -1;
            return;
        }
        if (_next is { } next)
            _target = next;
        _next = null;
        Snap();
        if (instant || FlySeconds <= 0)
            return;
        _fly = 0;
        StepFly(0);
    }

    private void StepFly(double delta)
    {
        _fly += delta;
        double apart = FlySeconds;
        if (apart <= 0)
        {
            Snap();
            return;
        }
        double speed = FlySpeed * FlyApart / apart * _scale;
        var places = Places(_target);
        float from = Size.X + 10 * _scale;                      // the screen's right edge
        bool flying = false;
        for (int i = 0; i < _target.Count; i++)
        {
            var c = _chips[Key(_target[i])];
            var to = places[Key(_target[i])];
            double flown = speed * (_fly - i * apart);
            float x = (float)Math.Max(to.X, from - flown);
            flying |= x > to.X;
            c.Panel.Visible = flown > 0;
            c.Panel.Position = new Vector2(x, to.Y);
            c.Panel.Modulate = Colors.White;
        }
        if (!flying)
            Snap();
    }

    /// <summary>Every chip at its place now; the chips that left are gone.</summary>
    private void Snap()
    {
        _fly = -1;
        var shown = _target.Select(Key).ToHashSet();
        foreach (var (key, c) in _chips.Where(p => !shown.Contains(p.Key)).ToList())
        {
            c.Panel.QueueFree();                                // the chips of units no longer in the order (or of the last battle)
            _chips.Remove(key);
        }
        Sync(_target);
        var places = Places(_target);
        foreach (var chip in _target)
        {
            var c = _chips[Key(chip)];
            c.From = c.To = places[Key(chip)];
            c.Jumps = c.Arriving = c.Leaving = false;
            c.Panel.Position = c.To;
            c.Panel.Modulate = Colors.White;
            c.Panel.Visible = true;
        }
        _t = 1;
        _speed = 1;
        _laidOut = Size;
    }

    /// <summary>A move from where the chips stand to <paramref name="chips"/>' places.</summary>
    private void Start(IReadOnlyList<OrderChip> chips)
    {
        var old = _target.Select(Key).ToList();
        _target = chips;
        Sync(chips);
        var places = Places(chips);
        var jumpers = Jumpers(old, [.. chips.Select(Key)]);
        foreach (var (key, c) in _chips)
        {
            c.From = c.Panel.Position;
            if (places.TryGetValue(key, out var to))
            {
                if (c.Arriving)
                    c.From = c.Panel.Position = to;                 // a new chip fades in at its place
                c.To = to;
                c.Jumps = jumpers.Contains(key);
            }
            else
            {
                c.To = c.From;
                c.Leaving = true;
            }
        }
        _t = 0;
        _speed = 1;
    }

    /// <summary>Creates the chips that are new (arriving at their places) and restyles all.</summary>
    private void Sync(IReadOnlyList<OrderChip> chips)
    {
        foreach (var chip in chips)
        {
            if (!_chips.ContainsKey(Key(chip)))
            {
                var box = new StyleBoxFlat { BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2 };
                var label = new Label { Text = chip.Name, MouseFilter = MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
                var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
                panel.AddThemeStyleboxOverride("panel", box);
                panel.AddChild(label);
                AddChild(panel);
                var c = new Chip { Panel = panel, Box = box, Label = label, Arriving = true };
                _chips[Key(chip)] = c;
                panel.Modulate = new Color(1, 1, 1, 0);
            }
            Restyle(chip);
        }
        foreach (var chip in _chips.Values)
        {
            chip.Panel.Size = chip.Panel.GetCombinedMinimumSize();
        }
    }

    private void Restyle(OrderChip chip)
    {
        if (!_chips.TryGetValue(Key(chip), out var c))
            return;
        var side = chip.Side == BattleSide.Player ? PlayerColour : EnemyColour;
        int pad = (int)Math.Round(6 * _scale);
        c.Box.ContentMarginLeft = c.Box.ContentMarginRight = pad;
        c.Box.BgColor = chip.State == ChipState.Acted ? Ui.OffButton : side.Darkened(0.35f);
        c.Box.BorderColor = chip.State switch
        {
            ChipState.Acting => Ui.HoverLine,
            ChipState.Acted => Ui.OffButton,
            _ => side,
        };
        c.Label.Text = chip.Name;
        c.Label.AddThemeColorOverride("font_color", chip.State == ChipState.Acted ? Ui.OffText : Ui.Text);
    }

    /// <summary>The chips' places: in a row, centred, 4 layout pixels apart.</summary>
    private Dictionary<(BattleSide, int), Vector2> Places(IReadOnlyList<OrderChip> chips)
    {
        float gap = 4 * _scale;
        var sizes = chips.Select(c => _chips[Key(c)].Panel.GetCombinedMinimumSize()).ToList();
        float width = sizes.Sum(s => s.X) + gap * Math.Max(0, sizes.Count - 1);
        float x = (Size.X - width) / 2;
        var places = new Dictionary<(BattleSide, int), Vector2>();
        for (int i = 0; i < chips.Count; i++)
        {
            places[Key(chips[i])] = new Vector2(x, (Size.Y - sizes[i].Y) / 2);
            x += sizes[i].X + gap;
        }
        return places;
    }

    /// <summary>
    /// The chips that jump: those out of the longest run kept in the same order between
    /// <paramref name="before"/> and <paramref name="after"/>; among runs as long, the one that keeps the
    /// chips that moved right (a chip moves left when it lost troops, so it is the one that jumps).
    /// </summary>
    private static HashSet<(BattleSide, int)> Jumpers(List<(BattleSide, int)> before, List<(BattleSide, int)> after)
    {
        var common = after.Where(before.Contains).ToList();
        var beforeCommon = before.Where(after.Contains).ToList();
        int n = common.Count;
        var oldIndex = common.Select(k => beforeCommon.IndexOf(k)).ToArray();
        var keepsRight = common.Select((_, i) => i >= oldIndex[i] ? 1 : 0).ToArray();     // 0: it moved left
        var length = new int[n];
        var score = new int[n];
        var previous = new int[n];
        for (int i = 0; i < n; i++)
        {
            (length[i], score[i], previous[i]) = (1, keepsRight[i], -1);
            for (int j = 0; j < i; j++)
            {
                if (oldIndex[j] < oldIndex[i] && (length[j] + 1, score[j] + keepsRight[i]).CompareTo((length[i], score[i])) > 0)
                    (length[i], score[i], previous[i]) = (length[j] + 1, score[j] + keepsRight[i], j);
            }
        }
        var kept = new HashSet<(BattleSide, int)>();
        int best = -1;
        for (int i = 0; i < n; i++)
        {
            if (best < 0 || (length[i], score[i]).CompareTo((length[best], score[best])) > 0)
                best = i;
        }
        for (int i = best; i >= 0; i = previous[i])
            kept.Add(common[i]);
        return [.. common.Where(k => !kept.Contains(k))];
    }

    public override void _Process(double delta)
    {
        if (_fly >= 0)
        {
            StepFly(delta);
            return;
        }
        if (Size != _laidOut && _t >= 1)
            Snap();                                             // resized: the places again
        if (_t >= 1)
            return;
        _t = Math.Min(1, _t + delta * _speed / Seconds);
        float rise = _chips.Values.Select(c => c.Panel.Size.Y).DefaultIfEmpty(0).Max() + 4 * _scale;
        foreach (var c in _chips.Values)
        {
            if (c.Leaving)
            {
                c.Panel.Modulate = new Color(1, 1, 1, (float)Math.Max(0, 1 - _t / DownEnd));
                continue;
            }
            if (c.Arriving)
                c.Panel.Modulate = new Color(1, 1, 1, (float)Ease(Phase(_t, AcrossEnd, 1)));
            float across = (float)Ease(Phase(_t, DownEnd, AcrossEnd));
            var at = c.From.Lerp(c.To, across);
            if (c.Jumps)
            {
                double up = Ease(Phase(_t, 0, DownEnd)) - Ease(Phase(_t, AcrossEnd, 1));
                at.Y -= (float)up * rise;
            }
            c.Panel.Position = at;
        }
        if (_t < 1)
            return;
        foreach (var (key, c) in _chips.Where(p => p.Value.Leaving).ToList())
        {
            c.Panel.QueueFree();
            _chips.Remove(key);
        }
        foreach (var c in _chips.Values)
        {
            c.Jumps = c.Arriving = false;
            c.Panel.Modulate = Colors.White;
        }
        _speed = 1;
        if (_next is { } next)
        {
            _next = null;
            if (next.Select(Key).SequenceEqual(_target.Select(Key)))
                _target = next;
            else
                Start(next);
        }
    }

    /// <summary>Where <paramref name="t"/> is between <paramref name="start"/> and <paramref name="end"/>, 0-1.</summary>
    private static double Phase(double t, double start, double end) => Math.Clamp((t - start) / (end - start), 0, 1);

    private static double Ease(double x) => x * x * (3 - 2 * x);
}
