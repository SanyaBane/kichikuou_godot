using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The hint that a picker's grid holds filled cells out of view, held upright (the owner's 3, 2026-11-04,
/// <c>tools/ui/mock_more_cells.py</c>; the Harem's and Treasure's grids) and on Treasure's grid held landscape (the owner's
/// R2, round P39, docs/ui/phone.md §53-§54): a lacquer pill over the rows' edge — "▼ 2 more
/// below" at their foot, "▲ 3 more above" at their top while scrolled past one — whose press scrolls the grid the least
/// that shows the nearest of them, and a tick on the scroll bar at every row holding a filled cell, the whole list laid
/// along the groove. The places are <see cref="PhonePickersPortrait"/>'s; the pills' boxes are kept from the last drawing
/// for the press.
/// </summary>
internal sealed class PickerMoreCells
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private PhoneFrame.Box? _above, _below;             // the pills drawn last, the canvas's units
    private int _nearestAbove, _nearestBelow;
    private bool? _pressed;                             // the pill pressed (true: above), till the release

    /// <summary>The pills and the ticks for <paramref name="filled"/> (the filled cells' numbers) over <paramref name="grid"/> at <paramref name="scroll"/>.</summary>
    public void Draw(CanvasItem c, PhoneUnits u, Font cinzel, PhonePickersPortrait.ScrolledGrid grid, int scroll, IReadOnlyCollection<int> filled)
    {
        _above = _below = null;
        if (grid.MaxScroll == 0)
            return;
        var f = u.Frame;
        var (track, _) = grid.Bar(f, scroll);
        var groove = u.Rect(track);
        float thick = u.Px(PhonePickersPortrait.TickThick), rim = u.Px(PhonePickersPortrait.TickRim), x0 = groove.Position.X - u.Px(PhonePickersPortrait.TickOut);
        foreach (int row in filled.Select(n => (n - 1) / grid.Columns).Distinct())
        {
            float y = grid.TickY(track, row) / u.K;
            c.DrawRect(new Rect2(x0 - rim, y - thick / 2 - rim, groove.End.X - x0 + rim, thick + 2 * rim), Ui.Ink);
            c.DrawRect(new Rect2(x0, y - thick / 2, groove.End.X - x0, thick), Ui.Text);
        }
        var (above, below, nearestAbove, nearestBelow) = grid.Hidden(filled, scroll);
        (_nearestAbove, _nearestBelow) = (nearestAbove, nearestBelow);
        int size = u.Size(PhonePickersPortrait.PillText);
        if (above > 0)
            _above = DrawPill(c, u, cinzel, grid, true, PhonePickersPortrait.MoreText(above, true), size);
        if (below > 0)
            _below = DrawPill(c, u, cinzel, grid, false, PhonePickersPortrait.MoreText(below, false), size);
    }

    private PhoneFrame.Box DrawPill(CanvasItem c, PhoneUnits u, Font cinzel, PhonePickersPortrait.ScrolledGrid grid, bool above, string text, int size)
    {
        float textWidth = cinzel.GetStringSize(text, fontSize: size).X;
        var box = PhonePickersPortrait.MorePill(u.Frame, grid, above, textWidth * u.K);
        var r = u.Rect(box);
        float radius = r.Size.Y / 2;
        Pill(c, new Rect2(r.Position + new Vector2(u.Px(4), u.Px(PhonePickersPortrait.PillShadow)), r.Size), radius, new Color(0, 0, 0, 0.85f), null, 0);
        bool held = _pressed == above;
        Pill(c, r, radius, held ? Ui.RowHover : Ui.Row, Ui.HoverLine, Math.Max(1, u.Px(4)));
        float cy = r.GetCenter().Y, half = u.Px(PhonePickersPortrait.PillArrowHalf);
        float ax = r.Position.X + u.Px(PhonePickersPortrait.PillPad) + half;
        Vector2[] arrow = above
            ? [new(ax - half, cy + half * 0.6f), new(ax + half, cy + half * 0.6f), new(ax, cy - half * 0.7f)]
            : [new(ax - half, cy - half * 0.6f), new(ax + half, cy - half * 0.6f), new(ax, cy + half * 0.7f)];
        c.DrawColoredPolygon(arrow, Ui.Name);
        c.DrawString(cinzel, new Vector2(r.Position.X + u.Px(PhonePickersPortrait.PillPad + PhonePickersPortrait.PillArrow),
            PhonePickerParts.Middle(cinzel, size, cy)), text, fontSize: size, modulate: Ui.Text);
        return box;
    }

    /// <summary>A rounded pill: <paramref name="fill"/>, its rim <paramref name="line"/> wide when <paramref name="rim"/> is given.</summary>
    private static void Pill(CanvasItem c, Rect2 r, float radius, Color fill, Color? rim, float line)
    {
        var box = new StyleBoxFlat { BgColor = fill, AntiAliasing = true };
        box.SetCornerRadiusAll((int)Math.Round(radius));
        if (rim is { } colour)
        {
            box.BorderColor = colour;
            box.SetBorderWidthAll((int)Math.Round(line));
        }
        c.DrawStyleBox(box, r);
    }

    /// <summary>
    /// A press on a pill (<paramref name="e"/>'s point in the canvas's units): taken, and at its release on the same pill
    /// the grid scrolls to the nearest filled cell out of view on its side (<paramref name="show"/>). Whether it was taken.
    /// </summary>
    public bool Touch(InputEvent e, Action<int> show, Action redraw)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                _pressed = Hit(press.Position);
                if (_pressed is null)
                    return false;
                redraw();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _pressed is { } pressed:
                _pressed = null;
                if (Hit(release.Position) == pressed && (pressed ? _nearestAbove : _nearestBelow) is var cell and > 0)
                    show(cell);
                redraw();
                return true;
            case InputEventMouseMotion when _pressed is not null:
                return true;
        }
        return false;
    }

    private bool? Hit(Vector2 at) =>
        _above is { } a && a.Contains(at.X, at.Y) ? true : _below is { } b && b.Contains(at.X, at.Y) ? false : null;
}
