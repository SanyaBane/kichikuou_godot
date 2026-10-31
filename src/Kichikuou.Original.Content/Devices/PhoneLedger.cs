namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The deployment table and the Subordinates screen on the phone held landscape (docs/ui/phone-build.md, step L5;
/// docs/ui/phone.md, §13: LD2, LU1, LU1q, the owner's changes): the unit ledger in one-line rows at the phone's type —
/// rows 6.4 mm (<see cref="Row"/>), every column of a tab in one line, the Name cell's face and troops' bar
/// (<see cref="NameFace"/>), the tabs over it. On the deployment table the
/// ledger stands wholly at the left, from the hole's strip (<see cref="Ledger"/>), and everything else in a part of its
/// own at the right (<see cref="Part"/>): the title, the date and the budget over the party two cards a row as the battle
/// shows our army (the back row left), the troops and the war expenses under it, Attack | Stop at the right thumb. On
/// Subordinates the ledger fills the frame; the left side holds the chosen unit's Add New Troops from the foot up, the
/// dearest on top (<see cref="Steps"/>); the right side the title, the date and the budget, then Talk, Replenish and End
/// at the thumb, Replenish's question in a callout left of it (<see cref="ReplenishQuestion"/>). The figures are the
/// mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>ld2</c>, <c>lu1</c>, <c>land_ledger</c>, <c>party_card</c>,
/// <c>lu_orders_side</c>) in the reference phone's pixels, the boxes in the canvas's units; a longer screen widens the
/// deployment's ledger (the right part keeps its width) and the sides. Interface only. Engine-independent.
/// </summary>
public static class PhoneLedger
{
    // ------------------------------------------------------------------ the ledger and its tabs

    /// <summary>
    /// A ledger's row (6.4 mm), its type (the names' 2.3 mm; the figures the same in the tree, one size a cell), the room
    /// between its columns.
    /// </summary>
    public const float Row = 112, Text = 40, ColumnGap = 18;

    /// <summary>
    /// The Name cell (owner, 2026-10-31): the unit's face left of the name, its square's side (5 mm) and the room after it;
    /// the name stands over a bar of the troops' strength without figures, drawn as the Troops cell draws its own.
    /// </summary>
    public const float NameFace = 88, NameFaceGap = 14;

    /// <summary>The ledger's paper: its top under the tabs, its foot over the screen's edge, in from the hole's strip or the frame.</summary>
    private const float LedgerTop = 104, LedgerFoot = 12, ScreenIn = 6, FrameIn = 12;

    /// <summary>The tabs: their top, width, the room between them, their height and type, how much lower an unchosen one starts; the order's seal's type.</summary>
    public const float TabsTop = 16, TabWidth = 300, TabGap = 12, TabHeight = 88, TabText = 34, TabDrop = 10, OrderText = 26;

    /// <summary>
    /// The ledger's paper (<paramref name="deployment"/>: the deployment table's, from the hole's strip to the right part;
    /// else Subordinates', the frame's width less 12 on each side), under the tabs and down to 12 over the screen's foot.
    /// </summary>
    public static PhoneFrame.Box Ledger(PhoneFrame f, bool deployment)
    {
        float x0 = deployment ? f.Safe.Left + f.Px(ScreenIn) : f.Frame.X + f.Px(FrameIn);
        float x1 = deployment ? Part(f).X - f.Px(PartGap) : f.Frame.Right - f.Px(FrameIn);
        float top = f.Px(LedgerTop);
        return new PhoneFrame.Box(x0, top, x1 - x0, f.Screen.H - f.Px(LedgerFoot) - top);
    }

    /// <summary>The tabs' row over <see cref="Ledger"/>: as wide as the paper, from the tabs' top to the paper.</summary>
    public static PhoneFrame.Box Tabs(PhoneFrame f, bool deployment)
    {
        var ledger = Ledger(f, deployment);
        return new PhoneFrame.Box(ledger.X, f.Px(TabsTop), ledger.W, ledger.Y - f.Px(TabsTop));
    }

    // ------------------------------------------------------------------ the deployment table's right part (LD2)

    /// <summary>The right part: its width (from the split's 1716 to the hole's strip less 6), the room between it and the ledger.</summary>
    private const float PartWidth = 880, PartGap = 16;

    /// <summary>
    /// The right part's lines, reference pixels from the screen's top: the title's top and type, the date's (right), the
    /// subtitle's, the budget's middle (right; the figure's type, the labels'), the divider, the rows' captions' middle and type.
    /// </summary>
    public const float TitleTop = 14, TitleText = 56, DateTop = 30, DateText = 28, SubtitleTop = 92, SubtitleText = 26,
        BudgetMiddle = 108, BudgetText = 36, LabelText = 24, Divider = 146, CaptionMiddle = 180, CaptionText = 24;

    /// <summary>The party's cards: their top, height, the room between them; the totals' middle under them and their type.</summary>
    private const float CardsTop = 200, CardHeight = 214, CardGap = 12, TotalsUnder = 46;

    /// <summary>The totals' label and figure type.</summary>
    public const float TotalsLabel = 26, TotalsText = 40;

    /// <summary>The bar's buttons: their height over the gesture zone (10 above it), Stop's width, the room between buttons; their type.</summary>
    private const float BarHeight = 150, BarFoot = 10, StopWidth = 300, BarGap = 16;

    /// <summary>The bar's type: Stop's, a send button's alone, two sends' (a defence's Plains and City), a send's note.</summary>
    public const float StopText = 44, SendText = 60, SendsText = 46, NoteText = 28;

    /// <summary>The deployment's right part, the screen's height over the gesture zone.</summary>
    public static PhoneFrame.Box Part(PhoneFrame f)
    {
        float x1 = f.Screen.W - f.Safe.Right - f.Px(ScreenIn);
        return new PhoneFrame.Box(x1 - f.Px(PartWidth), 0, f.Px(PartWidth), f.Screen.H - f.Safe.Bottom);
    }

    /// <summary>
    /// Place <paramref name="place"/>'s card as the battle shows our army: the back row (3, 4) left, the front row (1, 2)
    /// right, places 1 and 3 on top; two a row, 12 apart.
    /// </summary>
    public static PhoneFrame.Box Card(PhoneFrame f, int place)
    {
        var part = Part(f);
        float w = (part.W - f.Px(CardGap)) / 2;
        float x = part.X + (place is 1 or 2 ? w + f.Px(CardGap) : 0);
        float y = f.Px(CardsTop) + (place is 2 or 4 ? f.Px(CardHeight + CardGap) : 0);
        return new PhoneFrame.Box(x, y, w, f.Px(CardHeight));
    }

    /// <summary>The totals' middle (the troops left, the war expenses right): under the cards.</summary>
    public static float TotalsMiddle(PhoneFrame f) => Card(f, 2).Bottom + f.Px(TotalsUnder);

    /// <summary>
    /// The bar's buttons: the <paramref name="sends"/> buttons that send the party from the left, sharing the room, and Stop
    /// (300 wide) at the right — a way back stands right of the action (owner, 2026-10-30) —, 150 high over the gesture zone.
    /// Stop is the first box, a send's follow.
    /// </summary>
    public static IReadOnlyList<PhoneFrame.Box> Bar(PhoneFrame f, int sends)
    {
        var part = Part(f);
        float foot = f.Screen.H - f.Safe.Bottom - f.Px(BarFoot), h = f.Px(BarHeight), top = foot - h, gap = f.Px(BarGap);
        float stop = part.Right - f.Px(StopWidth);
        var boxes = new List<PhoneFrame.Box> { new(stop, top, f.Px(StopWidth), h) };
        float w = sends <= 0 ? 0 : (stop - gap - part.X - (sends - 1) * gap) / sends;
        for (int i = 0; i < sends; i++)
            boxes.Add(new PhoneFrame.Box(part.X + i * (w + gap), top, w, h));
        return boxes;
    }

    // ------------------------------------------------------------------ a party card (party_card at the phone's type)

    /// <summary>
    /// A party card's insides, reference pixels from its corner: the face (150, 12 in), the place's badge (its middle and
    /// radius), the name from its left at the badge's middle (to 10 short of ✕), ✕ (84, 10 in from the top right), the
    /// troops' top and the bar under them (from the face's right plus 32 to 16 short of the card's right), the cost at the
    /// foot right; their types.
    /// </summary>
    public const float CardFace = 150, CardIn = 12, BadgeX = 206, BadgeY = 44, Badge = 26, NameX = 242, NameText = 42, Remove = 84,
        RemoveIn = 10, TroopsX = 182, TroopsTop = 84, TroopsText = 34, StrengthTop = 128, StrengthHeight = 8, CardRight = 16, CostFoot = 14,
        CostText = 32, CostLabel = 22, EmptyText = 44, EmptyLabel = 24;

    /// <summary>A card's ✕ (the remove button): 84 square, 10 in from the card's top right.</summary>
    public static PhoneFrame.Box RemoveBox(PhoneFrame f, PhoneFrame.Box card) => Insides.RemoveBox(f, card);

    /// <summary>
    /// A party card's insides in reference pixels from its corner, as the constants above: the face and its inset, the
    /// badge, the name, ✕, the troops and their bar (its right <see cref="BarRight"/> short of the card's), the cost
    /// (<see cref="CostRight"/> short of the card's right, on its foot), an empty place's words; their types. Held upright
    /// the card is lower (<see cref="PhoneLedgerUpright.Insides"/>).
    /// </summary>
    public sealed record CardInsides(float Face, float In, float BadgeX, float BadgeY, float Badge, float NameX, float NameText,
        float Remove, float RemoveIn, float TroopsX, float TroopsTop, float TroopsText, float StrengthTop, float StrengthHeight,
        float BarRight, float CostRight, float CostFoot, float CostText, float CostLabel, float EmptyText, float EmptyLabel)
    {
        /// <summary>The card's ✕: <see cref="Remove"/> square, <see cref="RemoveIn"/> in from the card's top right.</summary>
        public PhoneFrame.Box RemoveBox(PhoneFrame f, PhoneFrame.Box card) =>
            new(card.Right - f.Px(Remove + RemoveIn), card.Y + f.Px(RemoveIn), f.Px(Remove), f.Px(Remove));
    }

    /// <summary>The card's insides held landscape (the constants above; the bar across the text column).</summary>
    public static CardInsides Insides { get; } = new(CardFace, CardIn, BadgeX, BadgeY, Badge, NameX, NameText, Remove, RemoveIn,
        TroopsX, TroopsTop, TroopsText, StrengthTop, StrengthHeight, CardRight, CardRight, CostFoot, CostText, CostLabel, EmptyText,
        EmptyLabel);

    // ------------------------------------------------------------------ Subordinates (LU1, LU1q)

    /// <summary>
    /// The left side's lines, reference pixels from the top: "ORDERS" (its type), the chosen unit's name, "ADD NEW TROOPS";
    /// the steps from the foot up — their height, the room between them, the least room under the caption; their type.
    /// </summary>
    public const float OrdersTop = 26, OrdersText = 40, UnitTop = 84, UnitText = 30, AddTop = 150, AddText = 24, StepHeight = 118,
        StepGap = 10, StepsUnder = 40, StepText = 40, PriceText = 30;

    /// <summary>
    /// The right side's lines from the top (<c>side_title</c>): the title (its type), the units ready, the year, the month
    /// and week, a rule, "BUDGET" and the budget; their types.
    /// </summary>
    public const float SideTitleTop = 20, SideTitleText = 48, ReadyTop = 90, ReadyText = 26, YearTop = 132, YearText = 30,
        WeekTop = 170, WeekText = 28, SideRule = 220, BudgetLabelTop = 240, SideBudgetTop = 274, SideBudgetText = 42;

    /// <summary>The right side's buttons from the foot up: End's height, Replenish's, Talk's, the room between them; their type.</summary>
    private const float EndHeight = 140, ReplenishHeight = 150, TalkHeight = 140, ButtonGap = 14;

    /// <summary>The buttons' type: End's, Replenish's word and its figures, Talk's word.</summary>
    public const float EndText = 44, ReplenishText = 44, ReplenishNote = 30, TalkText = 44;

    /// <summary>A rail's foot over the gesture zone.</summary>
    private const float RailFoot = 10;

    private static float RailFootY(PhoneFrame f) => f.RightRoom.Bottom - f.Px(RailFoot);

    /// <summary>The left side's rail at <paramref name="top"/> reference pixels, <paramref name="height"/> high.</summary>
    public static PhoneFrame.Box LeftLine(PhoneFrame f, float top, float height) => PhoneMap.Left(f, f.Px(top), height);

    /// <summary>The right side's rail at <paramref name="top"/> reference pixels, <paramref name="height"/> high.</summary>
    public static PhoneFrame.Box RightLine(PhoneFrame f, float top, float height) => PhoneMap.Right(f, f.Px(top), height);

    /// <summary>
    /// Add New Troops' <paramref name="count"/> steps in the left side from the foot up, the cheapest lowest; lower when
    /// they would not fit under the caption.
    /// </summary>
    public static IReadOnlyList<PhoneFrame.Box> Steps(PhoneFrame f, int count)
    {
        var rail = PhoneMap.Left(f, 0, 0);
        return PhoneMap.Stack(f, rail.X, rail.W, RailFootY(f), f.Px(AddTop + StepsUnder), count, StepHeight, StepGap);
    }

    /// <summary>End at the right thumb.</summary>
    public static PhoneFrame.Box End(PhoneFrame f) => PhoneMap.Right(f, RailFootY(f) - f.Px(EndHeight), EndHeight);

    /// <summary>Replenish between Talk and End (the owner's change).</summary>
    public static PhoneFrame.Box Replenish(PhoneFrame f) =>
        PhoneMap.Right(f, End(f).Y - f.Px(ButtonGap + ReplenishHeight), ReplenishHeight);

    /// <summary>Talk (the unit's face, the word beside it) over Replenish.</summary>
    public static PhoneFrame.Box Talk(PhoneFrame f) =>
        PhoneMap.Right(f, Replenish(f).Y - f.Px(ButtonGap + TalkHeight), TalkHeight);

    /// <summary>Talk's face: as high as the button less 14 on each side, 14 in from its left.</summary>
    public const float TalkFaceIn = 14;

    /// <summary>Replenish's question (LU1q): a callout 620 wide, 40 left of the button, its foot at the button's.</summary>
    private const float QuestionWidth = 620, QuestionGap = 40;

    /// <summary>
    /// The room Replenish's question stands in, left of its button (LU1q): 620 wide, 40 short of the button, its foot at
    /// the button's; the callout as tall as its text stands on its foot.
    /// </summary>
    public static PhoneFrame.Box ReplenishQuestion(PhoneFrame f)
    {
        var button = Replenish(f);
        float x1 = button.X - f.Px(QuestionGap);
        return new PhoneFrame.Box(x1 - f.Px(QuestionWidth), f.LeftRoom.Y, f.Px(QuestionWidth), button.Bottom - f.LeftRoom.Y);
    }

    // ------------------------------------------------------------------ a unit's picture on a long press (PD2d)

    /// <summary>The picture panel raised over the frame: as high as the frame less this much over and under it, the PC's shape.</summary>
    private const float PictureMargin = 60, PictureShape = 548f / 342f;

    /// <summary>
    /// A unit's picture panel raised by a long press on its row (PD2d): in the frame's middle, as high as the frame less 60
    /// over and under it, the PC's panel's shape (548 × 342).
    /// </summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        float h = f.Frame.H - 2 * f.Px(PictureMargin), w = h * PictureShape;
        return new PhoneFrame.Box(f.Frame.X + (f.Frame.W - w) / 2, f.Frame.Y + (f.Frame.H - h) / 2, w, h);
    }
}
