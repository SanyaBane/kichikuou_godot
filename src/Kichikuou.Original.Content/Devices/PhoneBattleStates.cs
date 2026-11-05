namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The battle's other states on the phone held landscape (docs/ui/phone-build.md, step L4; docs/ui/phone.md, §17): the
/// opening at the scene's band, laid out again at the frame's width (LB3o1); the results plate in the frame's middle, a
/// level-up's rows and our narration panel the same way (LB3r); a fighter's line in our balloon over the scene's foot,
/// two lines at most (LB3b), a defence's caption there; the cards' long-press menu at a finger's size — the frame's places
/// in its layout pixels (<see cref="PhoneBattle.Layout"/>) —; and the commanders' cards' type, in the canvas's units
/// (<see cref="PhoneFrame.Px"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>lb3o1</c>, <c>lb3r</c>,
/// <c>lb3b</c>) in the reference phone's pixels. Interface only. Engine-independent.
/// </summary>
public static class PhoneBattleStates
{
    private static float L(float reference) => PhoneBattle.Layout(reference);

    /// <summary>The frame's width and the scene band's height in reference pixels.</summary>
    private const float FrameWidth = 1952, Band = 438;

    // ------------------------------------------------------------------ the opening (LB3o1)

    /// <summary>The plate on top in the middle (<c>opening_plate</c>): 940 × 196 from 18.</summary>
    private const float PlateWidth = 940, PlateTop = 18, PlateHeight = 196;

    /// <summary>
    /// Its lines, reference pixels from its top: the place (its middle and type), the limit, the turns; its title's bar
    /// this far in and this high.
    /// </summary>
    public const float PlaceMiddle = 58, PlaceText = 50, LimitMiddle = 122, LimitText = 38, TurnsMiddle = 166, TurnsText = 36,
        BarInset = 12, BarHeight = 92;

    /// <summary>
    /// The armies' names beside the plate (<c>army_name</c>): centred in the room between the frame's edge and the plate,
    /// this far from both; at most this large, the lines this far apart; the advantage's middle and type under them; VS's.
    /// </summary>
    public const float NameMargin = 30, NameText = 104, NameMin = 40, NameGap = 6, AdvantageMiddle = 384, AdvantageText = 46,
        AdvantageRoom = 36, VsMiddle = 324, VsText = 120;

    /// <summary>The opening's plate (layout pixels).</summary>
    public static PhoneFrame.Box OpeningPlate =>
        new(L((FrameWidth - PlateWidth) / 2), L(PlateTop), L(PlateWidth), L(PlateHeight));

    /// <summary>
    /// The room of an army's name (layout pixels): beside the plate on its side, the band's height — or down to above the
    /// advantage's line when the army has one (<paramref name="advantage"/>). The name's lines are centred in it.
    /// </summary>
    public static PhoneFrame.Box OpeningNames(bool player, bool advantage)
    {
        float plateX = (FrameWidth - PlateWidth) / 2;
        float x = player ? NameMargin : plateX + PlateWidth + NameMargin;
        float foot = advantage ? AdvantageMiddle - AdvantageText / 2 - AdvantageRoom : Band;
        return new PhoneFrame.Box(L(x), 0, L(plateX - 2 * NameMargin), L(foot));
    }

    /// <summary>The advantage's middle (layout pixels): under its army's name, level with the band's foot.</summary>
    public static float OpeningAdvantage => L(AdvantageMiddle);

    /// <summary>"VS" (layout pixels): its middle under the plate, in the frame's middle.</summary>
    public static (float X, float Y) OpeningVs => (640, L(VsMiddle));

    // ------------------------------------------------------------------ the results plate (LB3r)

    /// <summary>The plate: 1400 wide, 720 high without a level-up, centred a little over the frame's middle (20 up).</summary>
    private const float ResultsWidth = 1400, ResultsHeight = 720, ResultsLift = 20, ResultsMargin = 20;

    /// <summary>
    /// Its lines, reference pixels from its top: the title (middle, type), the reason, the divider, each army's name, its
    /// damages and "DAMAGES" (the columns a quarter of the plate in from its middle), the rule between them, the EXP line.
    /// </summary>
    public const float TitleMiddle = 100, TitleText = 100, ReasonMiddle = 196, ReasonText = 36, Divider = 250, DividerSide = 60,
        ArmyMiddle = 318, ArmyText = 40, DamagesMiddle = 426, DamagesText = 120, DamagesWordMiddle = 520, DamagesWordText = 30,
        RuleTop = 290, RuleFoot = 540, ExpMiddle = 628, ExpText = 50, TriangleIn = 64, TriangleUp = 54, TriangleSize = 24;

    /// <summary>
    /// A level-up's rows under the EXP line, on the same plate: a divider this far under the plate's head (720), the rows
    /// from under it, the room under them for the click's triangle, their side room; their insides (docs/ui/messages.md, §7,
    /// A2 with L2): the label's, the name's and the gains' type, the row's room at its left, right, top and foot, between its
    /// columns and between the rows. A row is as tall as its text (the view lays it out); the names stand whole, the rows
    /// being wide.
    /// </summary>
    public const float LevelDivider = 6, LevelTop = 40, LevelFoot = 80, LevelSide = 60, LevelLabel = 22, LevelName = 40, LevelGains = 34,
        LevelLeft = 30, LevelRight = 24, LevelPad = 12, LevelGap = 45, LevelBetween = 10;

    /// <summary>The least the head's lines may close up (<see cref="HeadScale"/>): its empty bands, never its type.</summary>
    public const float HeadSqueezeLeast = 0.75f;

    /// <summary>
    /// How far the plate's head (its lines' places, <see cref="ResultsHeight"/>) closes up so that level-up rows
    /// <paramref name="levelRows"/> layout pixels high fit the frame over the gesture zone: 1 while they fit, down to
    /// <see cref="HeadSqueezeLeast"/> (the owner's A2: four rows of the longest names and gains).
    /// </summary>
    public static float HeadScale(float levelRows)
    {
        if (levelRows <= 0)
            return 1;
        float room = L(PhoneFrame.ReferenceShort - PhoneFrame.GestureZone - 2 * ResultsMargin - LevelTop - LevelFoot) - levelRows;
        return Math.Clamp(room / L(ResultsHeight), HeadSqueezeLeast, 1);
    }

    /// <summary>The results plate (layout pixels) with level-up rows <paramref name="levelRows"/> layout pixels high (0: none).</summary>
    public static PhoneFrame.Box ResultsPlate(float levelRows)
    {
        float h = L(ResultsHeight) * HeadScale(levelRows) + (levelRows > 0 ? L(LevelTop + LevelFoot) + levelRows : 0);
        float top = Math.Clamp((L(PhoneFrame.ReferenceShort) - h) / 2 - L(ResultsLift), L(ResultsMargin),
            Math.Max(L(ResultsMargin), L(PhoneFrame.ReferenceShort - PhoneFrame.GestureZone - ResultsMargin) - h));
        return new PhoneFrame.Box(L((FrameWidth - ResultsWidth) / 2), top, L(ResultsWidth), h);
    }

    /// <summary>The level-up rows' box on <paramref name="plate"/> with rows <paramref name="levelRows"/> high: their left, top and width.</summary>
    public static (float X, float Y, float W) LevelRows(PhoneFrame.Box plate, float levelRows) =>
        (plate.X + L(LevelSide), plate.Y + L(ResultsHeight) * HeadScale(levelRows) + L(LevelTop), plate.W - 2 * L(LevelSide));

    /// <summary>
    /// Our narration panel (a fall's box too) the same way: in the frame's middle at the plate's type, the sentences
    /// wrapped to <see cref="PanelTextWidth"/>, its lines this far apart, its padding.
    /// </summary>
    public const float PanelText = 50, PanelPitch = 66, PanelTextWidth = 1300, PanelSide = 60, PanelTop = 44, PanelFoot = 40;

    /// <summary>The panel (layout pixels) for <paramref name="lines"/> lines, its text <paramref name="textWidth"/> layout pixels wide.</summary>
    public static PhoneFrame.Box Panel(int lines, float textWidth)
    {
        float w = textWidth + L(2 * PanelSide), h = L(PanelTop + Math.Max(1, lines) * PanelPitch + PanelFoot);
        return new PhoneFrame.Box(640 - w / 2, 400 - h / 2 - L(ResultsLift), w, h);
    }

    // ------------------------------------------------------------------ a fighter's line (LB3b), a defence's caption

    /// <summary>
    /// The balloon (<c>lb3b</c>): from 50 right of the scene's picture's left edge, 1000 wide, its foot 486 down (over the
    /// scene's foot); the line's type and the smallest it shrinks to for two lines, its padding, the tail's place under the
    /// balloon's top and its size.
    /// </summary>
    public const float BalloonIn = 50, BalloonWidth = 1000, BalloonFoot = 486, BalloonMin = 190, LineText = 62, LineMin = 46,
        LineGap = 16, BalloonSide = 40, BalloonTop = 30, TailFromTop = 84, TailDepth = 40, TailHalf = 26, MaxLines = 2;

    /// <summary>
    /// The balloon (layout pixels) for a line of <paramref name="textHeight"/> layout pixels: the player's beside the scene's
    /// picture's left edge, its tail at the portrait in the blurred side; the enemy's mirrored. It grows up from its foot.
    /// </summary>
    public static PhoneFrame.Box Balloon(bool player, float textHeight)
    {
        float h = Math.Max(L(BalloonMin), textHeight + L(2 * BalloonTop));
        var picture = PhoneBattle.ScenePicture;
        float x = player ? picture.X + L(BalloonIn) : picture.Right - L(BalloonIn) - L(BalloonWidth);
        return new PhoneFrame.Box(x, L(BalloonFoot) - h, L(BalloonWidth), h);
    }

    /// <summary>A defence's caption at the scene's foot: its type (2.4 mm), the name's, its height, its place over the foot (above the order strip) and in from the picture's edge.</summary>
    public const float CaptionText = 42, CaptionName = 44, CaptionHeight = 117, CaptionAbove = 64, CaptionIn = 34, CaptionShield = 1.5f;

    /// <summary>
    /// A defence's caption (layout pixels), <paramref name="width"/> wide: on the defender's side, from the scene's picture's
    /// edge, over the scene's foot.
    /// </summary>
    public static PhoneFrame.Box Caption(bool player, float width)
    {
        var picture = PhoneBattle.ScenePicture;
        float x = player ? picture.X + L(CaptionIn) : picture.Right - L(CaptionIn) - width;
        return new PhoneFrame.Box(x, PhoneBattle.SceneBand - L(CaptionAbove + CaptionHeight), width, L(CaptionHeight));
    }

    /// <summary>A defence's ribbon on its card, larger than the PC's by this much.</summary>
    public const float RibbonScale = 1.3f;

    // ------------------------------------------------------------------ the cards' long-press menu

    /// <summary>The menu at a finger's size: its rows (5.9 mm), the type, its padding, the current order's mark, its least width.</summary>
    public const float MenuRow = 104, MenuText = 40, MenuPad = 12, MenuMark = 40, MenuMinWidth = 320;

    /// <summary>A row's height for <paramref name="items"/> items (layout pixels): lower when the menu would not fit the frame over the gesture zone.</summary>
    public static float MenuRowHeight(int items)
    {
        float room = PhoneFrame.ReferenceShort - PhoneFrame.GestureZone - 2 * MenuPad - 20;
        return L(Math.Min(MenuRow, room / Math.Max(1, items)));
    }

    // ------------------------------------------------------------------ the commanders' cards (LA2), the canvas's units

    /// <summary>
    /// A commander's card at the phone's type, the enemy attack's: its name (42), troop type (30), troops (38), "TROOPS"
    /// (22), the place's badge (its radius), the face (200, 12 in).
    /// </summary>
    public const float CardName = 42, CardKind = 30, CardTroops = 38, CardLabel = 22, Badge = 26, CardFace = 200, FaceIn = 12;
}
