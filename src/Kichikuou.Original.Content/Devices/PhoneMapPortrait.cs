using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// <c>COMMAND</c>'s map on the phone held upright (docs/ui/phone-build.md, step P2; docs/ui/phone.md, §3, PM1a, PM1b,
/// PM1c): the status on top (<see cref="StatusBand"/>: the date, the budget, the troops, the week's uses as tags in rows),
/// the map's whole height at the screen's width under it down to the bar (<see cref="MapBox"/>, scrolled sideways only,
/// <see cref="MapView"/>), the bar with Commands at the right thumb (<see cref="CommandsCell"/>; Menu, Log and Skip stay at
/// its left, PK1). Commands opens the command panel as a sheet over the map's foot (<see cref="Commands"/>: the other items
/// in the left column and the week's commands in the right, both from the foot up, Turn END at the top right; owner,
/// 2026-10-30: the week's commands at the right thumb), Close
/// where Commands was; a province's card and commands rise as a sheet too (<see cref="Province"/>), Return there, and the
/// map is scrolled up so the ringed province stays over the sheet (<see cref="MapView.Lift"/>). The figures are the
/// mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>m1()</c>) in the reference phone's pixels (<see cref="PhoneFrame.Px"/>),
/// the boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneMapPortrait
{
    /// <summary>
    /// The status band: how far it reaches over the room's top (into the hole's strip), its least height (two rows of tags),
    /// its text's room from the sides, the date's, the budget's and the troops' tops, the tags' top, their height and pitch,
    /// the room between two tags and after a group, the room under the last row.
    /// </summary>
    private const float StatusOver = 10, StatusHeight = 290, StatusSide = 32, DateTop = 26, BudgetTop = 30, TroopsTop = 86,
        TagsTop = 146, TagHeight = 48, TagPitch = 58, TagGap = 8, GroupGap = 22, TagsFoot = 38;

    /// <summary>The map's foot over the bar.</summary>
    private const float MapOverBar = 12;

    /// <summary>Commands (and Close, Return in its place) at the bar's right: its width (25 mm).</summary>
    private const float CommandsWidth = 440;

    /// <summary>
    /// The command panel's sheet (PM1b): its padding over Turn END, its rows' foot over the map's, the columns' margins and
    /// the room between them, the rows' height (7.1 mm) and the room between them, the room under Turn END, "This week"'s
    /// band under it over the commands.
    /// </summary>
    private const float SheetTop = 24, SheetFoot = 28, ColumnSide = 16, RowHeight = 124, RowGap = 14, TurnEndGap = 30,
        CaptionHeight = 56;

    /// <summary>The sheets' least distance from the status band: some of the map always shows.</summary>
    private const float SheetLeast = 240;

    /// <summary>
    /// A province's sheet (PM1c): the city's picture's place and width (at the tablet's 272 × 180), the text's left after it
    /// and its right margin, the name's, the army's and the figures' tops and the figures' pitch, the rows' margins and
    /// height (7.3 mm) and the room between them, the rows' foot over the map's, the room between the card and the rows,
    /// the place's caption over the rows (the dungeon's menu).
    /// </summary>
    private const float CardSide = 30, CardTop = 34, PictureWidth = 420, PictureHeight = PictureWidth * 180f / 272, TextGap = 36,
        TextRight = 40, NameTop = 40, ArmyTop = 126, FiguresTop = 182, FigurePitch = 50, ProvinceRowSide = 24,
        ProvinceRowHeight = 128, ProvinceRowGap = 14, ProvinceRowsFoot = 24, CardGap = 56, PlaceCaption = 60;

    /// <summary>The ring round the tapped province (its radius) and the room kept round it over the sheet and at the sides.</summary>
    public const float RingRadius = 74, RingRoom = 30;

    /// <summary>A tap's reach round a marker's point: a circle 8 mm across (PM1a's note), the nearest marker within it wins.</summary>
    public const float TapRadius = 4 * 446 / 25.4f;

    /// <summary>The plates' type over the map (2 mm), as held landscape.</summary>
    public const float PlateText = PhoneMap.PlateText;

    /// <summary>
    /// The sheets' rows' type, the command panel's (Turn END's among them) and a province's: the game's font at one size
    /// within a sheet (docs/ui/style.md, "Buttons side by side"), scaled with a low row, shrunk together where a name is too long.
    /// </summary>
    public const float RowText = 40;

    /// <summary>The map in its own pixels (<c>WAO_MAP.ADV</c>: six pictures of 640 × 400).</summary>
    public const int MapWidth = 1920, MapHeight = 800;

    // ---------------------------------------------------------------- the status

    /// <summary>The status band's lines: the date's and the troops' left, the budget's right, each line's top.</summary>
    public sealed record Status(PhoneFrame.Box Band, float Left, float Right, float DateTop, float BudgetTop, float TroopsTop);

    /// <summary>
    /// The week's uses as tags across the band (PM1a): the tags of <paramref name="widths"/> (each group's tags' widths in
    /// the canvas's units) left to right, wrapping to a new row where one would pass the band's right margin, a group's
    /// first tag after a wider gap; each group's boxes in order.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<PhoneFrame.Box>> Tags(PhoneFrame f, IReadOnlyList<IReadOnlyList<float>> widths)
    {
        float left = f.Room.X + f.Px(StatusSide), right = f.Room.Right - f.Px(StatusSide);
        float x = left, y = BandTop(f) + f.Px(TagsTop);
        var groups = new List<IReadOnlyList<PhoneFrame.Box>>();
        foreach (var group in widths)
        {
            var boxes = new List<PhoneFrame.Box>();
            foreach (float w in group)
            {
                if (x > left && x + w > right)
                    (x, y) = (left, y + f.Px(TagPitch));
                boxes.Add(new PhoneFrame.Box(x, y, w, f.Px(TagHeight)));
                x += w + f.Px(TagGap);
            }
            groups.Add(boxes);
            x += f.Px(GroupGap);
        }
        return groups;
    }

    /// <summary>The status band over the map with <paramref name="tagRows"/> rows of tags (from <see cref="Tags"/>): taller than the least for three rows on.</summary>
    public static Status StatusBand(PhoneFrame f, int tagRows)
    {
        float top = BandTop(f);
        float h = Math.Max(f.Px(StatusHeight), f.Px(TagsTop + Math.Max(0, tagRows - 1) * TagPitch + TagHeight + TagsFoot));
        return new Status(new PhoneFrame.Box(f.Room.X, top, f.Room.W, h), f.Room.X + f.Px(StatusSide),
            f.Room.Right - f.Px(StatusSide), top + f.Px(DateTop), top + f.Px(BudgetTop), top + f.Px(TroopsTop));
    }

    /// <summary>The rows <paramref name="tags"/> (from <see cref="Tags"/>) take.</summary>
    public static int TagRows(IReadOnlyList<IReadOnlyList<PhoneFrame.Box>> tags)
    {
        var tops = tags.SelectMany(g => g).Select(b => b.Y).Distinct().Count();
        return Math.Max(1, tops);
    }

    private static float BandTop(PhoneFrame f) => Math.Max(f.Screen.Y, f.Room.Y - f.Px(StatusOver));

    // ---------------------------------------------------------------- the map and the bar

    /// <summary>The map's room: the screen's width from the status band's foot down to the bar.</summary>
    public static PhoneFrame.Box MapBox(PhoneFrame f, PhoneFrame.Box band)
    {
        float foot = MapFoot(f);
        return new PhoneFrame.Box(f.Room.X, band.Bottom, f.Room.W, Math.Max(0, foot - band.Bottom));
    }

    /// <summary>Commands at the bar's right, at the right thumb (PM1a); Close (PM1b) and Return (PM1c) in its place.</summary>
    public static PhoneFrame.Box CommandsCell(PhoneFrame f)
    {
        var bar = f.Bar;
        float w = Math.Min(f.Px(CommandsWidth), bar.W);
        return new PhoneFrame.Box(bar.Right - w, bar.Y, w, bar.H);
    }

    private static float MapFoot(PhoneFrame f) => f.Bar.Y - f.Px(MapOverBar);

    // ---------------------------------------------------------------- the command panel

    /// <summary>The command panel's sheet: the sheet, "This week"'s band, Turn END, the week's commands and the other items, each list's first lowest.</summary>
    public sealed record CommandSheet(PhoneFrame.Box Sheet, PhoneFrame.Box Caption, PhoneFrame.Box TurnEnd,
        IReadOnlyList<PhoneFrame.Box> Commands, IReadOnlyList<PhoneFrame.Box> Links);

    /// <summary>
    /// The command panel as a sheet over the map's foot (PM1b): two columns side by side, <paramref name="links"/> other
    /// items in the left and <paramref name="commands"/> of the week's in the right, both from the foot up, Turn END and
    /// "This week" over the commands; as tall as the longer column, its rows lower when it would come within
    /// <see cref="SheetLeast"/> of <paramref name="band"/>.
    /// </summary>
    public static CommandSheet Commands(PhoneFrame f, PhoneFrame.Box band, int commands, int links)
    {
        float colw = (f.Room.W - 3 * f.Px(ColumnSide)) / 2;
        float lx = f.Room.X + f.Px(ColumnSide), rx = lx + colw + f.Px(ColumnSide);
        float foot = MapFoot(f) - f.Px(SheetFoot);
        float head = f.Px(SheetTop + RowHeight + TurnEndGap + CaptionHeight);
        float least = band.Bottom + f.Px(SheetLeast) + head;
        int n = Math.Max(commands, links);
        float scale = Rows(f, n, RowHeight, RowGap, foot - least);
        float h = f.Px(RowHeight) * scale, g = f.Px(RowGap) * scale;
        float rowsTop = foot - n * h - Math.Max(0, n - 1) * g;
        float captionTop = rowsTop - f.Px(CaptionHeight);
        float turnTop = captionTop - f.Px(TurnEndGap + RowHeight);
        var sheet = new PhoneFrame.Box(f.Room.X, turnTop - f.Px(SheetTop), f.Room.W, MapFoot(f) - turnTop + f.Px(SheetTop));
        return new CommandSheet(sheet, new PhoneFrame.Box(rx, captionTop, colw, f.Px(CaptionHeight)),
            new PhoneFrame.Box(rx, turnTop, colw, f.Px(RowHeight)), Stack(rx, colw, foot, h, g, commands),
            Stack(lx, colw, foot, h, g, links));
    }

    // ---------------------------------------------------------------- a province

    /// <summary>
    /// A province's sheet: the sheet, the city's picture, the text's left and right (the name, the army and the figures
    /// beside the picture), the name's, the army's and the first figure's tops and the figures' pitch, the commands (the
    /// first lowest) and the place's caption over them (the dungeon's menu; null without one).
    /// </summary>
    public sealed record ProvinceSheet(PhoneFrame.Box Sheet, PhoneFrame.Box Picture, float TextLeft, float TextRight, float NameTop,
        float ArmyTop, float FiguresTop, float FigurePitch, IReadOnlyList<PhoneFrame.Box> Rows, PhoneFrame.Box? Place);

    /// <summary>
    /// A province's card and commands as a sheet over the map's foot (PM1c): the city's picture at its top left, the name,
    /// the army and <paramref name="figures"/> lines of figures beside it, <paramref name="rows"/> commands across the
    /// sheet from its foot up, the place's caption over them when <paramref name="caption"/>; as tall as that, its rows
    /// lower when it would come within <see cref="SheetLeast"/> of <paramref name="band"/>.
    /// </summary>
    public static ProvinceSheet Province(PhoneFrame f, PhoneFrame.Box band, int figures, int rows, bool caption = false)
    {
        float foot = MapFoot(f) - f.Px(ProvinceRowsFoot);
        float card = f.Px(CardTop) + Math.Max(f.Px(PictureHeight), f.Px(FiguresTop) + figures * f.Px(FigurePitch) - f.Px(CardTop));
        float place = caption ? f.Px(PlaceCaption) : 0;
        float least = band.Bottom + f.Px(SheetLeast) + card + f.Px(CardGap) + place;
        float scale = Rows(f, rows, ProvinceRowHeight, ProvinceRowGap, foot - least);
        float h = f.Px(ProvinceRowHeight) * scale, g = f.Px(ProvinceRowGap) * scale;
        float x = f.Room.X + f.Px(ProvinceRowSide), w = f.Room.W - 2 * f.Px(ProvinceRowSide);
        var boxes = Stack(x, w, foot, h, g, rows);
        float rowsTop = rows > 0 ? boxes[^1].Y : foot;
        PhoneFrame.Box? placeBox = caption ? new PhoneFrame.Box(x, rowsTop - place, w, place) : null;
        float top = rowsTop - place - f.Px(CardGap) - card;
        var sheet = new PhoneFrame.Box(f.Room.X, top, f.Room.W, MapFoot(f) - top);
        var picture = new PhoneFrame.Box(f.Room.X + f.Px(CardSide), top + f.Px(CardTop), f.Px(PictureWidth), f.Px(PictureHeight));
        return new ProvinceSheet(sheet, picture, picture.Right + f.Px(TextGap), f.Room.Right - f.Px(TextRight), top + f.Px(NameTop),
            top + f.Px(ArmyTop), top + f.Px(FiguresTop), f.Px(FigurePitch), boxes, placeBox);
    }

    // ---------------------------------------------------------------- the map's view

    /// <summary>
    /// The map's view in <see cref="MapBox"/>: its whole height fills the box, so <see cref="Scale"/> canvas units a map
    /// pixel and <see cref="Width"/> map pixels across; <see cref="Left"/> is the map's column at the box's left (our own
    /// camera: the script's stays its own), <see cref="Lift"/> how far the map is drawn higher, in the canvas's units (a
    /// province's sheet over its foot).
    /// </summary>
    public readonly record struct MapView(PhoneFrame.Box Box, float Left, float Lift)
    {
        /// <summary>The canvas's units a map pixel: the map's height fills the box.</summary>
        public float Scale => Box.H / MapHeight;

        /// <summary>How many of the map's columns the box shows.</summary>
        public float Width => Box.W / Scale;

        /// <summary>The leftmost column the view may start at: the map's right edge at the box's.</summary>
        public float MaxLeft => Math.Max(0, MapWidth - Width);

        /// <summary>The view with its left at <paramref name="left"/>, a whole column, kept on the map.</summary>
        public MapView At(float left) => this with { Left = Math.Clamp(MathF.Round(left), 0, MathF.Floor(MaxLeft)) };

        /// <summary>How many whole map rows the view is drawn higher (<see cref="Lift"/>).</summary>
        public int LiftRows => (int)MathF.Round(Lift / Scale);

        /// <summary>
        /// The view centred on the script's own: its view's corner <paramref name="corner"/> (640 × 400 of the map), so the
        /// part the original would show is in the middle of ours.
        /// </summary>
        public MapView Centred((int X, int Y) corner) => At(corner.X + 320 - Width / 2);

        /// <summary>The canvas's point of map point (<paramref name="mx"/>, <paramref name="my"/>).</summary>
        public (float X, float Y) FromMap(float mx, float my) => (Box.X + (mx - Left) * Scale, Box.Y + my * Scale - Lift);

        /// <summary>The map point under the canvas's point (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public (float X, float Y) ToMap(float x, float y) => (Left + (x - Box.X) / Scale, (y - Box.Y + Lift) / Scale);

        /// <summary>Whether map point (<paramref name="mx"/>, <paramref name="my"/>) shows in the box.</summary>
        public bool Shows(float mx, float my)
        {
            var (x, y) = FromMap(mx, my);
            return Box.Contains(x, y);
        }
    }

    /// <summary>
    /// The view that keeps map point (<paramref name="mx"/>, <paramref name="my"/>) — a ringed province — in sight over a
    /// sheet whose top is <paramref name="sheetTop"/> (PM1c): drawn higher by as many whole map rows as its ring needs to
    /// stand clear of the sheet (at most the sheet's height: the map's foot then at the sheet's top), and moved sideways by
    /// as few whole columns as its ring needs to stand clear of the screen's sides.
    /// </summary>
    public static MapView Lift(PhoneFrame f, MapView view, float mx, float my, float sheetTop)
    {
        float room = f.Px(RingRadius + RingRoom);
        float left = view.Left;
        float x = view.Box.X + (mx - left) * view.Scale;
        if (x - room < view.Box.X)
            left -= MathF.Ceiling((view.Box.X - (x - room)) / view.Scale);
        else if (x + room > view.Box.Right)
            left += MathF.Ceiling((x + room - view.Box.Right) / view.Scale);
        float y = view.Box.Y + my * view.Scale;
        float most = Math.Max(0, view.Box.Bottom - sheetTop);
        float lift = Math.Min(MathF.Ceiling(Math.Max(0, y + room - sheetTop) / view.Scale) * view.Scale, most);
        return view.At(left) with { Lift = lift };
    }

    /// <summary>
    /// The marker a tap at the canvas's point (<paramref name="x"/>, <paramref name="y"/>) means: the one of
    /// <paramref name="markers"/> (those shown) whose point is nearest within <see cref="TapRadius"/>; null when none is.
    /// </summary>
    public static MapMarker? MarkerAt(PhoneFrame f, MapView view, IEnumerable<MapMarker> markers, float x, float y)
    {
        float reach = f.Px(TapRadius);
        MapMarker? nearest = null;
        float best = reach * reach;
        foreach (var marker in markers)
        {
            var (px, py) = view.FromMap(marker.MapX, marker.MapY);
            float d = (px - x) * (px - x) + (py - y) * (py - y);
            if (d <= best)
                (nearest, best) = (marker, d);
        }
        return nearest;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>How much of their height <paramref name="count"/> rows keep to fit in <paramref name="room"/> (1: all of it).</summary>
    private static float Rows(PhoneFrame f, int count, float height, float gap, float room) =>
        count <= 0 ? 1 : Math.Clamp(room / (count * f.Px(height) + (count - 1) * f.Px(gap)), 0, 1);

    /// <summary><paramref name="count"/> rows from <paramref name="foot"/> up, index 0 lowest.</summary>
    private static IReadOnlyList<PhoneFrame.Box> Stack(float x, float w, float foot, float h, float g, int count) =>
        [.. Enumerable.Range(0, Math.Max(0, count)).Select(i => new PhoneFrame.Box(x, foot - (i + 1) * h - i * g, w, h))];
}
