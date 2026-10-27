using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The round's order on our battle screen (<see cref="RoundOrder.Chips"/>), a chip per unit. A new order stands at once:
/// the order changes during an exchange, while the strip is out of the exchange's way, so the move a change once made
/// (a chip jumping ahead, the rest sliding; owner, 2026-10-12) was never seen and is gone (owner, 2026-10-27). While a
/// battle's opening stands the strip is held back; after its click the chips fly in from the right one after another
/// (<see cref="Hold"/>; Sengoku Rance's flags).
/// </summary>
internal sealed partial class OrderStrip : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    private readonly Dictionary<(BattleSide, int), Chip> _chips = [];
    private IReadOnlyList<OrderChip> _target = [];              // the order the chips stand at (or fly to)
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
    }

    public OrderStrip()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 1;                                             // over the names and the cards
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

    /// <summary>The order now: every chip at its place at once, or, while they fly in, flying to it.</summary>
    public void Show(IReadOnlyList<OrderChip> chips)
    {
        _target = chips;
        if (_fly >= 0)
            Sync(chips);                                        // they fly to the new places
        else
            Snap();
    }

    /// <summary>Whether the chips are flying in after the opening's click (<see cref="Hold"/>).</summary>
    public bool Flying => _fly >= 0;

    private static (BattleSide, int) Key(OrderChip chip) => (chip.Side, chip.Record);

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
            c.Panel.Position = places[Key(chip)];
            c.Panel.Visible = true;
        }
        _laidOut = Size;
    }

    /// <summary>Creates the chips that are new and restyles all.</summary>
    private void Sync(IReadOnlyList<OrderChip> chips)
    {
        foreach (var chip in chips)
        {
            if (!_chips.ContainsKey(Key(chip)))
            {
                var box = new StyleBoxFlat { BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2 };
                var label = new Label { Text = chip.Name, MouseFilter = MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
                var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = _fly < 0 };   // flying: StepFly shows it
                panel.AddThemeStyleboxOverride("panel", box);
                panel.AddChild(label);
                AddChild(panel);
                _chips[Key(chip)] = new Chip { Panel = panel, Box = box, Label = label };
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

    public override void _Process(double delta)
    {
        if (_fly >= 0)
            StepFly(delta);
        else if (Size != _laidOut)
            Snap();                                             // resized: the places again
    }
}
