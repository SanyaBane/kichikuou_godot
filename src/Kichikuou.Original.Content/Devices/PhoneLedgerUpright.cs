using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The deployment table and the Subordinates screen on the phone held upright (docs/ui/phone-build.md, step P5;
/// docs/ui/phone.md, §7 and §20: PD2, PD2d, PU1, PU1q with the owner's PD2s2 and PU1e3). Both open with a title band (the
/// title, the date at the right, the subtitle under them, a divider; <see cref="Band"/>). The deployment table puts the party
/// under it as the battle shows our army (PB3: the back row on top, then the front row, facing down; <see cref="Card"/>),
/// the front row held from below in the bracket whose head points down (<see cref="Bracket"/>, the owner's PD2f2, §32),
/// the troops and the war expenses beside the head, then the ledger's tabs and the ledger at the thumb down to the bar,
/// Attack | Stop in the bar (<see cref="Bar"/>). Subordinates puts the ledger under its tabs and the chosen unit's orders
/// under it (<see cref="Orders"/>: Replenish and Talk on one row, Add New Troops three a row), the budget at the bar's left
/// as text and End at the right thumb (<see cref="End"/>). The ledger's rows are two lines (PD2s2): the seal (or the chosen
/// row's diamond), №, the unit's face and the name over the unit's type over bars of the troops and of the commander's HP
/// fixed at the left (<see cref="Places"/>; the face and the bars the owner's, 2026-10-31), the tab's other columns
/// beside them in one line, scrolled sideways under a finger (<see cref="Scrolled"/>), each tab its own scroll. A unit's
/// picture rises on a long press as a sheet over the screen's foot (<see cref="PictureSheet"/>). The figures are the
/// mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>title_band</c>, <c>pd2_base</c>, <c>pd2d</c>, <c>pu1_base</c>,
/// <c>sideways_ledger</c>, <c>party_card</c>, <c>bar_budget</c>) in the reference phone's pixels, the boxes in the canvas's
/// units; a longer screen gives the ledger the height. Interface only. Engine-independent.
/// </summary>
public static class PhoneLedgerUpright
{
    // ------------------------------------------------------------------ the title band (title_band)

    /// <summary>The band: its top under the room's, its height (to the divider and 12 under it).</summary>
    private const float BandTop = 4, BandHeight = 156;

    /// <summary>
    /// The band's lines from its top and its sides: the title's left and type, the date's right, top and type, the
    /// subtitle's left, top and type, the divider's height and its ends' distance from the sides.
    /// </summary>
    public const float TitleX = 30, TitleText = 62, DateRight = 30, DateTop = 22, DateText = 32, SubtitleX = 32, SubtitleTop = 92,
        SubtitleText = 30, Divider = 144, DividerIn = 16;

    /// <summary>The title band across the room under its top.</summary>
    public static PhoneFrame.Box Band(PhoneFrame f) => new(f.Room.X, f.Room.Y + f.Px(BandTop), f.Room.W, f.Px(BandHeight));

    /// <summary>The order's note's line: the subtitle's, its seal ending under the date's right (no room is left beside the tabs).</summary>
    private const float NoteTop = 88, NoteHeight = 40;

    /// <summary>
    /// Where the ledger's order note ("Sorted by T.Atk ↓", while the tab shown has not the sorted column) stands: on the
    /// band's subtitle line, at its right end under the date.
    /// </summary>
    public static PhoneFrame.Box OrderNote(PhoneFrame f)
    {
        var band = Band(f);
        return new PhoneFrame.Box(band.X, band.Y + f.Px(NoteTop), band.W - f.Px(DateRight), f.Px(NoteHeight));
    }

    // ------------------------------------------------------------------ the deployment table's party (pd2_base)

    /// <summary>The party's panel: its distance under the band and its height (600 before the bracket, PD2f2's 45 more).</summary>
    private const float PartyGap = 4, PartyHeight = 645;

    /// <summary>
    /// The panel's lines from its top: "Party" (its left, middle and type), the budget's (right, the figure's type and the
    /// labels'), the rows' captions (their middles: the back row's over its cards, the front row's under its own, inside the
    /// bracket — the owner's PD2f2, docs/ui/phone.md, §32; their type), the totals' middle, beside the bracket's head.
    /// </summary>
    public const float HeadingX = 32, HeadingMiddle = 50, HeadingText = 48, BudgetRight = 32, BudgetText = 40, LabelText = 24,
        BackCaption = 116, FrontCaption = 516, CaptionText = 26, TotalsMiddle = 592;

    /// <summary>The cards: the first row's top, their height, the room between the rows' cards (14 apart), the sides' margin and the room between the columns.</summary>
    private const float CardsTop = 138, CardHeight = 170, RowStep = 184, CardsIn = 16, CardGap = 14;

    /// <summary>The bracket's line (<see cref="PhoneBracket"/>): this far under "Front row"'s middle.</summary>
    public const float BracketUnder = 30;

    /// <summary>The totals: the label's left, the troops' figure's left, the war expenses' right; their types.</summary>
    public const float TotalsX = 32, TotalsFigureX = 170, TotalsRight = 32, TotalsLabel = 26, TotalsText = 38;

    /// <summary>The party's panel across the room under the band.</summary>
    public static PhoneFrame.Box Party(PhoneFrame f)
    {
        var band = Band(f);
        return new PhoneFrame.Box(f.Room.X, band.Bottom + f.Px(PartyGap), f.Room.W, f.Px(PartyHeight));
    }

    /// <summary>
    /// Place <paramref name="place"/>'s card as the battle shows our army upright (PB3): the back row (3, 4) on top, the
    /// front row (1, 2) under it, places 3 and 1 at the left; two a row, 14 apart.
    /// </summary>
    public static PhoneFrame.Box Card(PhoneFrame f, int place)
    {
        var party = Party(f);
        float w = (party.W - 2 * f.Px(CardsIn) - f.Px(CardGap)) / 2;
        float x = party.X + f.Px(CardsIn) + (place is 4 or 2 ? w + f.Px(CardGap) : 0);
        float y = party.Y + f.Px(CardsTop) + (place is 1 or 2 ? f.Px(RowStep) : 0);
        return new PhoneFrame.Box(x, y, w, f.Px(CardHeight));
    }

    /// <summary>A rows' caption's middle: the back row's over the first row of cards, the front row's under the second.</summary>
    public static float CaptionMiddle(PhoneFrame f, bool front) => Party(f).Y + f.Px(front ? FrontCaption : BackCaption);

    /// <summary>
    /// The bracket under the front row (the owner's PD2f2): its ends at the outer sides (1 in) of place 1's card and of place
    /// 2's — of place 1's alone when <paramref name="places"/> is 1, the dungeon search —, its middle theirs, where "Front row"
    /// stands; its line <see cref="BracketUnder"/> under the caption's middle; the cards' foot its ends come up to.
    /// </summary>
    public static (float X0, float X1, float Middle, float Line, float CardsFoot) Bracket(PhoneFrame f, int places)
    {
        var left = Card(f, 1);
        float x0 = left.X + f.Px(1), x1 = (places >= 2 ? Card(f, 2).Right : left.Right) - f.Px(1);
        return (x0, x1, (x0 + x1) / 2, CaptionMiddle(f, front: true) + f.Px(BracketUnder), left.Bottom);
    }

    /// <summary>
    /// A party card's insides upright (<c>party_card</c> with no troop type): the face 146, 12 in; the badge, the name, ✕,
    /// the troops and their bar as held landscape's (<see cref="PhoneLedger"/>) moved to the smaller face; the bar short of
    /// the cost at the foot right.
    /// </summary>
    public static PhoneLedger.CardInsides Insides { get; } = new(Face: 146, In: 12, BadgeX: 202, BadgeY: 44, Badge: 26, NameX: 238,
        NameText: 42, Remove: 84, RemoveIn: 10, TroopsX: 178, TroopsTop: 84, TroopsText: 34, StrengthTop: 128, StrengthHeight: 8,
        BarRight: 214, CostRight: 16, CostFoot: 14, CostText: 32, CostLabel: 22, EmptyText: 44, EmptyLabel: 24);

    // ------------------------------------------------------------------ the tabs and the ledger

    /// <summary>The tabs: their distance under the party (the deployment table) or the band (Subordinates), their inset from the ledger's left.</summary>
    private const float TabsUnderParty = 14, TabsUnderBand = 6, TabsIn = 8;

    /// <summary>The tabs: their width, the room between them, their height, how much lower an unchosen one starts; their type, the order's note's.</summary>
    public const float TabWidth = 340, TabGap = 14, TabHeight = 116, TabDrop = 14, TabText = 40, OrderText = 28;

    /// <summary>The ledger's sides' margin; its foot over the bar (the deployment table) or the orders (Subordinates).</summary>
    private const float LedgerIn = 16, LedgerOverBar = 20, LedgerOverOrders = 14;

    /// <summary>The tabs' row over <see cref="Ledger"/>: from 8 in from the paper's left, as high as a tab.</summary>
    public static PhoneFrame.Box Tabs(PhoneFrame f, bool deployment)
    {
        float top = deployment ? Party(f).Bottom + f.Px(TabsUnderParty) : Band(f).Bottom + f.Px(TabsUnderBand);
        float x = f.Room.X + f.Px(LedgerIn + TabsIn);
        return new PhoneFrame.Box(x, top, f.Room.Right - f.Px(LedgerIn) - x, f.Px(TabHeight));
    }

    /// <summary>
    /// The ledger's paper under its tabs, the room's width less 16 a side, down to 20 over the bar (the deployment table) or
    /// 14 over the orders (Subordinates).
    /// </summary>
    public static PhoneFrame.Box Ledger(PhoneFrame f, bool deployment)
    {
        float top = Tabs(f, deployment).Bottom;
        float foot = deployment ? f.Bar.Y - f.Px(LedgerOverBar) : Orders(f).Y - f.Px(LedgerOverOrders);
        float x = f.Room.X + f.Px(LedgerIn);
        return new PhoneFrame.Box(x, top, f.Room.Right - f.Px(LedgerIn) - x, Math.Max(0, foot - top));
    }

    /// <summary>
    /// A ledger's row (8.5 mm) and its type: № (2.6 mm), the name (2.8 mm, smaller when it would not fit), the type under it
    /// (1.9 mm), the scrolled figures (2.6 mm), the headings (1.6 mm); the room between the columns.
    /// </summary>
    public const float Row = 150, NumberText = 46, NameText = 50, TypeText = 34, Text = 46, HeadText = 28, ColumnGap = 24;

    /// <summary>
    /// The fixed part's places from the paper's left that stay: the party's seal (its left and size), the chosen row's bar
    /// (its width; no diamond, the row's tint tells it — owner, 2026-10-31), the unit's face's side (6.2 mm, in the row's
    /// middle), the name's and the type's middles, the bars of the troops and of the commander's HP under them both (their
    /// tops and height, from the name's left to the name's right margin; no figures; the owner's H4, docs/ui/phone.md
    /// §37-§38); in the header the party's dot (its middle). The rest moves with № (<see cref="Places"/>).
    /// </summary>
    public const float SealX = 12, Seal = 60, ChosenBar = 8, Face = 108, NameMiddle = 44, TypeMiddle = 92, BarTop = 118,
        HpTop = 130, BarHeight = 8, NameRight = 12, DotX = 42;

    /// <summary>
    /// № as wide as two figures (fewer than a hundred units; half-width figures, an em of <see cref="NumberText"/>), this
    /// far right of the seal; the face this far right of №, the name this far right of the face, the type 2 further; the
    /// room from the name's left to the fixed part's right (the name's 226 and its margin); the header's № 4 left of the
    /// figures'.
    /// </summary>
    private const float NumberGap = 10, FaceGap = 14, NameGap = 16, NameRoom = 238, HeadNumberIn = 4;

    /// <summary>
    /// The fixed part's width and the places that move with №, from the paper's left: №'s right, the face's left, the name's
    /// and the type's left; in the header №'s right and the name's left (over the names, not the faces).
    /// </summary>
    public sealed record FixedPlaces(float Width, float NumberRight, float FaceX, float NameX, float TypeX, float HeadNumberRight,
        float HeadNameX);

    /// <summary>
    /// The fixed part's places (owner, 2026-10-31: № no wider than its figures): the deployment table's
    /// (<paramref name="seals"/>) after the party's seal (the part 28.7 mm wide), Subordinates' from the paper's left (no
    /// seal; 24.7 mm).
    /// </summary>
    public static FixedPlaces Places(bool seals)
    {
        float number = (seals ? SealX + Seal + NumberGap : SealX) + NumberText, face = number + FaceGap, name = face + Face + NameGap;
        return new FixedPlaces(name + NameRoom, number, face, name, name + 2, number - HeadNumberIn, name);
    }

    /// <summary>
    /// The shadow's width at the fixed part's edge (while columns have passed under it) and the view's (while some lie
    /// beyond it), and the paper's box round the ▶ at the view's right edge in the header.
    /// </summary>
    public const float Shadow = 26, MoreBox = 40, MoreArrow = 9;

    /// <summary>A Refill button's height in its row (the deployment table's).</summary>
    public const float RefillChip = 64;

    /// <summary>The columns in the fixed part, whatever the tab: №, the name and the type under it.</summary>
    public static IReadOnlyList<DeploymentColumn> FixedColumns { get; } =
        [DeploymentColumn.Number, DeploymentColumn.Name, DeploymentColumn.TroopType];

    /// <summary>
    /// What a tab scrolls sideways (<paramref name="shown"/>, <see cref="LedgerTabs.Of"/>'s): its columns but the fixed
    /// part's, in their order, the groups over them indexed again.
    /// </summary>
    public static LedgerTabColumns Scrolled(LedgerTabColumns shown)
    {
        var columns = shown.Columns.Where(c => !FixedColumns.Contains(c.Column)).ToList();
        var groups = new List<LedgerGroup>();
        foreach (var group in shown.Groups)
        {
            int first = columns.IndexOf(shown.Columns[group.First]), last = columns.IndexOf(shown.Columns[group.Last]);
            if (first >= 0 && last >= first)
                groups.Add(group with { First = first, Last = last });
        }
        return new LedgerTabColumns(columns, groups);
    }

    // ------------------------------------------------------------------ the bars

    /// <summary>The deployment's bar: Stop's width, the room between the buttons; their type (Stop's, a send's alone, two sends', a send's note).</summary>
    private const float StopWidth = 404, BarGap = 16;

    /// <summary>
    /// The bar's type: Stop's, a send button's alone, two sends' (a defence's Plains and City), a send's note (a field's
    /// limit: 32, 15 % over held landscape's, owner, 2026-11-04); the gap between a send's word and its note, as a part
    /// of held landscape's (85 %).
    /// </summary>
    public const float StopText = 48, SendText = 60, SendsText = 46, NoteText = 32, NoteGap = 0.85f;

    /// <summary>
    /// The deployment's bar: the <paramref name="sends"/> buttons that send the party from the left, sharing the room, and
    /// Stop (404 wide) at the right — a way back stands right of the action (owner, 2026-10-30) —, as high as the bar. Stop
    /// is the first box, a send's follow.
    /// </summary>
    public static IReadOnlyList<PhoneFrame.Box> Bar(PhoneFrame f, int sends)
    {
        var bar = f.Bar;
        float gap = f.Px(BarGap), stop = bar.Right - f.Px(StopWidth);
        var boxes = new List<PhoneFrame.Box> { new(stop, bar.Y, f.Px(StopWidth), bar.H) };
        float w = sends <= 0 ? 0 : (stop - gap - bar.X - (sends - 1) * gap) / sends;
        for (int i = 0; i < sends; i++)
            boxes.Add(new PhoneFrame.Box(bar.X + i * (w + gap), bar.Y, w, bar.H));
        return boxes;
    }

    // ------------------------------------------------------------------ Subordinates' orders (pu1_base, PU1e3)

    /// <summary>The orders' panel: its height, its foot over the bar.</summary>
    private const float OrdersHeight = 620, OrdersOverBar = 16;

    /// <summary>
    /// The orders' lines from the panel's top: "Orders" (its left, middle and type), the unit's name after it (its middle,
    /// the room before it, its type), "ADD NEW TROOPS" (its middle, type, the rule's room after it).
    /// </summary>
    public const float OrdersX = 32, OrdersMiddle = 50, OrdersText = 46, UnitMiddle = 54, UnitGap = 20, UnitText = 30, AddMiddle = 280,
        AddText = 28, AddRuleGap = 20;

    /// <summary>Replenish and Talk: their top, height, the sides' margin and the room between them.</summary>
    private const float ButtonsTop = 100, ButtonsHeight = 136, OrdersIn = 24, ButtonsGap = 20;

    /// <summary>
    /// The buttons' type and lines: Replenish's word and its figures (their middles from the top and the foot), Talk's word
    /// and "WITH …", Talk's face's inset.
    /// </summary>
    public const float ReplenishText = 52, ReplenishNote = 32, WordMiddle = 46, NoteFoot = 30, TalkText = 52, TalkWith = 26,
        TalkFaceIn = 14;

    /// <summary>Add New Troops: the steps' top, height, the room between them, how many a row; their type and lines.</summary>
    private const float StepsTop = 310, StepHeight = 128, StepGap = 12;

    /// <summary>The steps a row.</summary>
    public const int StepsAcross = 3;

    /// <summary>A step's type and lines from its top: the men's (brush) and the price's.</summary>
    public const float StepText = 48, StepMiddle = 46, PriceText = 32, PriceMiddle = 100;

    /// <summary>The orders' panel across the room, its foot 16 over the bar.</summary>
    public static PhoneFrame.Box Orders(PhoneFrame f)
    {
        float foot = f.Bar.Y - f.Px(OrdersOverBar);
        return new PhoneFrame.Box(f.Room.X, foot - f.Px(OrdersHeight), f.Room.W, f.Px(OrdersHeight));
    }

    /// <summary>Replenish at the orders' left (the owner's change: Replenish and Talk on one row).</summary>
    public static PhoneFrame.Box Replenish(PhoneFrame f) => OrderButton(f, right: false);

    /// <summary>Talk (the unit's face, the word, "WITH …") at the orders' right.</summary>
    public static PhoneFrame.Box Talk(PhoneFrame f) => OrderButton(f, right: true);

    private static PhoneFrame.Box OrderButton(PhoneFrame f, bool right)
    {
        var orders = Orders(f);
        float w = (orders.W - 2 * f.Px(OrdersIn) - f.Px(ButtonsGap)) / 2;
        float x = orders.X + f.Px(OrdersIn) + (right ? w + f.Px(ButtonsGap) : 0);
        return new PhoneFrame.Box(x, orders.Y + f.Px(ButtonsTop), w, f.Px(ButtonsHeight));
    }

    /// <summary>Add New Troops' <paramref name="count"/> steps three a row, the cheapest first, under their caption.</summary>
    public static IReadOnlyList<PhoneFrame.Box> Steps(PhoneFrame f, int count)
    {
        var orders = Orders(f);
        float gap = f.Px(StepGap), w = (orders.W - 2 * f.Px(OrdersIn) - (StepsAcross - 1) * gap) / StepsAcross, h = f.Px(StepHeight);
        var boxes = new List<PhoneFrame.Box>();
        for (int i = 0; i < count; i++)
            boxes.Add(new PhoneFrame.Box(orders.X + f.Px(OrdersIn) + i % StepsAcross * (w + gap),
                orders.Y + f.Px(StepsTop) + i / StepsAcross * (h + gap), w, h));
        return boxes;
    }

    /// <summary>End's width at the bar's right (PU1e3).</summary>
    private const float EndWidth = 420;

    /// <summary>End's type.</summary>
    public const float EndText = 52;

    /// <summary>End at the bar's right, the right thumb (PU1e3).</summary>
    public static PhoneFrame.Box End(PhoneFrame f) => new(f.Bar.Right - f.Px(EndWidth), f.Bar.Y, f.Px(EndWidth), f.Bar.H);

    /// <summary>
    /// The budget at the bar's left as text (PU1e3): its left from the bar's, the label's middle from the bar's top, the
    /// figure's middle, the "GOLD" after it (its middle, the room before it); their types.
    /// </summary>
    public const float BudgetX = 24, BudgetLabelMiddle = 42, BudgetFigureMiddle = 102, BudgetFigureText = 56, BudgetUnitMiddle = 108,
        BudgetUnitGap = 14, BudgetLabelText = 26;

    // ------------------------------------------------------------------ the result plate after a troop order (PU1r1d2)

    /// <summary>The plate's height, its margin at the sides.</summary>
    private const float ResultHeight = 800, ResultIn = 16;

    /// <summary>
    /// The plate's lines from its top and its left (<c>tools/ui/mock_result_plate.py</c>, <c>pu1r1</c>): the title (its
    /// middle, type, the room it keeps at the sides), the dividers (their heights and their ends' distance from the
    /// sides), the face (its top, left and size), the unit's name and troop type right of it (their left, middles and
    /// types), the troops before → after (their middle, type, the gain's type), the bar (its top, height, the sides'
    /// margin), the troops after under it (their middle, the label's type and the figures'), Cost | Budget (the labels'
    /// top, type, the room under them, the figures' type), the click's triangle (from the right and the foot).
    /// </summary>
    public const float ResultTitleMiddle = 92, ResultTitleText = 72, ResultTitleIn = 60, ResultDivider = 166, ResultDivider2 = 590,
        ResultDividerIn = 40, ResultFaceTop = 202, ResultFaceX = 48, ResultFace = 230, ResultNameX = 318, ResultNameMiddle = 250,
        ResultNameText = 60, ResultTypeMiddle = 316, ResultTypeText = 34, ResultTroopsMiddle = 396, ResultTroopsText = 60,
        ResultGainText = 44, ResultBarTop = 476, ResultBarHeight = 22, ResultBarIn = 48, ResultNowMiddle = 534,
        ResultNowLabel = 28, ResultNowText = 30, ResultColumnsTop = 626, ResultColumnsIn = 40, ResultLabelText = 32,
        ResultLabelGap = 24, ResultFigureText = 56, ResultTriangleRight = 46, ResultTriangleFoot = 40;

    /// <summary>The result plate (PU1r1): the room's width less 16 a side, 800 high, in the middle between the room's top and the bar.</summary>
    public static PhoneFrame.Box ResultPlate(PhoneFrame f)
    {
        float h = f.Px(ResultHeight);
        return new PhoneFrame.Box(f.Room.X + f.Px(ResultIn), f.Room.Y + (f.Bar.Y - f.Room.Y - h) / 2, f.Room.W - 2 * f.Px(ResultIn), h);
    }

    // ------------------------------------------------------------------ a unit's picture on a long press (PD2d)

    /// <summary>The picture's sheet: the room over the picture for the name, the picture's margin at the sides; the PC's panel's shape.</summary>
    private const float SheetHead = 116, PictureIn = 16, PictureShape = 548f / 342f;

    /// <summary>The sheet's lines from its top: the name (its left, middle, type), the details after it (the room before them, their middle and type).</summary>
    public const float SheetNameX = 32, SheetNameMiddle = 60, SheetNameText = 52, SheetDetailGap = 28, SheetDetailMiddle = 64,
        SheetDetailText = 28;

    /// <summary>
    /// The unit's picture panel a long press raises (PD2d): at the room's width less 16 a side, the PC's panel's shape, its
    /// foot at the bar's.
    /// </summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        float w = f.Room.W - 2 * f.Px(PictureIn), h = w / PictureShape;
        return new PhoneFrame.Box(f.Room.X + f.Px(PictureIn), f.Bar.Bottom - h, w, h);
    }

    /// <summary>The sheet the picture stands on: across the room from 116 over the picture down to the room's foot.</summary>
    public static PhoneFrame.Box PictureSheet(PhoneFrame f)
    {
        float top = Picture(f).Y - f.Px(SheetHead);
        return new PhoneFrame.Box(f.Room.X, top, f.Room.W, f.Room.Bottom - top);
    }
}
