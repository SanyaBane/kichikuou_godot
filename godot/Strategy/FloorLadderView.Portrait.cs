using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The dungeon's menus on the phone held upright (docs/ui/phone-build.md, step P8b; docs/ui/phone.md, §10, PF1 and PF1e):
// the screen black (the floors' own EC 0; before a search the map is not drawn), the floor's picture cut from the engine's
// picture (inside WAO_SUB 331's frame) at the room's width in a gold frame, window 113's lines drawn here left of the rail
// at 3.3 mm (the message window leaves it to this view, TakeWindow), the tablet's rail at the right thumb standing on the
// bar's foot with no panel — held landscape's rungs at PhoneLadderPortrait's places. Drawn in the frame's layout pixels over
// the whole screen (PhoneUnits).
internal sealed partial class FloorLadderView
{
    /// <summary>The engine's picture (1280 × 800 layout pixels), whose floor's picture the view shows upright.</summary>
    public Texture2D? Screen { get; set; }

    /// <summary>Window 113 as the view draws it upright: the dungeon, the floor and the searcher's unit, or the dungeon's state before a search.</summary>
    public MessageWindow? Window { get; set; }

    /// <summary>
    /// Held upright and open the view draws <paramref name="window"/> itself left of its rail (PF1): it takes it and returns
    /// true, so the message windows leave it out; otherwise false.
    /// </summary>
    public bool TakeWindow(MessageWindow? window)
    {
        var taken = Visible && _phone is { IsPortrait: true } ? window : null;
        if (!(taken?.Lines ?? []).SequenceEqual(Window?.Lines ?? []) || (taken is null) != (Window is null))
        {
            Window = taken;
            QueueRedraw();
        }
        return taken is not null;
    }

    private int Others => _entry?.Others.Count ?? 0;

    private Rect2 PortraitRectOf(PhoneFrame f, Part part)
    {
        var u = new PhoneUnits(f);
        if (_entry is not null)
            return u.Rect(part switch
            {
                Part.Start => PhoneLadderPortrait.Start(f),
                Part.Return => PhoneLadderPortrait.Return(f, Others),
                _ => PhoneLadderPortrait.Other(f, part - Part.Other0, Others),
            });
        return u.Rect(part switch
        {
            Part.Up => PhoneLadderPortrait.Way(f, down: false),
            Part.Down => PhoneLadderPortrait.Way(f, down: true),
            _ => PhoneLadderPortrait.Retreat(f),
        });
    }

    /// <summary>The rail's whole place, from its top to the bar's foot.</summary>
    private Rect2 PortraitRailRect(PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        var foot = PhoneLadderPortrait.Way(f, down: true);
        float top = PhoneLadderPortrait.Top(f, _entry is not null, Others);
        return u.Rect(new PhoneFrame.Box(foot.X, top, foot.W, foot.Bottom - top));
    }

    /// <summary>The black screen, the floor's picture in its gold frame and window 113's lines; the rail is drawn over them.</summary>
    private void DrawPortraitFrame(PhoneUnits u)
    {
        var f = u.Frame;
        bool entry = _entry is not null;
        DrawRect(new Rect2(0, 0, f.Screen.W / u.K, f.Screen.H / u.K), Colors.Black);
        var picture = u.Rect(PhoneLadderPortrait.Picture(f, entry, Others));
        var (sx, sy, sw, sh) = PhoneLadderPortrait.Source;
        if (Screen is { } screen)
            DrawTextureRectRegion(screen, picture, new Rect2(sx, sy, sw, sh));
        DrawRect(picture, Ui.Gold, false, Lacquer.Line(u.Px(3), _k));
        if (Window is not { } window)
            return;
        var box = u.Rect(PhoneLadderPortrait.Window(f, entry, Others));
        Ui.DrawPanel(this, box, _k, Ui.NarrationFill);
        float x = box.Position.X + u.Px(PhoneLadderPortrait.LinesIn), room = box.End.X - u.Px(PhoneLadderPortrait.LinesIn) - x;
        int size = u.Size(PhoneLadderPortrait.LinesText);
        foreach (string line in window.Lines)
            size = Math.Min(size, SideCells.Fit(_gothic, line, size, room));
        float top = box.Position.Y + u.Px(PhoneLadderPortrait.LinesTop);
        var rows = window.Glyphs.Select(g => g.Y).Distinct().Order().ToList();     // a line each, top to bottom
        for (int i = 0; i < window.Lines.Count; i++)
        {
            // An empty line of the script keeps its place: a line's row from its cells' height (the window's own pitch).
            int row = i < rows.Count && window.Glyphs.Count > 0
                ? (int)Math.Round((rows[i] - rows[0]) / (double)Math.Max(1, window.Glyphs[0].Size)) : i;
            DrawString(_gothic, new Vector2(x, top + row * u.Px(PhoneLadderPortrait.LinesPitch) + _gothic.GetAscent(size)),
                window.Lines[i], fontSize: size, modulate: Ui.NarrationText);
        }
    }
}
