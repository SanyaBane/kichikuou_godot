namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Save/Load screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PL2 as the owner
/// changed it, and §25, PY2): one page down the room. On top the title band (the title, Load | Save from <c>COMMAND</c> at
/// its right, <see cref="Chip"/>, a divider), the two lists' tabs (<see cref="Tab"/>), the heading with the sweep
/// (<see cref="Sweep"/>) and the list in one-line rows (<see cref="Row"/>; New save heading them in Save, <see cref="New"/>),
/// as many as the room above the page holds — nine on the reference phone —, the rest a drag away (the scroll bar at the
/// right). The chosen save's page stands on the bar (<see cref="Page"/>): its title with Delete (<see cref="Delete"/>), the
/// map at the room's width cut at its foot (<see cref="Map"/>; an auto-save's two lines on its load under the comment cut it
/// lower still), the figures in two columns (<see cref="Figure"/>), the comment with Edit (<see cref="Comment"/>,
/// <see cref="Edit"/>); the red button | Back in the bar (<see cref="Button"/>, <see cref="Back"/>; a way back stands right
/// of the action, owner, 2026-10-30). Delete's question hangs
/// under Delete, the sweep's under the sweep (<see cref="DeleteQuestion"/>, <see cref="SweepQuestion"/>). While the comment
/// is written its own card stands just over Android's keyboard (<see cref="Card"/>, PY2): whose comment, the field two lines
/// high (the words wrapped, <see cref="Wrap"/>), the count, Keep | Cancel at its foot, Cancel at the right thumb. The figures
/// are the mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>save_title</c>, <c>save_list</c>, <c>save_row</c>,
/// <c>save_page</c>, <c>back_and</c>, <c>pl2</c>, <c>comment_card</c>, <c>py2</c>) in the reference phone's pixels from the
/// room's top and the bar; the boxes in the canvas's units. A longer screen gives the list more rows in view. Interface
/// only. Engine-independent.
/// </summary>
public static class PhoneSavesUpright
{
    /// <summary>The list's, the page's and the card's distance from the room's sides.</summary>
    private const float SideIn = 16;

    private static float Left(PhoneFrame f) => f.Room.X + f.Px(SideIn);

    private static float Right(PhoneFrame f) => f.Room.Right - f.Px(SideIn);

    // ------------------------------------------------------------------ the title band (save_title)

    /// <summary>The title's left (in from the room's), its top under the room's and type (the brush face).</summary>
    public const float TitleIn = 30, TitleTop = 4, TitleText = 62;

    /// <summary>The divider under the band: under the room's top, its ends in from the room's sides.</summary>
    public const float Divider = 116, DividerIn = 24;

    /// <summary>Load | Save: 160 × 88, 10 apart, 8 under the room's top, the pair ending at the list's right.</summary>
    private const float ChipWidth = 160, ChipHeight = 88, ChipGap = 10, ChipTop = 8;

    /// <summary>The chips' type (Cinzel).</summary>
    public const float ChipText = 32;

    /// <summary>Load (<paramref name="save"/> false) or Save at the band's right, from <c>COMMAND</c>.</summary>
    public static PhoneFrame.Box Chip(PhoneFrame f, bool save)
    {
        float x = Right(f) - f.Px(2 * ChipWidth + ChipGap) + (save ? f.Px(ChipWidth + ChipGap) : 0);
        return new PhoneFrame.Box(x, f.Room.Y + f.Px(ChipTop), f.Px(ChipWidth), f.Px(ChipHeight));
    }

    // ------------------------------------------------------------------ the list (save_list, save_row)

    /// <summary>The lists' tabs: 104 high from 134 under the room's top, 12 apart, across the list.</summary>
    private const float TabsTop = 134, TabsHeight = 104, TabsGap = 12;

    /// <summary>The tabs' type (Cinzel).</summary>
    public const float TabText = 34;

    /// <summary>The list: the band, the tabs, the heading and the rows, across the room down to 36 over the page.</summary>
    public static PhoneFrame.Box List(PhoneFrame f)
    {
        float foot = Page(f).Y - f.Px(ListFoot);
        return new PhoneFrame.Box(Left(f), f.Room.Y, Right(f) - Left(f), Math.Max(0, foot - f.Room.Y));
    }

    /// <summary>The list's foot over the page, and the divider between them (over the page's top).</summary>
    private const float ListFoot = 36, PageDivider = 18;

    /// <summary>The divider's height between the list and the page.</summary>
    public static float PageDividerY(PhoneFrame f) => Page(f).Y - f.Px(PageDivider);

    /// <summary>The manual saves' tab (<paramref name="auto"/> false) or the auto-saves'.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, bool auto)
    {
        float w = (Right(f) - Left(f) - f.Px(TabsGap)) / 2;
        return new PhoneFrame.Box(Left(f) + (auto ? w + f.Px(TabsGap) : 0), f.Room.Y + f.Px(TabsTop), w, f.Px(TabsHeight));
    }

    /// <summary>The heading 18 under the tabs, 64 high; the sweep 470 wide at its right.</summary>
    private const float HeadTop = TabsTop + TabsHeight + 18, HeadHeight = 64, SweepWidth = 470;

    /// <summary>The heading's words: in from the list's left, their type (Cinzel); the sweep's type and its star's radius.</summary>
    public const float HeadIn = 8, HeadText = 26, SweepText = 28, SweepStar = 12;

    /// <summary>The heading's middle ("NEWEST FIRST" at the left).</summary>
    public static float HeadMiddle(PhoneFrame f) => f.Room.Y + f.Px(HeadTop + HeadHeight / 2);

    /// <summary>The sweep, "Delete all without ★ (n)", at the heading's right.</summary>
    public static PhoneFrame.Box Sweep(PhoneFrame f) =>
        new(Right(f) - f.Px(SweepWidth), f.Room.Y + f.Px(HeadTop), f.Px(SweepWidth), f.Px(HeadHeight));

    /// <summary>
    /// The rows: from 20 under the heading, 104 high and 10 apart, ending short of the list's right by the scroll bar's
    /// room (the reference, <see cref="PhoneScrollBar"/>: its right at the list's); New save 88 high over them in Save's
    /// manual saves.
    /// </summary>
    private const float RowsTop = HeadTop + HeadHeight + 20, RowHeight = 104, RowGap = 10, NewHeight = 88;

    private static float FirstRow(PhoneFrame f, bool newRow) => f.Room.Y + f.Px(RowsTop + (newRow ? NewHeight + RowGap : 0));

    private static float RowWidth(PhoneFrame f) => Right(f) - Left(f) - PhoneScrollBar.Room(f);

    /// <summary>New save over the rows (Save's manual saves).</summary>
    public static PhoneFrame.Box New(PhoneFrame f) => new(Left(f), f.Room.Y + f.Px(RowsTop), RowWidth(f), f.Px(NewHeight));

    /// <summary>A row's pitch: its height and the gap under it.</summary>
    public static float Pitch(PhoneFrame f) => f.Px(RowHeight + RowGap);

    /// <summary>How many rows are in view, under New save when <paramref name="newRow"/>.</summary>
    public static int RowsShown(PhoneFrame f, bool newRow) =>
        Math.Max(1, (int)Math.Floor((List(f).Bottom - FirstRow(f, newRow) + f.Px(RowGap)) / Pitch(f) + 0.0001f));

    /// <summary>The row at <paramref name="place"/> in view (0: the top one).</summary>
    public static PhoneFrame.Box Row(PhoneFrame f, int place, bool newRow) =>
        new(Left(f), FirstRow(f, newRow) + place * Pitch(f), RowWidth(f), f.Px(RowHeight));

    /// <summary>The scroll bar's groove right of the rows in view.</summary>
    public static PhoneFrame.Box ScrollBar(PhoneFrame f, bool newRow) =>
        PhoneScrollBar.Groove(f, Right(f), FirstRow(f, newRow), RowsShown(f, newRow) * Pitch(f) - f.Px(RowGap));

    /// <summary>Where a finger scrolls the list: New save and the rows in view, the list's width.</summary>
    public static PhoneFrame.Box Scrolled(PhoneFrame f, bool newRow)
    {
        var bar = ScrollBar(f, newRow);
        float top = f.Room.Y + f.Px(RowsTop);
        return new PhoneFrame.Box(Left(f), top, Right(f) - Left(f), bar.Bottom - top);
    }

    // ------------------------------------------------------------------ the page (save_page, compact)

    /// <summary>The page's title band (its title's middle in it, its type, the auto-save's tags'), Delete at its right (230 × 84, its type).</summary>
    private const float TitleBand = 100, DeleteWidth = 230, DeleteHeight = 84;

    /// <summary>The page's title: its middle under the page's top, its type (Cinzel), the auto-save's tags' type, Delete's type.</summary>
    public const float PageTitleMiddle = 40, PageTitleText = 34, TagText = 28, DeleteText = 30;

    /// <summary>The page's foot over the bar.</summary>
    private const float PageOver = 24;

    /// <summary>All Data's map, 1280 × 533, and how much of its height the page shows (the compact page: its top 80%).</summary>
    public const float MapShape = 533f / 1280f, MapShown = 0.8f;

    /// <summary>
    /// Under the map: 20, the figures' three rows 96 apart and 10, the comment's band (its label, 36, the box 104 high); an
    /// auto-save's lines 14 under the box, 40 apart.
    /// </summary>
    private const float FiguresDown = 20, FigureRows = 3, FigurePitch = 96, CommentDown = 10, BoxDown = 36, BoxHeight = 104,
        LinesDown = 14, LinePitch = 40, Lines = 2;

    private const float Under = FiguresDown + FigureRows * FigurePitch + CommentDown + BoxDown + BoxHeight;

    /// <summary>The figures' columns.</summary>
    public const int FigureColumns = 2;

    /// <summary>A figure: its label 8 in at its type (Cinzel), the value 34 under it at its type, shrunk to the column less 24.</summary>
    public const float FigureIn = 8, LabelText = 24, ValueDown = 34, ValueText = 38, ValueRoom = 24;

    /// <summary>The comment's box: its words 24 in at their type; Edit 204 wide at the page's right, 16 right of the box (its type); an auto-save's lines' type.</summary>
    public const float CommentIn = 24, CommentText = 38, EditText = 40, LineText = 28;

    private const float EditWidth = 204, EditGap = 16;

    private static float MapHeight(PhoneFrame f) => (Right(f) - Left(f)) * MapShape * MapShown;

    /// <summary>
    /// The page standing 24 over the bar across the room: its title band, the map at the room's width (its top
    /// <see cref="MapShown"/>), the figures and the comment.
    /// </summary>
    public static PhoneFrame.Box Page(PhoneFrame f)
    {
        float h = f.Px(TitleBand + Under) + MapHeight(f);
        return new PhoneFrame.Box(Left(f), f.Bar.Y - f.Px(PageOver) - h, Right(f) - Left(f), h);
    }

    /// <summary>The page title's middle.</summary>
    public static float TitleMiddle(PhoneFrame f) => Page(f).Y + f.Px(PageTitleMiddle);

    /// <summary>Delete at the title's right (the owner's B5).</summary>
    public static PhoneFrame.Box Delete(PhoneFrame f)
    {
        var page = Page(f);
        return new PhoneFrame.Box(page.Right - f.Px(DeleteWidth), page.Y, f.Px(DeleteWidth), f.Px(DeleteHeight));
    }

    /// <summary>
    /// The map at the page's width under the title, its foot cut off (and with <paramref name="lines"/> cut by an auto-save's
    /// lines' room too, so the page and the list stay where they are).
    /// </summary>
    public static PhoneFrame.Box Map(PhoneFrame f, bool lines)
    {
        var page = Page(f);
        float cut = lines ? f.Px(LinesDown + Lines * LinePitch) : 0;
        return new PhoneFrame.Box(page.X, page.Y + f.Px(TitleBand), page.W, Math.Max(0, MapHeight(f) - cut));
    }

    /// <summary>The figure <paramref name="index"/>'s label's top left (its value <see cref="ValueDown"/> under it) and its column's width.</summary>
    public static (float X, float Y, float W) Figure(PhoneFrame f, int index, bool lines)
    {
        var map = Map(f, lines);
        float w = map.W / FigureColumns;
        return (map.X + f.Px(FigureIn) + index % FigureColumns * w, map.Bottom + f.Px(FiguresDown) + index / FigureColumns * f.Px(FigurePitch), w);
    }

    /// <summary>The comment's label's top ("COMMENT", the figures' label type).</summary>
    public static float CommentLabel(PhoneFrame f, bool lines) =>
        Map(f, lines).Bottom + f.Px(FiguresDown + FigureRows * FigurePitch + CommentDown);

    /// <summary>The comment's box, 36 under its label, to 16 short of Edit.</summary>
    public static PhoneFrame.Box Comment(PhoneFrame f, bool lines)
    {
        var page = Page(f);
        return new PhoneFrame.Box(page.X, CommentLabel(f, lines) + f.Px(BoxDown), page.W - f.Px(EditWidth + EditGap), f.Px(BoxHeight));
    }

    /// <summary>Edit at the comment's right.</summary>
    public static PhoneFrame.Box Edit(PhoneFrame f, bool lines)
    {
        var box = Comment(f, lines);
        return new PhoneFrame.Box(Page(f).Right - f.Px(EditWidth), box.Y, f.Px(EditWidth), box.H);
    }

    /// <summary>The middle of an auto-save's line <paramref name="index"/> (0, 1) on what its load does, under the comment.</summary>
    public static float LineMiddle(PhoneFrame f, int index) =>
        Comment(f, true).Bottom + f.Px(LinesDown + LinePitch / 2 + index * LinePitch);

    // ------------------------------------------------------------------ the bar (back_and)

    /// <summary>Back's width at the bar's right, the gap between it and the red button.</summary>
    private const float BackWidth = 384, BarGap = 16;

    /// <summary>Back's and the red button's type (the brush face), its word alone.</summary>
    public const float BackText = 50, ButtonText = 58;

    /// <summary>Back at the bar's right — the right thumb: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box Back(PhoneFrame f) => new(f.Bar.Right - f.Px(BackWidth), f.Bar.Y, f.Px(BackWidth), f.Bar.H);

    /// <summary>The red button (Load, Save, Save over No. n) from the bar's left to 16 short of Back.</summary>
    public static PhoneFrame.Box Button(PhoneFrame f) =>
        new(f.Bar.X, f.Bar.Y, Math.Max(0, f.Bar.W - f.Px(BackWidth + BarGap)), f.Bar.H);

    // ------------------------------------------------------------------ the questions (the PC's callouts at the phone's sizes)

    /// <summary>A question's callout: 250 high, hanging 28 under its button at its right; Delete's 900 wide, the sweep's 1000.</summary>
    private const float QuestionHeight = 250, QuestionDown = 28, DeleteQuestionWidth = 900, SweepQuestionWidth = 1000;

    /// <summary>Delete's question, hanging under Delete over the map (its tip at Delete's middle).</summary>
    public static PhoneFrame.Box DeleteQuestion(PhoneFrame f)
    {
        var delete = Delete(f);
        return new PhoneFrame.Box(delete.Right - f.Px(DeleteQuestionWidth), delete.Bottom + f.Px(QuestionDown), f.Px(DeleteQuestionWidth),
            f.Px(QuestionHeight));
    }

    /// <summary>The sweep's question, hanging under the sweep over the list's first rows (its tip at the sweep's middle).</summary>
    public static PhoneFrame.Box SweepQuestion(PhoneFrame f)
    {
        var sweep = Sweep(f);
        return new PhoneFrame.Box(sweep.Right - f.Px(SweepQuestionWidth), sweep.Bottom + f.Px(QuestionDown), f.Px(SweepQuestionWidth),
            f.Px(QuestionHeight));
    }

    /// <summary>A question's answer: Delete (<paramref name="yes"/>, red) left of Keep, at the callout's foot right (held landscape's).</summary>
    public static PhoneFrame.Box Answer(PhoneFrame f, PhoneFrame.Box question, bool yes) => PhoneSaves.Answer(f, question, yes);

    // ------------------------------------------------------------------ the comment's card (comment_card, py2)

    /// <summary>
    /// Android's keyboard held upright where the system reports none (a PC playing a phone, or before it is up): Gboard with
    /// its suggestions and the gesture bar, 1000 of the 2712 (the mock-up's <c>KB_TOP</c>).
    /// </summary>
    public const float KeyboardFallback = 1000;

    /// <summary>The card: 560 high, 24 over the keyboard, across the room; never above 14 under the room's top.</summary>
    private const float CardOver = 24, CardHeight = 560, CardTop = 14;

    /// <summary>The card's words: "COMMENT" at the left and whose at the right, in this far, their middle and type (Cinzel).</summary>
    public const float CardLabelIn = 32, CardLabelMiddle = 56, CardLabelText = 28, CardTitleText = 30;

    /// <summary>The field's type, its words in from its left, the lines' pitch in its type; the count's middle under the field, its type; the buttons' type.</summary>
    public const float FieldText = 46, FieldTextIn = 26, FieldLeading = 1.3f, CountDown = 36, CountText = 32, CancelText = 48, KeepText = 56;

    /// <summary>The field's lines: the words wrap into two in view.</summary>
    public const int FieldLines = 2;

    private const float FieldIn = 24, FieldDown = 100, FieldHeight = 200, ButtonHeight = 130, ButtonFoot = 36, ButtonGap = 16;

    /// <summary>
    /// The comment's own card just over the keyboard <paramref name="keyboard"/> high (the canvas's units; 0 or less: none
    /// reported, the reference's <see cref="KeyboardFallback"/>), across the room.
    /// </summary>
    public static PhoneFrame.Box Card(PhoneFrame f, float keyboard)
    {
        float top = f.Screen.H - (keyboard > 0 ? keyboard : f.Px(KeyboardFallback));
        float y = Math.Max(f.Room.Y + f.Px(CardTop), top - f.Px(CardOver + CardHeight));
        return new PhoneFrame.Box(Left(f), y, Right(f) - Left(f), f.Px(CardHeight));
    }

    /// <summary>The field two lines high, 100 under the card's top, 24 in from its sides.</summary>
    public static PhoneFrame.Box Field(PhoneFrame f, float keyboard)
    {
        var card = Card(f, keyboard);
        return new PhoneFrame.Box(card.X + f.Px(FieldIn), card.Y + f.Px(FieldDown), card.W - f.Px(2 * FieldIn), f.Px(FieldHeight));
    }

    /// <summary>The count's right end and middle ("25 / 60"), under the field's right.</summary>
    public static (float Right, float Middle) Count(PhoneFrame f, float keyboard)
    {
        var card = Card(f, keyboard);
        return (card.Right - f.Px(CardLabelIn), Field(f, keyboard).Bottom + f.Px(CountDown));
    }

    private static PhoneFrame.Box Half(PhoneFrame f, float keyboard, bool right)
    {
        var card = Card(f, keyboard);
        float w = (card.W - f.Px(2 * FieldIn + ButtonGap)) / 2;
        float x = card.X + f.Px(FieldIn) + (right ? w + f.Px(ButtonGap) : 0);
        return new PhoneFrame.Box(x, card.Bottom - f.Px(ButtonFoot + ButtonHeight), w, f.Px(ButtonHeight));
    }

    /// <summary>Cancel at the card's foot right — the right thumb: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box Cancel(PhoneFrame f, float keyboard) => Half(f, keyboard, right: true);

    /// <summary>Keep, red, at the card's foot left.</summary>
    public static PhoneFrame.Box Keep(PhoneFrame f, float keyboard) => Half(f, keyboard, right: false);

    // ------------------------------------------------------------------ the field's words wrapped

    /// <summary>A line of the field's words: its first letter's index in the text, and its letters.</summary>
    public readonly record struct FieldLine(int Start, string Text)
    {
        /// <summary>The index after its last letter.</summary>
        public int End => Start + Text.Length;
    }

    /// <summary>
    /// <paramref name="text"/> wrapped into lines no wider than <paramref name="room"/> as <paramref name="width"/> measures
    /// them: each line ends after a space where one fits (the space stays with the line before), a word wider than the room
    /// is broken between its letters; an empty text is one empty line.
    /// </summary>
    public static IReadOnlyList<FieldLine> Wrap(string text, Func<string, float> width, float room)
    {
        var lines = new List<FieldLine>();
        int start = 0;
        while (start < text.Length)
        {
            int end = start, space = -1;
            while (end < text.Length && width(text[start..(end + 1)]) <= room)
            {
                if (text[end] == ' ')
                    space = end;
                end++;
            }
            if (end == text.Length)
            {
                lines.Add(new FieldLine(start, text[start..]));
                break;
            }
            if (text[end] == ' ')
                space = end;                                            // the space that does not fit ends the line
            int cut = space >= start ? space + 1 : Math.Max(end, start + 1);
            lines.Add(new FieldLine(start, text[start..cut]));
            start = cut;
        }
        if (lines.Count == 0)
            lines.Add(new FieldLine(0, ""));
        return lines;
    }

    /// <summary>The line the caret before letter <paramref name="column"/> stands on, and the letters left of it there.</summary>
    public static (int Line, string Before) CaretAt(IReadOnlyList<FieldLine> lines, int column)
    {
        for (int i = 0; i < lines.Count; i++)
            if (column < lines[i].End || i == lines.Count - 1)
            {
                int at = Math.Clamp(column - lines[i].Start, 0, lines[i].Text.Length);
                return (i, lines[i].Text[..at]);
            }
        return (0, "");
    }

    /// <summary>
    /// The caret's column for a tap <paramref name="x"/> along line <paramref name="line"/> (from the words' left, as
    /// <paramref name="width"/> measures): the letter boundary nearest it, a wrapped line's last before its end.
    /// </summary>
    public static int ColumnAt(IReadOnlyList<FieldLine> lines, int line, float x, Func<string, float> width)
    {
        var at = lines[Math.Clamp(line, 0, lines.Count - 1)];
        bool last = line >= lines.Count - 1;
        int most = last ? at.Text.Length : Math.Max(0, at.Text.Length - 1);
        int best = 0;
        float off = float.MaxValue;
        for (int n = 0; n <= most; n++)
        {
            float d = Math.Abs(width(at.Text[..n]) - x);
            if (d < off)
            {
                off = d;
                best = n;
            }
        }
        return at.Start + best;
    }
}
