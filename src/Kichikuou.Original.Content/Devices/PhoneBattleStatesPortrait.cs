namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The battle's other states on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11 and §20): the
/// opening at the scene's place, laid out again at the phone's type (PB4o3); the results plate over the dimmed board at the
/// screen's width, a level-up's rows and our narration panel the same way (PB4r); a fighter's line in our balloon over the
/// scene's foot beside the speaker's portrait, a defence's caption there (PB4b); the cards' long-press menu at a finger's
/// size — the board's places in its layout pixels (<see cref="PhoneBattlePortrait.Layout"/>, its corner the room's) —; and the
/// strategy's cut-in stacked (PB4c), in the canvas's units (<see cref="PhoneFrame.Px"/>). The figures are the mock-ups'
/// (<c>tools/ui/mock_phone.py</c>, <c>pb4o3</c>, <c>pb4c</c>, <c>pb4r</c>, <c>pb4b</c>) in the reference phone's pixels.
/// Interface only. Engine-independent.
/// </summary>
public static class PhoneBattleStatesPortrait
{
    private static float L(float reference) => PhoneBattlePortrait.Layout(reference);

    /// <summary>The board's width in reference pixels: the reference phone's.</summary>
    private const float Width = PhoneFrame.ReferenceShort;

    /// <summary>
    /// How much larger a figure is in the board's layout pixels than in the landscape frame's for the same millimetres (1.6):
    /// what held landscape scales by a factor of the PC's, upright scales by this much more.
    /// </summary>
    public static float Larger => PhoneBattlePortrait.LayoutPerReference / PhoneBattle.LayoutPerReference;

    // ------------------------------------------------------------------ the opening (PB4o3)

    /// <summary>The plate on top in the middle: 840 × 176 from 16.</summary>
    private const float PlateWidth = 840, PlateTop = 16, PlateHeight = 176;

    /// <summary>
    /// Its lines, reference pixels from its top: the place (its middle and type, 2.6 mm), the limit (2.1 mm), the turns
    /// (1.9 mm); its title's bar this far in and this high.
    /// </summary>
    public const float PlaceMiddle = 52, PlaceText = 46, LimitMiddle = 108, LimitText = 36, TurnsMiddle = 150, TurnsText = 34,
        BarInset = 12, BarHeight = 80;

    /// <summary>
    /// The armies' names at the plate's sides (<c>army_name</c>): centred on 230 from their side's edge in a room 420 wide,
    /// their lines centred on 304 under the scene's top (under the plate, over the advantage); at most 72 (4.1 mm), the lines
    /// this far apart; the advantage's middle and type under the names; VS's between them.
    /// </summary>
    private const float NameCentre = 230, NameRoom = 420, NamesMiddle = 304, NamesTop = PlateTop + PlateHeight + 8;

    /// <summary>The names' type, the least it shrinks to, the gap between their lines; the advantage's and VS's places and type.</summary>
    public const float NameText = 72, NameMin = 36, NameGap = 0, AdvantageMiddle = 448, AdvantageText = 40, VsMiddle = 360,
        VsText = 110;

    /// <summary>The seam between the halves: its x at the scene's top and at its foot (the mock-up's split, 50 off the middle).</summary>
    private const float SeamTop = Width / 2 + 50, SeamFoot = Width / 2 - 50;

    /// <summary>The opening's plate (the board's layout pixels).</summary>
    public static PhoneFrame.Box OpeningPlate => new(L((Width - PlateWidth) / 2), L(PlateTop), L(PlateWidth), L(PlateHeight));

    /// <summary>
    /// The room of an army's name (layout pixels): beside the plate on its side, from under it, its middle the names' — their
    /// lines are centred in it, wrapped to its width.
    /// </summary>
    public static PhoneFrame.Box OpeningNames(bool player)
    {
        float cx = player ? NameCentre : Width - NameCentre;
        return new PhoneFrame.Box(L(cx - NameRoom / 2), L(NamesTop), L(NameRoom), L(2 * (NamesMiddle - NamesTop)));
    }

    /// <summary>The advantage's middle (layout pixels): under its army's name, over the scene's foot.</summary>
    public static float OpeningAdvantage => L(AdvantageMiddle);

    /// <summary>"VS" (layout pixels): its middle between the names, in the board's middle.</summary>
    public static (float X, float Y) OpeningVs => (PhoneBattlePortrait.Width / 2, L(VsMiddle));

    /// <summary>The seam's x at the scene's top and at its foot (layout pixels).</summary>
    public static (float Top, float Foot) Seam => (L(SeamTop), L(SeamFoot));

    // ------------------------------------------------------------------ the results plate (PB4r)

    /// <summary>The plate: 24 in from the sides, 720 high without a level-up, its top 890 under the board's (over our front row).</summary>
    private const float ResultsSide = 24, ResultsHeight = 720, ResultsTop = 890, ResultsMargin = 20;

    /// <summary>
    /// Its lines, reference pixels from its top: the title (middle, type: 5 mm), the reason, the divider, each army's name,
    /// its damages (6 mm) and "DAMAGES" (the columns a quarter of the plate in from its middle), the rule between them, the
    /// EXP line; the click's triangle in from the corner and its size.
    /// </summary>
    public const float TitleMiddle = 90, TitleText = 90, ReasonMiddle = 180, ReasonText = 30, Divider = 236, DividerSide = 40,
        ArmyMiddle = 300, ArmyText = 36, DamagesMiddle = 400, DamagesText = 110, DamagesWordMiddle = 490, DamagesWordText = 28,
        RuleTop = 270, RuleFoot = 520, ExpMiddle = 610, ExpText = 44, TriangleIn = 60, TriangleUp = 50, TriangleSize = 22;

    /// <summary>
    /// A level-up's rows under the EXP line, on the same plate (as held landscape): a divider this far under the plate's 720,
    /// the rows from under it, the room under them for the click's triangle, their side room; their insides (docs/ui/messages.md,
    /// §7, A2 with L2): the label's, the name's and the gains' type, the row's room at its left, right, top and foot, between
    /// its columns and between the rows. A row is as tall as its text (the view lays it out).
    /// </summary>
    public const float LevelDivider = 6, LevelTop = 40, LevelFoot = 80, LevelSide = 40, LevelLabel = 22, LevelName = 40, LevelGains = 34,
        LevelLeft = 24, LevelRight = 20, LevelPad = 12, LevelGap = 32, LevelBetween = 14;

    /// <summary>
    /// The results plate (layout pixels) with level-up rows <paramref name="levelRows"/> layout pixels high (0: none) on the
    /// board of <paramref name="height"/> layout pixels: a taller one grows up, its foot kept where the plate's is (over the
    /// battle's Skip, which stands on its strip under the enemy's rows, <see cref="PhoneBattlePortrait.Skip"/>), and over the
    /// board's foot.
    /// </summary>
    public static PhoneFrame.Box ResultsPlate(float levelRows, float height)
    {
        float h = L(ResultsHeight) + (levelRows > 0 ? L(LevelTop + LevelFoot) + levelRows : 0);
        float top = Math.Max(L(ResultsMargin), Math.Min(L(ResultsTop + ResultsHeight) - h, height - L(ResultsMargin) - h));
        return new PhoneFrame.Box(L(ResultsSide), top, PhoneBattlePortrait.Width - 2 * L(ResultsSide), h);
    }

    /// <summary>The level-up rows' box on <paramref name="plate"/>: their left, top and width (layout pixels).</summary>
    public static (float X, float Y, float W) LevelRows(PhoneFrame.Box plate) =>
        (plate.X + L(LevelSide), plate.Y + L(ResultsHeight + LevelTop), plate.W - 2 * L(LevelSide));

    /// <summary>
    /// Our narration panel (a fall's box too) the same way: centred where the plate is, at 2.6 mm, the sentences wrapped to
    /// <see cref="PanelTextWidth"/> (the plate's width less the padding), its lines this far apart, its padding.
    /// </summary>
    public const float PanelText = 46, PanelPitch = 60, PanelSide = 60, PanelTop = 44, PanelFoot = 40,
        PanelTextWidth = Width - 2 * ResultsSide - 2 * PanelSide;

    /// <summary>The panel (layout pixels) for <paramref name="lines"/> lines, its text <paramref name="textWidth"/> layout pixels wide.</summary>
    public static PhoneFrame.Box Panel(int lines, float textWidth)
    {
        float w = textWidth + L(2 * PanelSide), h = L(PanelTop + Math.Max(1, lines) * PanelPitch + PanelFoot);
        float middle = L(ResultsTop + ResultsHeight / 2);
        return new PhoneFrame.Box(PhoneBattlePortrait.Width / 2 - w / 2, middle - h / 2, w, h);
    }

    // ------------------------------------------------------------------ a fighter's line (PB4b), a defence's caption

    /// <summary>The speaker's portrait at the scene's top left (the enemy's at its right): 286 × 379, 8 in and 6 down (the PC's at the width).</summary>
    private const float SpeakerIn = 8, SpeakerTop = 6, SpeakerWidth = 286, SpeakerHeight = 379;

    /// <summary>The speaker's portrait (layout pixels): the player's at the scene's left, the enemy's at its right.</summary>
    public static PhoneFrame.Box Speaker(bool player) =>
        new(player ? L(SpeakerIn) : PhoneBattlePortrait.Width - L(SpeakerIn + SpeakerWidth), L(SpeakerTop), L(SpeakerWidth), L(SpeakerHeight));

    /// <summary>
    /// The balloon (<c>pb4b</c>): from 46 right of the portrait (its tail's room) to 16 short of the board's edge, its foot 440
    /// down (over the scene's foot), growing up; the line's type (3.5 mm) and the smallest it shrinks to for two lines, its
    /// padding, the tail's place under the balloon's top and its size.
    /// </summary>
    public const float BalloonGap = 46, BalloonEnd = 16, BalloonFoot = 440, BalloonMin = 190, LineText = 62, LineMin = 46, LineGap = 16,
        BalloonSide = 40, BalloonTop = 30, TailFromTop = 84, TailDepth = 40, MaxLines = 2;

    /// <summary>The balloon's width in reference pixels: the scene's less the portrait, the tail's room and the edge.</summary>
    public const float BalloonWidth = Width - SpeakerIn - SpeakerWidth - BalloonGap - BalloonEnd;

    /// <summary>
    /// The balloon (layout pixels) for a line of <paramref name="textHeight"/> layout pixels: the player's right of its
    /// portrait, the enemy's mirrored, its tail at the portrait. It grows up from its foot.
    /// </summary>
    public static PhoneFrame.Box Balloon(bool player, float textHeight)
    {
        float h = Math.Max(L(BalloonMin), textHeight + L(2 * BalloonTop));
        float x = player ? L(SpeakerIn + SpeakerWidth + BalloonGap) : L(BalloonEnd);
        return new PhoneFrame.Box(x, L(BalloonFoot) - h, L(BalloonWidth), h);
    }

    /// <summary>A defence's caption at the scene's foot (as held landscape): its type (2.4 mm), the name's, its height, its place over the foot (above the order strip) and in from the edge.</summary>
    public const float CaptionText = 42, CaptionName = 44, CaptionHeight = 117, CaptionAbove = 64, CaptionIn = 34;

    /// <summary>
    /// A defence's caption (layout pixels), <paramref name="width"/> wide: on the defender's side, from the scene's edge, over
    /// the scene's foot.
    /// </summary>
    public static PhoneFrame.Box Caption(bool player, float width)
    {
        float x = player ? L(CaptionIn) : PhoneBattlePortrait.Width - L(CaptionIn) - width;
        return new PhoneFrame.Box(x, PhoneBattlePortrait.SceneFoot - L(CaptionAbove + CaptionHeight), width, L(CaptionHeight));
    }

    /// <summary>A defence's ribbon on its card and the caption's shield, larger than the PC's by this much: held landscape's millimetres.</summary>
    public static float RibbonScale => PhoneBattleStates.RibbonScale * Larger;

    /// <summary>The caption's shield, larger than the PC's by this much (held landscape's millimetres).</summary>
    public static float CaptionShield => PhoneBattleStates.CaptionShield * Larger;

    // ------------------------------------------------------------------ the cards' long-press menu

    /// <summary>
    /// A row's height for <paramref name="items"/> items on the board of <paramref name="height"/> layout pixels: held
    /// landscape's 5.9 mm (<see cref="PhoneBattleStates.MenuRow"/>), lower only when the menu would pass the board's foot.
    /// </summary>
    public static float MenuRowHeight(int items, float height) =>
        Math.Min(L(PhoneBattleStates.MenuRow), (height - L(2 * PhoneBattleStates.MenuPad + 20)) / Math.Max(1, items));

    // ------------------------------------------------------------------ the strategy's cut-in (PB4c), the canvas's units

    /// <summary>The strategist's balloon's padding: its text this far in from its sides and from its top.</summary>
    public const float TextSide = 40, TextTop = 30;

    /// <summary>The cut-in's middle: halfway between the room's top and the bar; its places are given from it.</summary>
    public static float CutInMiddle(PhoneFrame f) => (f.Room.Y + f.Bar.Y) / 2;

    /// <summary>
    /// Its places, reference pixels from <see cref="CutInMiddle"/> (<c>pb4c</c>): the announcement's middle and type, the
    /// title's (6.8 mm); the band across the screen, its top edge from 247 over the middle at the left to 307 at the right,
    /// its foot from 213 under it to 153; the strategist's portrait 372 × 496, 40 in from his side, from 293 over; what it
    /// does on the band's middle (4 mm; a great success's larger, as on the PC), centred in the band beside the portrait's
    /// column (440); his line's balloon across the room 16 in from 293 under, 290 high at least, its tail up at the
    /// portrait's middle; the line at 2.6 mm; the portrait's and the title's slides in.
    /// </summary>
    public const float AnnouncementMiddle = -547, AnnouncementText = 32, CutInTitleMiddle = -437, CutInTitleText = 120, BandTop = -307,
        BandFoot = 213, BandSlant = 60, StrategistIn = 40, StrategistTop = -293, StrategistWidth = 372, StrategistHeight = 496,
        LabelMiddle = -47, LabelText = 72, GreatLabelText = 87, LabelIn = 440, LineTop = 293, LineMinHeight = 290, LineEdge = 16,
        CutInLine = 46, CutInGap = 12, SlideIn = 380, TitleSlide = 60;

    /// <summary>The band's edges' heights (canvas units) at the screen's left and right: its top's, then its foot's.</summary>
    public static (float TopLeft, float TopRight, float FootLeft, float FootRight) CutInBand(PhoneFrame f)
    {
        float m = CutInMiddle(f);
        return (m + f.Px(BandTop + BandSlant), m + f.Px(BandTop), m + f.Px(BandFoot), m + f.Px(BandFoot - BandSlant));
    }

    /// <summary>The strategist's portrait on his side (<paramref name="player"/>: the left), before its slide.</summary>
    public static PhoneFrame.Box Strategist(PhoneFrame f, bool player)
    {
        float x = player ? f.Room.X + f.Px(StrategistIn) : f.Room.Right - f.Px(StrategistIn + StrategistWidth);
        return new PhoneFrame.Box(x, CutInMiddle(f) + f.Px(StrategistTop), f.Px(StrategistWidth), f.Px(StrategistHeight));
    }

    /// <summary>The middle of what the strategy does: on the band beside the portrait's column.</summary>
    public static (float X, float Y) CutInLabel(PhoneFrame f, bool player)
    {
        float w = f.Room.W - f.Px(LabelIn);
        return (player ? f.Room.Right - w / 2 : f.Room.X + w / 2, CutInMiddle(f) + f.Px(LabelMiddle));
    }

    /// <summary>
    /// The widest what the strategy does may be (canvas units): the band beside the portrait's column, <see cref="LabelEdge"/>
    /// clear of the screen's side and the portrait's column; a wider label (a great success's "Strategy +50%") is set smaller.
    /// </summary>
    public static float CutInLabelWidth(PhoneFrame f) => f.Room.W - f.Px(LabelIn + 2 * LabelEdge);

    /// <summary>What the strategy does: its room from the screen's side and from the portrait's column (reference pixels).</summary>
    public const float LabelEdge = 24;

    /// <summary>The strategist's line's balloon, <paramref name="textHeight"/> canvas units of text in it, growing down.</summary>
    public static PhoneFrame.Box CutInBalloon(PhoneFrame f, float textHeight) =>
        new(f.Room.X + f.Px(LineEdge), CutInMiddle(f) + f.Px(LineTop), f.Room.W - f.Px(2 * LineEdge),
            Math.Max(f.Px(LineMinHeight), textHeight + f.Px(2 * TextTop)));
}
