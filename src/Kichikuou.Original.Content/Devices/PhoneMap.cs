using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// <c>COMMAND</c>'s map on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1, LM1b,
/// LM1c): the map stays in the frame as on the tablet, nothing of ours over it but the plates, and our controls go to the
/// black sides. On the map's wait the left side holds the Menu cell, the status stacked under it (<see cref="StatusBox"/>)
/// and the drawer at the thumb; the right side the week's uses by group (<see cref="Tags"/>) and Commands at the thumb
/// (<see cref="CommandsCell"/>). The command panel takes both sides: the status's figures on the left's top
/// (<see cref="HeaderBox"/>) and the other items from the foot up under them; Turn END on the right's top and the week's
/// commands under their caption from the foot up over Close, which stands at the right side's foot (<see cref="FootCell"/>;
/// owner, 2026-10-30: the week's commands at the right thumb). A province's
/// card stands beside it in the frame (<see cref="ProvinceCard"/>), the province ringed, its commands in the right side
/// from the foot up over Return. The rows are two lines (the tag over the name), 14.5 × 7.0 mm, as many as a rail has
/// room for at that height, lower when they would not fit. The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>)
/// in the reference phone's pixels (<see cref="PhoneFrame.Px"/>), the boxes in the canvas's units. Interface only.
/// Engine-independent.
/// </summary>
public static class PhoneMap
{
    /// <summary>A rail's rows: their height (7.0 mm), the room between them, the lowest one's foot over the gesture zone.</summary>
    private const float RowHeight = 123, RowGap = 12, RailFoot = 10;

    /// <summary>The rails in a side: from the hole's strip plus the first to the frame less the second (the mock-ups' 14.5 mm), mirrored on the right.</summary>
    private const float RailInner = 6, RailOuter = 10;

    /// <summary>The status under the Menu cell (the date, the budget, the troops): its distance from the cell and its height.</summary>
    private const float StatusGap = 34, StatusHeight = 246;

    /// <summary>The status's figures on the left side's top while the command panel is open, and the room under them before the items.</summary>
    private const float HeaderTop = 20, HeaderHeight = 130, HeaderGap = 30;

    /// <summary>The week's caption under Turn END: its distance from it, its height, the room under it before the commands.</summary>
    private const float CaptionTop = 26, CaptionHeight = 34, CaptionGap = 16;

    /// <summary>The week's uses in the right side: from this far under its top, a group's caption, a tag and its pitch, the room after a group.</summary>
    private const float TagsTop = 24, GroupCaption = 36, TagHeight = 46, TagPitch = 54, GroupGap = 12, TagInset = 6;

    /// <summary>Commands at the right thumb (8.5 mm high); Close and Return a row's height where it stands.</summary>
    private const float CommandsHeight = 150;

    /// <summary>Turn END on the right side's top.</summary>
    private const float TurnEndTop = 16, TurnEndHeight = 140;

    /// <summary>A province's commands: the rail's highest row's least distance from the side's top, and the place's caption over them.</summary>
    private const float ProvinceTop = 16, PlaceCaption = 50;

    /// <summary>
    /// A province's commands in the frame (PE): a column's width (25.6 mm), the room between the columns, the panel's
    /// padding, its least distance from the frame's right edge and top.
    /// </summary>
    private const float ColumnWidth = 450, ColumnGap = 15, ColumnPad = 21, ColumnMargin = 21;

    /// <summary>
    /// A province's card (LM1c): its width, its distance from the province, its margins in the frame, its padding, the city's
    /// picture (at the PC's 3:2 as the tablet's 272 × 180), the room under the picture, the name's line, the army's line
    /// under a long name, a figure's line.
    /// </summary>
    private const float CardWidth = 560, CardGap = 90, CardMargin = 20, CardPad = 24, PictureHeight = 512 * 180f / 272,
        PictureGap = 16, NameLine = 72, ArmyLine = 36, FigureLine = 44;

    /// <summary>The ring round the tapped province (its radius) and the plates' type over the map (2 mm).</summary>
    public const float RingRadius = 74, PlateText = 36;

    /// <summary>
    /// The rails' rows' type, the command panel's (Turn END's among them) and a province's: the game's font at one size within
    /// a rail (docs/ui/style.md, "Buttons side by side"), scaled with a low row, shrunk together where a name is too long.
    /// </summary>
    public const float RowText = 36;

    /// <summary>A province's rows' type past what the side holds, in the frame's panel down two columns (2.3 mm).</summary>
    public const float ColumnsRowText = 40;

    /// <summary>The status stacked under the Menu cell in the left side (LM1a).</summary>
    public static PhoneFrame.Box StatusBox(PhoneFrame f) => Left(f, PhoneScenes.MenuCell(f).Bottom + f.Px(StatusGap), StatusHeight);

    /// <summary>The status's figures on the left side's top while the command panel is open (LM1b): the Menu cell is gone then.</summary>
    public static PhoneFrame.Box HeaderBox(PhoneFrame f) => Left(f, f.LeftRoom.Y + f.Px(HeaderTop), HeaderHeight);

    /// <summary>The caption over the week's commands ("This week") under Turn END.</summary>
    public static PhoneFrame.Box CaptionBox(PhoneFrame f) => Right(f, TurnEnd(f).Bottom + f.Px(CaptionTop), CaptionHeight);

    /// <summary>The command panel's week's commands in the right side over Close, the first lowest, under their caption.</summary>
    public static IReadOnlyList<PhoneFrame.Box> CommandRows(PhoneFrame f, int count) =>
        Stack(f, Right(f, 0, 0), FootCell(f).Y - f.Px(RowGap), CaptionBox(f).Bottom + f.Px(CaptionGap), count);

    /// <summary>Commands at the right thumb on the map's wait (LM1a).</summary>
    public static PhoneFrame.Box CommandsCell(PhoneFrame f) => FootOf(f, CommandsHeight);

    /// <summary>Close (the command panel's) and Return (a province's) at the right side's foot, where Commands stands: a row's height.</summary>
    public static PhoneFrame.Box FootCell(PhoneFrame f) => FootOf(f, RowHeight);

    /// <summary>Turn END on the right side's top (LM1b).</summary>
    public static PhoneFrame.Box TurnEnd(PhoneFrame f) => Right(f, f.RightRoom.Y + f.Px(TurnEndTop), TurnEndHeight);

    /// <summary>The command panel's other items in the left side, the first lowest (index 0 at the foot), under the header.</summary>
    public static IReadOnlyList<PhoneFrame.Box> LinkRows(PhoneFrame f, int count) =>
        Stack(f, Left(f, 0, 0), f.LeftRoom.Bottom - f.Px(RailFoot), HeaderBox(f).Bottom + f.Px(HeaderGap), count);

    /// <summary>
    /// A province's commands (LM1c): in the right side over Return, the first lowest, with <paramref name="caption"/> (the
    /// dungeon's menu, its place) the caption's room kept over them; past what the side holds at a row's full height, in
    /// the frame's columns, the first top left (<see cref="ProvincePanel"/>).
    /// </summary>
    public static IReadOnlyList<PhoneFrame.Box> ProvinceRows(PhoneFrame f, int count, bool caption = false) =>
        ProvincePanel(f, count, caption)?.Rows ?? Stack(f, Right(f, 0, 0), RowsFoot(f), RowsTop(f, caption), count);

    /// <summary>How many of a province's commands the right side holds at a row's full height over Return: seven on the reference phone.</summary>
    public static int SideMost(PhoneFrame f, bool caption = false)
    {
        float h = f.Px(RowHeight), g = f.Px(RowGap);
        return Math.Max(0, (int)MathF.Floor((RowsFoot(f) - RowsTop(f, caption) + g) / (h + g) + 0.0001f));
    }

    /// <summary>The panel of a province's commands in the frame and the rows in it (<see cref="ProvincePanel"/>).</summary>
    public sealed record ProvinceColumns(PhoneFrame.Box Panel, IReadOnlyList<PhoneFrame.Box> Rows);

    /// <summary>
    /// A province's commands past what the right side holds (the owner's PE, 2026-10-30; docs/ui/touch.md, "Fifteen
    /// commands"): a panel in the frame's right edge at the right thumb, its foot level with Return's, the commands in two
    /// columns at the phone's type, down the left one, then the right (<see cref="ProvinceMenu.FirstColumn"/>), the rows
    /// 7.0 × 25.6 mm, lower in proportion should they not fit the frame's height. Null while the side holds them, and for
    /// the dungeon's menu (<paramref name="caption"/>: at most four cells and Look Around).
    /// </summary>
    public static ProvinceColumns? ProvincePanel(PhoneFrame f, int count, bool caption = false)
    {
        int most = SideMost(f, caption);
        if (caption || count <= most)
            return null;
        int first = ProvinceMenu.FirstColumn(count, most);
        float w = f.Px(ColumnWidth), gap = f.Px(ColumnGap), pad = f.Px(ColumnPad), h = f.Px(RowHeight), g = f.Px(RowGap);
        float bottom = FootCell(f).Bottom, top = f.Frame.Y + f.Px(ColumnMargin);
        float scale = Math.Min(1, (bottom - top - 2 * pad) / (first * h + (first - 1) * g));
        (h, g) = (h * scale, g * scale);
        float height = 2 * pad + first * h + (first - 1) * g;
        float right = f.Frame.Right - f.Px(ColumnMargin), x = right - 2 * w - gap - 2 * pad;
        var panel = new PhoneFrame.Box(x, bottom - height, right - x, height);
        return new ProvinceColumns(panel, [.. Enumerable.Range(0, count).Select(i =>
        {
            int column = i < first ? 0 : 1, row = i < first ? i : i - first;
            return new PhoneFrame.Box(x + pad + column * (w + gap), panel.Y + pad + row * (h + g), w, h);
        })]);
    }

    /// <summary>The province's rows in the side: the lowest's foot, the highest's least top.</summary>
    private static float RowsFoot(PhoneFrame f) => FootCell(f).Y - f.Px(RowGap);

    private static float RowsTop(PhoneFrame f, bool caption) => f.RightRoom.Y + f.Px(ProvinceTop + (caption ? PlaceCaption : 0));

    /// <summary>The place's caption over a province's rows (the dungeon's menu): its band, just over the highest row.</summary>
    public static PhoneFrame.Box PlaceBox(PhoneFrame f, IReadOnlyList<PhoneFrame.Box> rows)
    {
        float bottom = rows.Count > 0 ? rows[^1].Y : FootCell(f).Y;
        return Right(f, bottom - f.Px(PlaceCaption), PlaceCaption);
    }

    /// <summary>A group of the week's uses in the right side: its caption's band and its tags top down.</summary>
    public sealed record TagGroup(PhoneFrame.Box Caption, IReadOnlyList<PhoneFrame.Box> Tags);

    /// <summary>
    /// The week's uses as the status strip's tags by group in the right side (LM1a), <paramref name="groups"/> holding each
    /// group's count of tags: from its top down, each group's caption over its tags, the tags as wide as the rail.
    /// </summary>
    public static IReadOnlyList<TagGroup> Tags(PhoneFrame f, IReadOnlyList<int> groups)
    {
        var column = Right(f, 0, 0);
        float x = column.X + f.Px(TagInset), w = column.W - 2 * f.Px(TagInset);
        float y = f.RightRoom.Y + f.Px(TagsTop);
        var result = new List<TagGroup>();
        foreach (int count in groups)
        {
            var caption = new PhoneFrame.Box(x, y, w, f.Px(GroupCaption));
            y += f.Px(GroupCaption);
            var tags = new List<PhoneFrame.Box>();
            for (int i = 0; i < count; i++)
            {
                tags.Add(new PhoneFrame.Box(x, y, w, f.Px(TagHeight)));
                y += f.Px(TagPitch);
            }
            result.Add(new TagGroup(caption, tags));
            y += f.Px(GroupGap);
        }
        return result;
    }

    /// <summary>
    /// A province's card (LM1c): <see cref="Box"/> beside the province, the city's picture at its top (<see cref="Picture"/>),
    /// the name's line under it (<see cref="NameTop"/>), then the figures a line each from <see cref="FiguresTop"/>,
    /// <see cref="FigurePitch"/> apart; <see cref="Pad"/> in from its sides.
    /// </summary>
    public sealed record Card(PhoneFrame.Box Box, PhoneFrame.Box Picture, float NameTop, float FiguresTop, float FigurePitch, float Pad);

    /// <summary>
    /// The card of a province at <paramref name="at"/> (the canvas's units; null when it is out of view, the card then in
    /// the frame's middle) with <paramref name="figures"/> lines of figures, the army under the name when
    /// <paramref name="armyUnder"/> (a long name): left of it when the frame has room there, else right of it, its middle
    /// level with it, kept in the frame over the gesture zone — and left of the commands' panel (<paramref name="beside"/>,
    /// <see cref="ProvincePanel"/>), however near it the province lies.
    /// </summary>
    public static Card ProvinceCard(PhoneFrame f, (float X, float Y)? at, int figures, bool armyUnder = false,
        PhoneFrame.Box? beside = null)
    {
        float w = f.Px(CardWidth), pad = f.Px(CardPad), picture = f.Px(PictureHeight);
        float name = f.Px(NameLine + (armyUnder ? ArmyLine : 0));
        float h = pad + picture + f.Px(PictureGap) + name + figures * f.Px(FigureLine) + pad;
        float end = beside?.X ?? f.Frame.Right;
        float left = f.Frame.X + f.Px(CardMargin), right = end - f.Px(CardMargin);
        float top = f.LeftRoom.Y + f.Px(CardMargin), foot = f.LeftRoom.Bottom - f.Px(CardMargin);
        float x, y;
        if (at is not { } p)
        {
            x = f.Frame.X + (end - f.Frame.X - w) / 2;
            y = f.Frame.Y + (f.Frame.H - h) / 2;
        }
        else
        {
            x = p.X - f.Px(CardGap) - w;
            if (x < left)
                x = p.X + f.Px(CardGap);
            x = Math.Min(x, right - w);
            y = p.Y - h / 2;
        }
        y = Math.Clamp(y, top, Math.Max(top, foot - h));
        var box = new PhoneFrame.Box(x, y, w, h);
        float nameTop = y + pad + picture + f.Px(PictureGap);
        return new Card(box, new PhoneFrame.Box(x + pad, y + pad, w - 2 * pad, picture), nameTop, nameTop + name,
            f.Px(FigureLine), pad);
    }

    /// <summary>The rows from <paramref name="foot"/> up, index 0 lowest, lower than a row's height when they would reach over <paramref name="top"/>.</summary>
    private static IReadOnlyList<PhoneFrame.Box> Stack(PhoneFrame f, PhoneFrame.Box column, float foot, float top, int count) =>
        Stack(f, column.X, column.W, foot, top, count, RowHeight, RowGap);

    /// <summary>
    /// <paramref name="count"/> rows <paramref name="width"/> wide from <paramref name="x"/>, from <paramref name="foot"/> up,
    /// index 0 lowest: <paramref name="height"/> reference pixels high and <paramref name="gap"/> apart, both lower in
    /// proportion when the rows would reach over <paramref name="top"/> (a side's rails: the map's, the battle's).
    /// </summary>
    internal static IReadOnlyList<PhoneFrame.Box> Stack(PhoneFrame f, float x, float width, float foot, float top, int count,
        float height, float gap)
    {
        if (count <= 0)
            return [];
        float h = f.Px(height), g = f.Px(gap);
        float scale = Math.Min(1, Math.Max(0, foot - top) / (count * h + (count - 1) * g));
        h *= scale;
        g *= scale;
        return [.. Enumerable.Range(0, count).Select(i => new PhoneFrame.Box(x, foot - (i + 1) * h - i * g, width, h))];
    }

    private static PhoneFrame.Box FootOf(PhoneFrame f, float height)
    {
        float foot = f.RightRoom.Bottom - f.Px(RailFoot);
        return Right(f, foot - f.Px(height), height);
    }

    /// <summary>The left side's rail at <paramref name="y"/>, <paramref name="height"/> reference pixels high (PhoneScenes's cells).</summary>
    internal static PhoneFrame.Box Left(PhoneFrame f, float y, float height)
    {
        var room = f.LeftRoom;
        return new PhoneFrame.Box(room.X + f.Px(RailInner), y, room.W - f.Px(RailInner + RailOuter), f.Px(height));
    }

    /// <summary>The right side's rail, the left's mirrored: from the frame plus <see cref="RailOuter"/> to the hole's strip less <see cref="RailInner"/>.</summary>
    internal static PhoneFrame.Box Right(PhoneFrame f, float y, float height)
    {
        var room = f.RightRoom;
        return new PhoneFrame.Box(room.X + f.Px(RailOuter), y, room.W - f.Px(RailInner + RailOuter), f.Px(height));
    }
}
