namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our enemy attack screen on the phone held upright (docs/ui/phone-build.md, step P6; docs/ui/phone.md, §8: PA1 and PA1n
/// with the owner's change): the PC's screen stacked over the whole room. The head under the hole (<see cref="Head"/>: the
/// attacker's flag, the title, the date at the right, the news, a divider); the defended province's panel (<see cref="Province"/>:
/// its picture at the left, its four figures beside it, each a label over its value); "VS"; the attacking army's panel
/// (<see cref="Army"/>: the four places' cards two a row, the front row (1, 2) on top facing up at the province, held from
/// above in the PC's bracket turned up (<see cref="Bracket"/>, the owner's PA1f2a, §31), the back row (3, 4) under it, the
/// totals and the report's source under them, all in the panel's middle — the owner's change to the landscape screen, LA2);
/// Maris's question (<see cref="Advisor"/>); then the answers at the thumb: Abandon City (10% narrower,
/// the owner's change) | Ninja Scout, and Intercept in the bar. Ninja Scout's list opens above its button over Maris's
/// question: one ninja from the mock-up's left, more two a row across the room. The figures are the mock-ups'
/// (<c>tools/ui/mock_phone.py</c>: <c>attack_head</c>, <c>pa1_base</c>, <c>pa1n</c>, <c>attacker_card</c>, <c>maris</c>,
/// <c>attack_answers</c>) in the reference phone's pixels, from the room's top and the bar; the boxes in the canvas's units.
/// A longer screen gives the army's panel the height, a shorter one takes it from its cards. Interface only.
/// Engine-independent.
/// </summary>
public static class PhoneAttackUpright
{
    /// <summary>The panels' and the answers' distance from the room's sides.</summary>
    private const float SideIn = 16;

    // ------------------------------------------------------------------ the head (attack_head)

    /// <summary>The head: its top under the room's, its height (to the divider and 14 under it).</summary>
    private const float HeadTop = 6, HeadHeight = 186;

    /// <summary>
    /// The head's lines from its top and the room's sides: the attacker's flag (its left, width and middle), the title's left
    /// and type, the date's right, top and type, the news's left, top and type, the divider and its ends' distance from the sides.
    /// </summary>
    public const float FlagIn = 24, FlagSize = 78, FlagMiddle = 45, TitleIn = 120, TitleText = 60, DateRight = 30, DateTop = 22,
        DateText = 30, NewsIn = 30, NewsTop = 104, NewsText = 40, Divider = 172, DividerIn = 24;

    /// <summary>The head across the room under its top.</summary>
    public static PhoneFrame.Box Head(PhoneFrame f) => new(f.Room.X, f.Room.Y + f.Px(HeadTop), f.Room.W, f.Px(HeadHeight));

    // ------------------------------------------------------------------ the province's panel (pa1_base)

    /// <summary>The province's panel's height; the picture's left and top in it, its width (the PC's 3:2).</summary>
    private const float ProvinceHeight = 540, PictureIn = 24, PictureTop = 84, PictureWidth = 600, PictureShape = 200f / 300f;

    /// <summary>The figures beside the picture: their left in the panel, their right's distance from its, the first row's top, the rows' pitch and height (to the rule).</summary>
    private const float FiguresIn = 654, FiguresRight = 28, FiguresTop = 100, FigurePitch = 88, FigureHeight = 82;

    /// <summary>A figure's label (at its row's top) and value (this far under it, at the right): their type.</summary>
    public const float FigureLabel = 31, FigureValue = 38, ValueTop = 36;

    /// <summary>The figures the panel lists beside the picture.</summary>
    public const int Figures = 4;

    /// <summary>The defended province's panel, under the head.</summary>
    public static PhoneFrame.Box Province(PhoneFrame f) =>
        new(f.Room.X + f.Px(SideIn), Head(f).Bottom, f.Room.W - f.Px(2 * SideIn), f.Px(ProvinceHeight));

    /// <summary>The province's picture at the panel's left, under its caption.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        var panel = Province(f);
        return new PhoneFrame.Box(panel.X + f.Px(PictureIn), panel.Y + f.Px(PictureTop), f.Px(PictureWidth), f.Px(PictureWidth * PictureShape));
    }

    /// <summary>The figure row <paramref name="i"/> (0-3) beside the picture: the label at its top left, the value under it at the right, a rule at its foot.</summary>
    public static PhoneFrame.Box Figure(PhoneFrame f, int i)
    {
        var panel = Province(f);
        float x = panel.X + f.Px(FiguresIn);
        return new PhoneFrame.Box(x, panel.Y + f.Px(FiguresTop + i * FigurePitch), panel.Right - f.Px(FiguresRight) - x, f.Px(FigureHeight));
    }

    // ------------------------------------------------------------------ "VS" and the army's panel (pa1_base, attacker_card)

    /// <summary>The room "VS" stands in between the panels.</summary>
    private const float VsRoom = 80;

    /// <summary>"VS"'s type.</summary>
    public const float VsText = 56;

    /// <summary>"VS"'s middle: in the room between the panels, the room's middle across.</summary>
    public static (float X, float Y) Vs(PhoneFrame f) => (f.Room.X + f.Room.W / 2, Province(f).Bottom + f.Px(VsRoom / 2));

    /// <summary>The army's panel's distance over Maris's question.</summary>
    private const float ArmyFoot = 20;

    /// <summary>The attacking army's panel, from under "VS" to over Maris's question.</summary>
    public static PhoneFrame.Box Army(PhoneFrame f)
    {
        float top = Province(f).Bottom + f.Px(VsRoom), foot = Advisor(f).Y - f.Px(ArmyFoot);
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, f.Room.W - f.Px(2 * SideIn), Math.Max(0, foot - top));
    }

    /// <summary>
    /// The cards: 16 in from the panel's sides, 14 apart across and down, at most 270 high; each row's caption in the 40 over
    /// it; the totals 16 under the cards, their block 106 high (<see cref="PhoneAttack.TotalsMiddle"/> on); the block in the
    /// panel's middle and 20 lower, room for the bracket's head over the front row's caption (round P25 drew it 40 lower;
    /// 20 keeps Ninja Scout's list of all five ninjas under the back row); the head's tip at least 20 under the panel's top
    /// — beside the panel's caption, which keeps to the left —, the room kept over the panel's foot.
    /// </summary>
    private const float CardsIn = 16, CardGap = 14, CardHeight = 270, CaptionsRoom = 40, BracketDrop = 20, TotalsUnder = 16,
        TotalsBlock = 106, TipUnder = 20, ContentFoot = 24, TotalsIn = 28;

    /// <summary>A row's caption's middle over its cards.</summary>
    public const float RowCaptionAbove = 24;

    /// <summary>The bracket's line (<see cref="PhoneBracket"/>, the owner's PA1f2a, docs/ui/phone.md, §31): this far over "FRONT ROW"'s middle.</summary>
    public const float BracketOver = 30;

    /// <summary>A card's face: at most this large, as high as the card less 12 at the top and the foot.</summary>
    public const float CardFace = 200;

    /// <summary>The cards' height: 270, less on a screen too short for the panel to hold them.</summary>
    private static float CardH(PhoneFrame f)
    {
        float fixedPart = FrontLeast + CardGap + CaptionsRoom + TotalsUnder + TotalsBlock + ContentFoot;
        return Math.Max(0, Math.Min(f.Px(CardHeight), (Army(f).H - f.Px(fixedPart)) / 2));
    }

    /// <summary>The front row's least distance under the panel's top: the head's tip, the head, the line and the caption over it.</summary>
    private const float FrontLeast = TipUnder + PhoneBracket.HeadTip + BracketOver + RowCaptionAbove;

    private static float CardsTop(PhoneFrame f)
    {
        var panel = Army(f);
        float block = 2 * CardH(f) + f.Px(2 * CaptionsRoom + CardGap + TotalsUnder + TotalsBlock);
        return Math.Max(panel.Y + f.Px(FrontLeast), panel.Y + (panel.H - block) / 2 + f.Px(CaptionsRoom + BracketDrop));
    }

    /// <summary>Place <paramref name="slot"/>'s card: the front row (1, 2) on top, nearer the province, the back row (3, 4) under it; 1 and 3 at the left.</summary>
    public static PhoneFrame.Box Card(PhoneFrame f, int slot)
    {
        var panel = Army(f);
        float w = (panel.W - f.Px(2 * CardsIn + CardGap)) / 2, h = CardH(f);
        float x = panel.X + f.Px(CardsIn) + (slot is 2 or 4 ? w + f.Px(CardGap) : 0);
        float y = CardsTop(f) + (slot is 3 or 4 ? h + f.Px(CardGap + CaptionsRoom) : 0);
        return new PhoneFrame.Box(x, y, w, h);
    }

    /// <summary>The middle of a row's caption ("FRONT ROW" over 1-2, "BACK ROW" over 3-4), the panel's middle across.</summary>
    public static (float X, float Y) RowCaptionAt(PhoneFrame f, int row)
    {
        var panel = Army(f);
        return (panel.X + panel.W / 2, Card(f, 1 + 2 * row).Y - f.Px(RowCaptionAbove));
    }

    /// <summary>
    /// The bracket over the front row: its ends at the front row's outer sides (1 in), its middle the panel's, its line
    /// <see cref="BracketOver"/> over "FRONT ROW"'s middle, and the cards' top its ends come down to.
    /// </summary>
    public static (float X0, float X1, float Middle, float Line, float CardsTop) Bracket(PhoneFrame f)
    {
        var (middle, caption) = RowCaptionAt(f, 0);
        var left = Card(f, 1);
        return (left.X + f.Px(1), Card(f, 2).Right - f.Px(1), middle, caption - f.Px(BracketOver), left.Y);
    }

    /// <summary>The totals under the cards: 28 in from the panel's sides; their top, 16 under the cards.</summary>
    public static (float X0, float X1, float Top) Totals(PhoneFrame f)
    {
        var panel = Army(f);
        return (panel.X + f.Px(TotalsIn), panel.Right - f.Px(TotalsIn), Card(f, 4).Bottom + f.Px(TotalsUnder));
    }

    // ------------------------------------------------------------------ Maris and the answers (maris, attack_answers)

    /// <summary>The answers' row's top over the bar; Abandon City's inset in it (10 lower, 128 high) and share of its half; Ninja Scout's height; the gap between the halves.</summary>
    private const float RowAbove = 168, AbandonTop = 10, AbandonHeight = 128, AbandonShare = 0.9f, ScoutHeight = 148, HalvesGap = 16;

    private static float RowTop(PhoneFrame f) => f.Bar.Y - f.Px(RowAbove);

    private static float Half(PhoneFrame f) => (f.Room.W - f.Px(2 * SideIn + HalvesGap)) / 2;

    /// <summary>Intercept: the bar, at the thumb.</summary>
    public static PhoneFrame.Box Intercept(PhoneFrame f) => f.Bar;

    /// <summary>Ninja Scout: the row's right half, over the bar.</summary>
    public static PhoneFrame.Box NinjaScout(PhoneFrame f)
    {
        float x = f.Room.X + f.Px(SideIn) + Half(f) + f.Px(HalvesGap);
        return new PhoneFrame.Box(x, RowTop(f), f.Room.Right - f.Px(SideIn) - x, f.Px(ScoutHeight));
    }

    /// <summary>Abandon City: the row's left half less a tenth (the owner's change), 10 under Ninja Scout's top and 10 over its foot.</summary>
    public static PhoneFrame.Box Abandon(PhoneFrame f) =>
        new(f.Room.X + f.Px(SideIn), RowTop(f) + f.Px(AbandonTop), (float)Math.Round(Half(f) * AbandonShare), f.Px(AbandonHeight));

    /// <summary>Maris's question: 330 high, 20 over the answers' row.</summary>
    private const float AdvisorHeight = 330, AdvisorGap = 20;

    /// <summary>Her panel's insides: her face (170, in from its left and top), the text's left in from the panel's, "MARIS"'s top and type, the lines' first top, type and pitch, their right margin.</summary>
    public const float AdvisorFace = 170, AdvisorFaceIn = 24, AdvisorFaceTop = 20, AdvisorTextIn = 224, AdvisorNameTop = 26,
        AdvisorName = 28, AdvisorLinesTop = 66, AdvisorText = 42, AdvisorPitch = 1.22f, AdvisorRight = 24;

    /// <summary>Maris's panel with her question, over the answers.</summary>
    public static PhoneFrame.Box Advisor(PhoneFrame f)
    {
        float foot = RowTop(f) - f.Px(AdvisorGap);
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), foot - f.Px(AdvisorHeight), f.Room.W - f.Px(2 * SideIn), f.Px(AdvisorHeight));
    }

    // ------------------------------------------------------------------ Ninja Scout's list (pa1n, ninja_callout)

    /// <summary>
    /// The list: 30 over its button, ending at its right; one ninja from 440 in from the room's left (the mock-up's), more
    /// across the room two a row; its head, its rows (150 high, 12 apart; two columns 16 apart), 24 in, the room under them.
    /// </summary>
    private const float ListAbove = 30, ListOneIn = 440, ListHead = 86, ListRow = 150, ListGap = 12, ListColumns = 16, ListIn = 24,
        ListFoot = 24;

    /// <summary>A row's name's left: this far right of its face's square (the row's height).</summary>
    public const float RowTextGap = 20;

    private static int Columns(int ninjas) => ninjas > 1 ? 2 : 1;

    /// <summary>The list of <paramref name="ninjas"/> ninjas above Ninja Scout, as high as its rows.</summary>
    public static PhoneFrame.Box List(PhoneFrame f, int ninjas)
    {
        var button = NinjaScout(f);
        int rows = Math.Max(1, (ninjas + Columns(ninjas) - 1) / Columns(ninjas));
        float h = f.Px(ListHead + rows * ListRow + (rows - 1) * ListGap + ListFoot);
        float x = f.Room.X + f.Px(Columns(ninjas) == 1 ? ListOneIn : SideIn), foot = button.Y - f.Px(ListAbove);
        return new PhoneFrame.Box(x, foot - h, button.Right - x, h);
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
