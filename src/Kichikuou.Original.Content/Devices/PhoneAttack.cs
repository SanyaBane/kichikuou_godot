namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our enemy attack screen on the phone held landscape (docs/ui/phone-build.md, step L6; docs/ui/phone.md, §14: LA2 and
/// LA2n with the owner's changes): across the whole width between the hole's strips, the facts at the left — the defended
/// province's panel (<see cref="Province"/>: its picture and four figures) and beyond "VS" the attacking army's
/// (<see cref="Army"/>: the four places' cards two a row, the front row left, held in the bracket facing the province,
/// <see cref="Bracket"/>; the totals and the
/// report's source under them), both panels' content in their middle, their captions at their top — and a column of its own
/// at the right thumb (<see cref="Column"/>): the title, the date and the news, Maris's question, then Abandon City (10%
/// narrower and lower) | Ninja Scout, and Intercept, half as high as first drawn, at the thumb. Ninja Scout's list opens
/// above its button over Maris's question, the column's width, its ninjas two a row (all five the game has in view). The
/// figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>la2_base</c>, <c>la2n</c>, <c>ys_panel</c>,
/// <c>rebels_panel</c>, <c>attacker_card</c>, <c>maris</c>, <c>ninja_callout</c>) in the reference phone's pixels, the boxes
/// in the canvas's units; a longer screen widens the army's panel (the province's panel and the column keep their width).
/// Interface only. Engine-independent.
/// </summary>
public static class PhoneAttack
{
    // ------------------------------------------------------------------ the split

    /// <summary>The room's edges in from the hole's strips; the panels' top and how far over the screen's foot they end.</summary>
    private const float ScreenIn = 6, PanelsTop = 16, PanelsFoot = 16;

    /// <summary>
    /// The scouting ninja's seal (the owner's LK2 with a 90 square of the dump's, docs/ui/enemy-attack.md, §14): her face
    /// square 135 in the army panel's top right corner, 25 in from its right and 21 down; "SCOUTED BY" (24) on the panel's
    /// caption's baseline and her name (42) 55 under it, both right-aligned 24 left of the square.
    /// </summary>
    public const float SealSize = 135, SealIn = 25, SealTop = 21, SealWordsGap = 24, SealLabel = 24, SealName = 42, SealNameDrop = 55;

    /// <summary>The scouting ninja's face square in the army panel's top right corner.</summary>
    public static PhoneFrame.Box Seal(PhoneFrame f)
    {
        var army = Army(f);
        return new PhoneFrame.Box(army.Right - f.Px(SealIn + SealSize), army.Y + f.Px(SealTop), f.Px(SealSize), f.Px(SealSize));
    }

    /// <summary>The province's panel's width, the room "VS" stands in after it, the column's width and the gap before it.</summary>
    private const float ProvinceWidth = 580, VsRoom = 100, ColumnWidth = 804, ColumnGap = 16;

    /// <summary>A panel's caption: in from its left, down from its top, its type.</summary>
    public const float CaptionIn = 28, CaptionTop = 26, CaptionText = 28;

    /// <summary>"VS" between the panels: its type.</summary>
    public const float VsText = 40;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ScreenIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ScreenIn);

    private static float Top(PhoneFrame f) => f.Px(PanelsTop);

    private static float Foot(PhoneFrame f) => f.Screen.H - f.Px(PanelsFoot);

    /// <summary>The defended province's panel, from the hole's strip down the whole height.</summary>
    public static PhoneFrame.Box Province(PhoneFrame f) => new(Left(f), Top(f), f.Px(ProvinceWidth), Foot(f) - Top(f));

    /// <summary>"VS"'s middle: in the room between the panels, half way down.</summary>
    public static (float X, float Y) Vs(PhoneFrame f) => (Left(f) + f.Px(ProvinceWidth + VsRoom / 2), (Top(f) + Foot(f)) / 2);

    /// <summary>The attacking army's panel, from beyond "VS" to the column.</summary>
    public static PhoneFrame.Box Army(PhoneFrame f)
    {
        float x0 = Left(f) + f.Px(ProvinceWidth + VsRoom), x1 = Column(f).X - f.Px(ColumnGap);
        return new PhoneFrame.Box(x0, Top(f), x1 - x0, Foot(f) - Top(f));
    }

    /// <summary>The column at the right thumb: 804 wide, ending at the hole's strip, the screen's height over the gesture zone.</summary>
    public static PhoneFrame.Box Column(PhoneFrame f) =>
        new(Right(f) - f.Px(ColumnWidth), 0, f.Px(ColumnWidth), f.Screen.H - f.Safe.Bottom);

    // ------------------------------------------------------------------ the province's panel (ys_panel)

    /// <summary>The picture: as wide as this, the PC's 3:2; the figures under it 18 lower, their rows, 30 in from the panel's sides.</summary>
    private const float PictureWidth = 540, PictureShape = 200f / 300f, PictureUnder = 18, FigureRow = 88, FigureIn = 30, ContentTop = 76;

    /// <summary>A figure's label and value type.</summary>
    public const float FigureLabel = 31, FigureValue = 38;

    /// <summary>The figures the panel lists under the picture.</summary>
    public const int Figures = 4;

    /// <summary>The province's picture: centred in the panel with its figures (the owner's change), never over the caption's room.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        var panel = Province(f);
        float w = f.Px(PictureWidth), h = w * PictureShape;
        float block = h + f.Px(PictureUnder + Figures * FigureRow);
        float top = Math.Max(panel.Y + f.Px(ContentTop), panel.Y + (panel.H - block) / 2);
        return new PhoneFrame.Box(panel.X + (panel.W - w) / 2, top, w, h);
    }

    /// <summary>The figure row <paramref name="i"/> (0-3) under the picture: the label at its left, the value at its right, a rule at its foot.</summary>
    public static PhoneFrame.Box Figure(PhoneFrame f, int i)
    {
        var panel = Province(f);
        float top = Picture(f).Bottom + f.Px(PictureUnder + i * FigureRow);
        return new PhoneFrame.Box(panel.X + f.Px(FigureIn), top, panel.W - f.Px(2 * FigureIn), f.Px(FigureRow));
    }

    // ------------------------------------------------------------------ the army's panel (rebels_panel, attacker_card)

    /// <summary>
    /// The cards: in from the panel's left past the bracket pointing at the province (the owner's LA2f1, docs/ui/phone.md,
    /// §47: its head's tip 24 in, where LA2's pointer had its own, its line <see cref="PhoneBracket.HeadTip"/> right of it and
    /// <see cref="PhoneBracket.Beside"/> off the cards), 24 short of its right, 16 apart in a row and 14 in a column, 300
    /// high; the rows' captions 40 over them; the totals 20 under them, their block 106 high.
    /// </summary>
    private const float TipIn = 24, CardsIn = TipIn + PhoneBracket.HeadTip + PhoneBracket.Beside, CardsRight = 24, CardColumns = 16,
        CardRows = 14, CardHeight = 300, CaptionsRoom = 40, TotalsUnder = 20, TotalsBlock = 106;

    /// <summary>The rows' captions' middle over the cards and their type.</summary>
    public const float RowCaptionAbove = 26, RowCaption = 26;

    /// <summary>
    /// The totals, reference pixels from their top (<see cref="Totals"/>): their middle, the labels' and figures' type, the
    /// commanders' figure's place after the cards' left, the rule, the report's note's top and type; 28 short of the panel's right.
    /// </summary>
    public const float TotalsMiddle = 28, TotalsLabel = 24, TotalsText = 38, CountAt = 210, TotalsRule = 62, NoteTop = 76, NoteText = 30,
        TotalsRight = 28;

    /// <summary>A card's face: at most this large, as high as the card less 12 at the top and the foot.</summary>
    public const float CardFace = 170;

    private static float CardsTop(PhoneFrame f)
    {
        var panel = Army(f);
        float block = f.Px(CaptionsRoom + 2 * CardHeight + CardRows + TotalsUnder + TotalsBlock);
        return Math.Max(panel.Y + f.Px(ContentTop), panel.Y + (panel.H - block) / 2) + f.Px(CaptionsRoom);
    }

    /// <summary>Place <paramref name="slot"/>'s card: the front row (1, 2) left, nearer the province, the back row (3, 4) right; 1 and 3 on top.</summary>
    public static PhoneFrame.Box Card(PhoneFrame f, int slot)
    {
        var panel = Army(f);
        float x0 = panel.X + f.Px(CardsIn);
        float w = (panel.Right - f.Px(CardsRight) - x0 - f.Px(CardColumns)) / 2;
        float x = x0 + (slot is 3 or 4 ? w + f.Px(CardColumns) : 0);
        float y = CardsTop(f) + (slot is 2 or 4 ? f.Px(CardHeight + CardRows) : 0);
        return new PhoneFrame.Box(x, y, w, f.Px(CardHeight));
    }

    /// <summary>The middle of a column's caption ("FRONT ROW" over 1-2, "BACK ROW" over 3-4).</summary>
    public static (float X, float Y) RowCaptionAt(PhoneFrame f, int column)
    {
        var card = Card(f, 1 + 2 * column);
        return (card.X + card.W / 2, card.Y - f.Px(RowCaptionAbove));
    }

    /// <summary>
    /// The bracket left of the front row, its head pointing at the province (the owner's LA2f1): its ends at the top (1 in)
    /// of place 1's card and the foot of place 2's, its middle theirs, level with the gap between the rows; its line
    /// <see cref="PhoneBracket.Beside"/> left of the cards' left side, which its ends come up to.
    /// </summary>
    public static (float Y0, float Y1, float Middle, float Line, float CardsSide) Bracket(PhoneFrame f)
    {
        var top = Card(f, 1);
        float y0 = top.Y + f.Px(1), y1 = Card(f, 2).Bottom - f.Px(1);
        return (y0, y1, (y0 + y1) / 2, top.X - f.Px(PhoneBracket.Beside), top.X);
    }

    /// <summary>The totals under the cards: from the cards' left to 28 short of the panel's right; their top, 20 under the cards.</summary>
    public static (float X0, float X1, float Top) Totals(PhoneFrame f) =>
        (Card(f, 1).X, Army(f).Right - f.Px(TotalsRight), Card(f, 2).Bottom + f.Px(TotalsUnder));

    // ------------------------------------------------------------------ the column (la2_base, maris)

    /// <summary>
    /// The column's head, reference pixels from the screen's top: the attacker's flag (its size, in from the column's left,
    /// its middle), the title's left (in from the column's) and top, its type; the date's top (right) and type; the news's two
    /// lines' tops and type; the divider.
    /// </summary>
    public const float FlagIn = 4, FlagSize = 70, FlagMiddle = 59, TitleIn = 92, TitleTop = 16, TitleText = 56, DateTop = 108,
        DateText = 28, NewsIn = 4, NewsTop = 166, NewsPitch = 50, NewsText = 40, Divider = 286;

    /// <summary>The answers from the foot up: Intercept's height 10 over the gesture zone, the room over it, the row's height (Ninja Scout's), Abandon City's inset in the row (10% lower) and width (90% of its half).</summary>
    private const float AnswersFoot = 10, InterceptHeight = 130, AnswersGap = 16, RowHeight = 180, AbandonInset = 18, AbandonShare = 0.9f;

    /// <summary>
    /// The answers' type: Intercept's, Ninja Scout's and Abandon City's words, Cinzel 700 at one size (docs/ui/style.md,
    /// "Buttons side by side"), shrunk together where a word is too long for its box; Intercept's note (two lines closer on a
    /// low button) and Ninja Scout's.
    /// </summary>
    public const float ButtonText = 42, InterceptNote = 26, ScoutNote = 28;

    /// <summary>Intercept, as wide as the column, at the thumb.</summary>
    public static PhoneFrame.Box Intercept(PhoneFrame f)
    {
        var column = Column(f);
        float foot = column.Bottom - f.Px(AnswersFoot);
        return new PhoneFrame.Box(column.X, foot - f.Px(InterceptHeight), column.W, f.Px(InterceptHeight));
    }

    private static float RowTop(PhoneFrame f) => Intercept(f).Y - f.Px(AnswersGap + RowHeight);

    private static float Half(PhoneFrame f) => (Column(f).W - f.Px(AnswersGap)) / 2;

    /// <summary>Ninja Scout: the row's right half, over Intercept.</summary>
    public static PhoneFrame.Box NinjaScout(PhoneFrame f)
    {
        var column = Column(f);
        float x = column.X + Half(f) + f.Px(AnswersGap);
        return new PhoneFrame.Box(x, RowTop(f), column.Right - x, f.Px(RowHeight));
    }

    /// <summary>Abandon City: the row's left half less a tenth, 18 in from the row's top and foot (the owner's changes).</summary>
    public static PhoneFrame.Box Abandon(PhoneFrame f) =>
        new(Column(f).X, RowTop(f) + f.Px(AbandonInset), (float)Math.Round(Half(f) * AbandonShare), f.Px(RowHeight - 2 * AbandonInset));

    /// <summary>Maris's question: 320 high, 16 over the answers' row.</summary>
    private const float AdvisorHeight = 320;

    /// <summary>Her panel's insides: her face (140, in from its left and top), the text's left in from the panel's, "MARIS"'s top and type, the lines' first top, type and pitch.</summary>
    public const float AdvisorFace = 140, AdvisorFaceIn = 24, AdvisorFaceTop = 20, AdvisorTextIn = 194, AdvisorNameTop = 26,
        AdvisorName = 28, AdvisorLinesTop = 66, AdvisorText = 40, AdvisorPitch = 1.22f, AdvisorRight = 24;

    /// <summary>Maris's panel with her question, over the answers.</summary>
    public static PhoneFrame.Box Advisor(PhoneFrame f)
    {
        var column = Column(f);
        float foot = RowTop(f) - f.Px(AnswersGap);
        return new PhoneFrame.Box(column.X, foot - f.Px(AdvisorHeight), column.W, f.Px(AdvisorHeight));
    }

    // ------------------------------------------------------------------ Ninja Scout's list (la2n, ninja_callout)

    /// <summary>
    /// The list: 30 over its button, the column's width; its head ("SEND TO SCOUT", its middle and type), its rows (128 high,
    /// 12 apart; two columns 16 apart when there is more than one ninja), 24 in, the room under them.
    /// </summary>
    private const float ListAbove = 30, ListHead = 86, ListRow = 128, ListGap = 12, ListColumns = 16, ListIn = 24, ListFoot = 24;

    /// <summary>The list's head's middle, its left and type; the tip's depth over the button and its half width.</summary>
    public const float ListTitleMiddle = 46, ListTitleIn = 30, ListTitle = 28, TipAbove = 6, TipHalf = 26;

    /// <summary>A row's insides: the face (as high as the row less 12 at its top and foot), the text right of it, the name's and the state's middles (of the row's height) and type.</summary>
    public const float RowFaceIn = 12, RowTextGap = 12, RowNameMiddle = 0.31f, RowName = 40, RowStateMiddle = 0.68f, RowState = 32;

    private static int Columns(int ninjas) => ninjas > 1 ? 2 : 1;

    /// <summary>The list of <paramref name="ninjas"/> ninjas: above Ninja Scout, the column's width, as high as its rows.</summary>
    public static PhoneFrame.Box List(PhoneFrame f, int ninjas)
    {
        var column = Column(f);
        int rows = Math.Max(1, (ninjas + Columns(ninjas) - 1) / Columns(ninjas));
        float h = f.Px(ListHead + rows * ListRow + (rows - 1) * ListGap + ListFoot);
        float foot = NinjaScout(f).Y - f.Px(ListAbove);
        return new PhoneFrame.Box(column.X, foot - h, column.W, h);
    }

    /// <summary>Ninja <paramref name="i"/>'s row of <paramref name="ninjas"/>, in the scout menu's order, two a row.</summary>
    public static PhoneFrame.Box ListRowBox(PhoneFrame f, int i, int ninjas)
    {
        var list = List(f, ninjas);
        int columns = Columns(ninjas);
        float w = (list.W - f.Px(2 * ListIn + (columns - 1) * ListColumns)) / columns;
        return new PhoneFrame.Box(list.X + f.Px(ListIn) + i % columns * (w + f.Px(ListColumns)),
            list.Y + f.Px(ListHead) + i / columns * f.Px(ListRow + ListGap), w, f.Px(ListRow));
    }
}
