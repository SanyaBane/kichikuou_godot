namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Save/Load screen on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15, LL1, and §26,
/// LY1, the owner's choices): the PC's split across the whole width between the hole's strips. At the left the title, Load
/// | Save from <c>COMMAND</c> (<see cref="Chip"/>), the two lists' tabs (<see cref="Tab"/>), the heading with the sweep
/// (<see cref="Sweep"/>) and the list in one-line rows (<see cref="Row"/>; New save heading them in Save, <see cref="New"/>),
/// as many as fit — seven on the reference phone —, the rest a drag away (the scroll bar). At the right the chosen save's
/// page: its title with Delete (<see cref="Delete"/>), the map at the page's width cut lower (<see cref="Map"/>) for the
/// figures in three columns (<see cref="Figure"/>) and the comment with Edit (<see cref="Comment"/>, <see cref="Edit"/>; an
/// auto-save's two lines on what its load does under them, the map cut lower by their room), the red button | Back at its
/// foot (<see cref="Button"/>, <see cref="Back"/>), Back at the right thumb (a way back stands right of the action, owner,
/// 2026-10-30); Delete's and the sweep's questions in callouts at them (<see cref="DeleteQuestion"/>,
/// <see cref="SweepQuestion"/>). While the comment is written, its own card stands just over Android's keyboard
/// (<see cref="Card"/>, LY1): whose comment, the field in one line, the count, Keep | Cancel at the right thumb. The figures
/// are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>ll1</c>, <c>land_save_list</c>, <c>save_row</c>,
/// <c>land_save_page</c>, <c>load_save_chips</c>, <c>land_back</c>, <c>bin_button</c>, <c>ly1</c>; the foot's and the
/// card's buttons swapped since) in the reference phone's pixels, the boxes in the canvas's units; a longer screen widens
/// the page (the list keeps its width). Interface only. Engine-independent.
/// </summary>
public static class PhoneSaves
{
    // ------------------------------------------------------------------ the split

    /// <summary>The room's edges in from the hole's strips; the list's width, the gap between it and the page.</summary>
    private const float ScreenIn = 6, ListWidth = 884, PageGap = 40;

    /// <summary>How far over the gesture zone the list and the foot's buttons end.</summary>
    private const float FootOver = 10;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ScreenIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ScreenIn);

    private static float Foot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    /// <summary>The list's column at the left: the title, Load | Save, the tabs, the heading and the rows.</summary>
    public static PhoneFrame.Box List(PhoneFrame f) => new(Left(f), 0, f.Px(ListWidth), Foot(f));

    // ------------------------------------------------------------------ the title band (ll1, load_save_chips)

    /// <summary>The title's left (in from the list's), its top and type (the brush face); the divider under the band.</summary>
    public const float TitleIn = 4, TitleTop = 14, TitleText = 56, Divider = 110;

    /// <summary>Load | Save: 200 × 80, 12 apart, 18 from the top, the pair ending at the list's right (from 412 short of it).</summary>
    private const float ChipWidth = 200, ChipHeight = 80, ChipGap = 12, ChipTop = 18;

    /// <summary>The chips' type (Cinzel).</summary>
    public const float ChipText = 32;

    /// <summary>Load (<paramref name="save"/> false) or Save at the band's right, from <c>COMMAND</c>.</summary>
    public static PhoneFrame.Box Chip(PhoneFrame f, bool save)
    {
        float right = List(f).Right;
        float x = right - f.Px(2 * ChipWidth + ChipGap) + (save ? f.Px(ChipWidth + ChipGap) : 0);
        return new PhoneFrame.Box(x, f.Px(ChipTop), f.Px(ChipWidth), f.Px(ChipHeight));
    }

    // ------------------------------------------------------------------ the list (land_save_list, save_row)

    /// <summary>The lists' tabs: 96 high from 124, 12 apart, across the list.</summary>
    private const float TabsTop = 124, TabsHeight = 96, TabsGap = 12;

    /// <summary>The tabs' type (Cinzel).</summary>
    public const float TabText = 34;

    /// <summary>The manual saves' tab (<paramref name="auto"/> false) or the auto-saves'.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, bool auto)
    {
        var list = List(f);
        float w = (list.W - f.Px(TabsGap)) / 2;
        return new PhoneFrame.Box(list.X + (auto ? w + f.Px(TabsGap) : 0), f.Px(TabsTop), w, f.Px(TabsHeight));
    }

    /// <summary>The heading 16 under the tabs, 64 high; the sweep 440 wide at its right.</summary>
    private const float HeadTop = TabsTop + TabsHeight + 16, HeadHeight = 64, SweepWidth = 440;

    /// <summary>The heading's words: in from the list's left, their type (Cinzel); the sweep's type and its star's radius.</summary>
    public const float HeadIn = 8, HeadText = 26, SweepText = 28, SweepStar = 12;

    /// <summary>The heading's middle ("NEWEST FIRST" at the left).</summary>
    public static float HeadMiddle(PhoneFrame f) => f.Px(HeadTop + HeadHeight / 2);

    /// <summary>The sweep, "Delete all without ★ (n)", at the heading's right.</summary>
    public static PhoneFrame.Box Sweep(PhoneFrame f) =>
        new(List(f).Right - f.Px(SweepWidth), f.Px(HeadTop), f.Px(SweepWidth), f.Px(HeadHeight));

    /// <summary>
    /// The rows: from 20 under the heading, 104 high and 10 apart, ending short of the list's right by the scroll bar's
    /// room (the reference, <see cref="PhoneScrollBar"/>: its right at the list's); New save 88 high over them in Save's
    /// manual saves.
    /// </summary>
    private const float RowsTop = HeadTop + HeadHeight + 20, RowHeight = 104, RowGap = 10, NewHeight = 88;

    private static float FirstRow(PhoneFrame f, bool newRow) => f.Px(RowsTop + (newRow ? NewHeight + RowGap : 0));

    private static float RowWidth(PhoneFrame f) => f.Px(ListWidth) - PhoneScrollBar.Room(f);

    /// <summary>New save over the rows (Save's manual saves).</summary>
    public static PhoneFrame.Box New(PhoneFrame f) => new(Left(f), f.Px(RowsTop), RowWidth(f), f.Px(NewHeight));

    /// <summary>A row's pitch: its height and the gap under it.</summary>
    public static float Pitch(PhoneFrame f) => f.Px(RowHeight + RowGap);

    /// <summary>How many rows are in view, under New save when <paramref name="newRow"/>.</summary>
    public static int RowsShown(PhoneFrame f, bool newRow) =>
        Math.Max(1, (int)Math.Floor((Foot(f) - FirstRow(f, newRow) + f.Px(RowGap)) / Pitch(f) + 0.0001f));

    /// <summary>The row at <paramref name="place"/> in view (0: the top one).</summary>
    public static PhoneFrame.Box Row(PhoneFrame f, int place, bool newRow) =>
        new(Left(f), FirstRow(f, newRow) + place * Pitch(f), RowWidth(f), f.Px(RowHeight));

    /// <summary>The scroll bar's groove right of the rows in view.</summary>
    public static PhoneFrame.Box ScrollBar(PhoneFrame f, bool newRow)
    {
        float top = FirstRow(f, newRow);
        return PhoneScrollBar.Groove(f, List(f).Right, top, RowsShown(f, newRow) * Pitch(f) - f.Px(RowGap));
    }

    /// <summary>Where a finger scrolls the list: New save and the rows in view, the list's width.</summary>
    public static PhoneFrame.Box Scrolled(PhoneFrame f, bool newRow)
    {
        var bar = ScrollBar(f, newRow);
        float top = f.Px(RowsTop);
        return new PhoneFrame.Box(Left(f), top, f.Px(ListWidth), bar.Bottom - top);
    }

    /// <summary>
    /// A row's insides in one line (<c>save_row</c>): the seal 20 in, 64 square, centred, a comment's star at its top left; the
    /// short date's left and type; the time written ending this far in from the right, its type; an auto-save's kind on its
    /// tag (its type), standing this far left of the time.
    /// </summary>
    public const float SealIn = 20, Seal = 64, Star = 16, DateIn = 108, DateText = 42, WrittenIn = 22, WrittenText = 32,
        RingText = 26, RingGap = 24;

    /// <summary>"No manual saves yet." and the like: this far under the rows' top, their type.</summary>
    public const float EmptyDown = 80, EmptyText = 30;

    // ------------------------------------------------------------------ the page (land_save_page)

    /// <summary>The page's top; its title band (the title's middle in it, its type), Delete at its right (240 × 84, its type).</summary>
    private const float PageTop = 14, TitleBand = 100, DeleteWidth = 240, DeleteHeight = 84;

    /// <summary>The page's title: its middle under the page's top, its type (Cinzel), the auto-save's tags' type.</summary>
    public const float PageTitleMiddle = 40, PageTitleText = 36, TagText = 28, DeleteText = 30;

    /// <summary>The foot's buttons: 150 high; Back 360 wide, the red button 16 right of it to the page's right; the page 16 over them.</summary>
    private const float FootHeight = 150, BackWidth = 360, FootGap = 16;

    /// <summary>Back's and the red button's type (the brush face), the red button's line under its word.</summary>
    public const float BackText = 50, ButtonText = 64, ButtonLineText = 28;

    /// <summary>The page at the right: 40 right of the list to the hole's strip, from its top to 16 over the foot's buttons.</summary>
    public static PhoneFrame.Box Page(PhoneFrame f)
    {
        float x = List(f).Right + f.Px(PageGap), top = f.Px(PageTop);
        return new PhoneFrame.Box(x, top, Right(f) - x, Foot(f) - f.Px(FootHeight + FootGap) - top);
    }

    /// <summary>The page title's middle.</summary>
    public static float TitleMiddle(PhoneFrame f) => Page(f).Y + f.Px(PageTitleMiddle);

    /// <summary>Delete at the title's right (the owner's B5).</summary>
    public static PhoneFrame.Box Delete(PhoneFrame f)
    {
        var page = Page(f);
        return new PhoneFrame.Box(page.Right - f.Px(DeleteWidth), page.Y, f.Px(DeleteWidth), f.Px(DeleteHeight));
    }

    /// <summary>All Data's map, 1280 × 533: the page's width shows it this high whole.</summary>
    public const float MapShape = 533f / 1280f;

    /// <summary>
    /// Under the map: 18, the figures' two rows 92 apart, 10, the comment's band (its label, 36, the box 100 high); an
    /// auto-save's lines 14 under the box, 40 apart.
    /// </summary>
    private const float FiguresDown = 18, FigureRows = 2, FigurePitch = 92, CommentDown = 10, CommentBand = 136,
        BoxDown = 36, BoxHeight = 100, LinesDown = 14, LinePitch = 40, Lines = 2;

    /// <summary>The figures' columns.</summary>
    public const int FigureColumns = 3;

    /// <summary>A figure: its label 8 in at its type (Cinzel), the value 32 under it at its type, shrunk to the column less 48.</summary>
    public const float FigureIn = 8, LabelText = 24, ValueDown = 32, ValueText = 38, ValueRoom = 48;

    /// <summary>The comment's box: its words 24 in at their type; Edit 204 wide at the page's right, 16 right of the box (its type).</summary>
    public const float CommentIn = 24, CommentText = 38, EditText = 40, LineText = 28;

    private const float EditWidth = 204, EditGap = 16;

    private static float Under(bool lines) =>
        FiguresDown + FigureRows * FigurePitch + CommentDown + CommentBand + (lines ? LinesDown + Lines * LinePitch : 0);

    /// <summary>
    /// The map at the page's width under the title, its foot cut off where the figures, the comment (and with
    /// <paramref name="lines"/> an auto-save's lines) need the room; never taller than the whole map.
    /// </summary>
    public static PhoneFrame.Box Map(PhoneFrame f, bool lines)
    {
        var page = Page(f);
        float top = page.Y + f.Px(TitleBand);
        float room = page.Bottom - top - f.Px(Under(lines));
        return new PhoneFrame.Box(page.X, top, page.W, Math.Max(0, Math.Min(page.W * MapShape, room)));
    }

    /// <summary>The figure <paramref name="index"/>'s label's top left (its value <see cref="ValueDown"/> under it) and its column's width.</summary>
    public static (float X, float Y, float W) Figure(PhoneFrame f, int index, bool lines)
    {
        var map = Map(f, lines);
        float w = map.W / FigureColumns;
        return (map.X + f.Px(FigureIn) + index % FigureColumns * w, map.Bottom + f.Px(FiguresDown) + index / FigureColumns * f.Px(FigurePitch), w);
    }

    /// <summary>The comment's label's top ("COMMENT", the figures' label type).</summary>
    public static float CommentLabel(PhoneFrame f, bool lines) => Map(f, lines).Bottom + f.Px(FiguresDown + FigureRows * FigurePitch + CommentDown);

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

    /// <summary>Back at the page's foot, at its right — the right thumb: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box Back(PhoneFrame f) =>
        new(Right(f) - f.Px(BackWidth), Foot(f) - f.Px(FootHeight), f.Px(BackWidth), f.Px(FootHeight));

    /// <summary>The red button (Load, Save, Save over No. n) from the page's left to 16 short of Back.</summary>
    public static PhoneFrame.Box Button(PhoneFrame f)
    {
        var back = Back(f);
        float x = Page(f).X;
        return new PhoneFrame.Box(x, back.Y, back.X - f.Px(FootGap) - x, back.H);
    }

    // ------------------------------------------------------------------ the questions (the PC's callouts at the phone's sizes)

    /// <summary>A question's callout: its lines 32 in, their middles and type; its answers 84 high, 24 in from its foot and right, 16 apart.</summary>
    public const float QuestionIn = 32, QuestionFirst = 56, QuestionSecond = 104, QuestionText = 32, AnswerText = 40, Tip = 24;

    private const float QuestionHeight = 250, AnswerHeight = 84, AnswerIn = 24, AnswerGap = 16, YesWidth = 260, NoWidth = 180;

    /// <summary>Delete's question: 900 wide, hanging 28 under Delete, at the page's right (its tip at Delete's middle).</summary>
    private const float DeleteQuestionWidth = 900, DeleteQuestionDown = 28;

    /// <summary>The sweep's question: 1000 wide, right of the sweep in the gap to the page (its tip at the sweep's middle).</summary>
    private const float SweepQuestionWidth = 1000;

    /// <summary>Delete's question, under Delete.</summary>
    public static PhoneFrame.Box DeleteQuestion(PhoneFrame f)
    {
        var delete = Delete(f);
        return new PhoneFrame.Box(delete.Right - f.Px(DeleteQuestionWidth), delete.Bottom + f.Px(DeleteQuestionDown), f.Px(DeleteQuestionWidth),
            f.Px(QuestionHeight));
    }

    /// <summary>The sweep's question, right of the sweep, never above the page's top.</summary>
    public static PhoneFrame.Box SweepQuestion(PhoneFrame f)
    {
        var sweep = Sweep(f);
        float y = Math.Max(f.Safe.Top + f.Px(PageTop), sweep.Y + sweep.H / 2 - f.Px(QuestionHeight) / 2);
        return new PhoneFrame.Box(Page(f).X, y, f.Px(SweepQuestionWidth), f.Px(QuestionHeight));
    }

    /// <summary>A question's answer: Delete (<paramref name="yes"/>, red) left of Keep, at the callout's foot right.</summary>
    public static PhoneFrame.Box Answer(PhoneFrame f, PhoneFrame.Box question, bool yes)
    {
        float y = question.Bottom - f.Px(AnswerIn + AnswerHeight);
        float no = question.Right - f.Px(AnswerIn + NoWidth);
        return yes ? new PhoneFrame.Box(no - f.Px(AnswerGap + YesWidth), y, f.Px(YesWidth), f.Px(AnswerHeight))
            : new PhoneFrame.Box(no, y, f.Px(NoWidth), f.Px(AnswerHeight));
    }

    // ------------------------------------------------------------------ the comment's card (ly1)

    /// <summary>
    /// Android's keyboard held landscape where the system reports none (a PC playing a phone, or before it is up): Gboard
    /// with its suggestions and the gesture bar, 650 of the 1220 (the mock-up's <c>LKB_TOP</c>).
    /// </summary>
    public const float KeyboardFallback = 650;

    /// <summary>The card: 360 high, 24 over the keyboard, across the room; never above 14 under the screen's top.</summary>
    private const float CardOver = 24, CardHeight = 360, CardTop = 14;

    /// <summary>The card's words: "COMMENT" and whose it is, their left, middle and type (Cinzel).</summary>
    public const float CardLabelIn = 32, CardLabelMiddle = 54, CardLabelText = 28, CardTitleIn = 220, CardTitleText = 30;

    /// <summary>The field: 24 in, 100 under the card's top, 130 high, to 32 short of the buttons; its type; the count under its right.</summary>
    public const float FieldText = 46, CountDown = 40, CountText = 32, CancelText = 48, KeepText = 56;

    private const float FieldIn = 24, FieldDown = 100, FieldHeight = 130, FieldGap = 32, CardButton = 330, CardButtonIn = 24;

    /// <summary>
    /// The comment's own card just over the keyboard <paramref name="keyboard"/> high (the canvas's units; 0 or less: none
    /// reported, the reference's <see cref="KeyboardFallback"/>), across the room between the hole's strips.
    /// </summary>
    public static PhoneFrame.Box Card(PhoneFrame f, float keyboard)
    {
        float top = f.Screen.H - (keyboard > 0 ? keyboard : f.Px(KeyboardFallback));
        float y = Math.Max(f.Safe.Top + f.Px(CardTop), top - f.Px(CardOver + CardHeight));
        return new PhoneFrame.Box(Left(f), y, Right(f) - Left(f), f.Px(CardHeight));
    }

    /// <summary>Cancel at the card's right — the right thumb: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box Cancel(PhoneFrame f, float keyboard)
    {
        var card = Card(f, keyboard);
        return new PhoneFrame.Box(card.Right - f.Px(CardButtonIn + CardButton), card.Y + f.Px(FieldDown), f.Px(CardButton), f.Px(FieldHeight));
    }

    /// <summary>Keep, red, left of Cancel, 32 right of the field.</summary>
    public static PhoneFrame.Box Keep(PhoneFrame f, float keyboard)
    {
        var field = Field(f, keyboard);
        return new PhoneFrame.Box(field.Right + f.Px(FieldGap), field.Y, f.Px(CardButton), field.H);
    }

    /// <summary>The field in one line, from 24 in to 32 short of Keep.</summary>
    public static PhoneFrame.Box Field(PhoneFrame f, float keyboard)
    {
        var card = Card(f, keyboard);
        float x = card.X + f.Px(FieldIn);
        float right = card.Right - f.Px(FieldGap + 2 * CardButton + FieldGap);
        return new PhoneFrame.Box(x, card.Y + f.Px(FieldDown), right - x, f.Px(FieldHeight));
    }

    /// <summary>The count's right end and top ("25 / 60"), under the field's right.</summary>
    public static (float Right, float Top) Count(PhoneFrame f, float keyboard)
    {
        var field = Field(f, keyboard);
        return (field.Right, field.Bottom + f.Px(CountDown));
    }
}
