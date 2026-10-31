namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The battle's other states on the phone held landscape (docs/ui/phone-build.md, step L4; docs/ui/phone.md, §17): the
/// opening at the scene's band, laid out again at the frame's width (LB3o1); the results plate in the frame's middle, a
/// level-up's rows and our narration panel the same way (LB3r); a fighter's line in our balloon over the scene's foot,
/// two lines at most (LB3b), a defence's caption there; the cards' long-press menu at a finger's size — the frame's places
/// in its layout pixels (<see cref="PhoneBattle.Layout"/>) —; and the ninja's scout report across the whole width (LB3s),
/// in the canvas's units (<see cref="PhoneFrame.Px"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>,
/// <c>lb3o1</c>, <c>lb3s</c>, <c>lb3r</c>, <c>lb3b</c>) in the reference phone's pixels. Interface only. Engine-independent.
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
    /// A level-up's rows under the EXP line, on the same plate: a divider this far under the plate's 720, the rows from
    /// under it, their height and pitch (lower when the plate would not fit the frame), the room under them for the click's
    /// triangle, their side room and type.
    /// </summary>
    public const float LevelDivider = 6, LevelTop = 40, LevelRow = 76, LevelPitch = 92, LevelFoot = 80, LevelSide = 60, LevelWord = 30,
        LevelName = 40, LevelGains = 34;

    /// <summary>The level-ups' pitch for <paramref name="levelUps"/> rows (reference pixels): the plate kept over the gesture zone.</summary>
    public static float LevelUpPitch(int levelUps)
    {
        if (levelUps <= 0)
            return LevelPitch;
        float room = PhoneFrame.ReferenceShort - PhoneFrame.GestureZone - 2 * ResultsMargin - ResultsHeight - LevelTop - LevelFoot;
        return Math.Min(LevelPitch, room / levelUps);
    }

    /// <summary>The results plate (layout pixels) with <paramref name="levelUps"/> level-ups' rows.</summary>
    public static PhoneFrame.Box ResultsPlate(int levelUps)
    {
        float h = ResultsHeight + (levelUps > 0 ? LevelTop + levelUps * LevelUpPitch(levelUps) + LevelFoot : 0);
        float top = Math.Clamp((PhoneFrame.ReferenceShort - h) / 2 - ResultsLift, ResultsMargin,
            Math.Max(ResultsMargin, PhoneFrame.ReferenceShort - PhoneFrame.GestureZone - ResultsMargin - h));
        return new PhoneFrame.Box(L((FrameWidth - ResultsWidth) / 2), L(top), L(ResultsWidth), L(h));
    }

    /// <summary>The level-up row <paramref name="i"/> of <paramref name="count"/> on <paramref name="plate"/> (layout pixels).</summary>
    public static PhoneFrame.Box LevelUpRow(PhoneFrame.Box plate, int i, int count)
    {
        float pitch = LevelUpPitch(count), height = Math.Min(LevelRow, pitch - 8);
        return new PhoneFrame.Box(plate.X + L(LevelSide), plate.Y + L(ResultsHeight + LevelTop + i * pitch), plate.W - 2 * L(LevelSide),
            L(height));
    }

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

    // ------------------------------------------------------------------ the scout report (LB3s), the canvas's units

    /// <summary>
    /// Its places from the room between the hole's strips (its left edge, <c>LX0</c>, 6 in from the strip): the ninja's portrait
    /// 345 × 460 from 14 in and 30 down; her balloon 60 right of it from 40 to 246 down, 10 short of the room's right edge; the
    /// cards 700 × 270, 40 apart in a row and 20 in a column, from 330 down, centred under the balloon, the rows' captions at
    /// 300; Rance's portrait 246 × 320 at the foot, his balloon 60 right of it, 1064 wide, from 210 to 50 over the foot;
    /// Maris's mirrored at the right, her balloon 40 right of his and 60 short of her.
    /// </summary>
    private const float ReportIn = 6, PortraitIn = 14, NinjaTop = 30, NinjaWidth = 345, NinjaHeight = 460, ReportGap = 60,
        ReportTop = 40, ReportFoot = 246, ReportEnd = 10, CardWidth = 700, CardHeight = 270, CardColumns = 40, CardRows = 20,
        CardsTop = 330, CaptionsMiddle = 300, SpeakerWidth = 246, SpeakerHeight = 320, SpeakerFoot = 10, ReplyWidth = 1064,
        ReplyTop = 210, ReplyFoot = 50, CommentGap = 40;

    /// <summary>
    /// The report's type: the balloons' text (the report's 54, the answers' 60) and its padding, the tail's place under the
    /// balloon's top; a card's name (42), troop type (30), troops (38), "TROOPS" (22), the place's badge (its radius), the
    /// face (200, 12 in), the rows' captions (30).
    /// </summary>
    public const float ReportText = 54, ReplyText = 60, TextSide = 40, TextTop = 30, LinePitch = 1.25f, ReportTail = 70,
        ReplyTail = 60, CardName = 42, CardKind = 30, CardTroops = 38, CardLabel = 22, Badge = 26, CardFace = 200, FaceIn = 12,
        RowCaption = 30;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ReportIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ReportIn);

    /// <summary>The ninja's portrait at the top left.</summary>
    public static PhoneFrame.Box NinjaPortrait(PhoneFrame f) =>
        new(Left(f) + f.Px(PortraitIn), f.Px(NinjaTop), f.Px(NinjaWidth), f.Px(NinjaHeight));

    /// <summary>Her balloon with the army's name and totals, right of her.</summary>
    public static PhoneFrame.Box ReportBalloon(PhoneFrame f)
    {
        float x = NinjaPortrait(f).Right + f.Px(ReportGap);
        return new PhoneFrame.Box(x, f.Px(ReportTop), Right(f) - f.Px(ReportEnd) - x, f.Px(ReportFoot - ReportTop));
    }

    /// <summary>Place <paramref name="slot"/>'s card (1-2 the front row in the left column, 3-4 the back row; 1 and 3 on top).</summary>
    public static PhoneFrame.Box ScoutCard(PhoneFrame f, int slot)
    {
        var balloon = ReportBalloon(f);
        float x0 = balloon.X + (balloon.W - f.Px(2 * CardWidth + CardColumns)) / 2;
        int column = (slot - 1) / 2, row = (slot - 1) % 2;
        return new PhoneFrame.Box(x0 + column * f.Px(CardWidth + CardColumns), f.Px(CardsTop + row * (CardHeight + CardRows)),
            f.Px(CardWidth), f.Px(CardHeight));
    }

    /// <summary>The middle of a column's caption ("FRONT ROW", "BACK ROW") over its cards.</summary>
    public static (float X, float Y) RowCaptionAt(PhoneFrame f, int column)
    {
        var card = ScoutCard(f, 1 + 2 * column);
        return (card.X + card.W / 2, f.Px(CaptionsMiddle));
    }

    /// <summary>
    /// The bracket left of the front row, its head pointing left as the enemy attack's (the owner's LB3sf1, docs/ui/phone.md,
    /// §47; <see cref="PhoneBracket"/>): its ends at the top (1 in) of place 1's card and the foot of place 2's, its middle
    /// theirs; its line <see cref="PhoneBracket.Beside"/> left of the cards' left side, in the black beside them.
    /// </summary>
    public static (float Y0, float Y1, float Middle, float Line, float CardsSide) ScoutBracket(PhoneFrame f)
    {
        var top = ScoutCard(f, 1);
        float y0 = top.Y + f.Px(1), y1 = ScoutCard(f, 2).Bottom - f.Px(1);
        return (y0, y1, (y0 + y1) / 2, top.X - f.Px(PhoneBracket.Beside), top.X);
    }

    /// <summary>Rance's portrait at the bottom left (<paramref name="right"/>: the advisor's, at the bottom right).</summary>
    public static PhoneFrame.Box SpeakerPortrait(PhoneFrame f, bool right)
    {
        float foot = f.Screen.H - f.Safe.Bottom - f.Px(SpeakerFoot);
        float x = right ? Right(f) - f.Px(PortraitIn + SpeakerWidth) : Left(f) + f.Px(PortraitIn);
        return new PhoneFrame.Box(x, foot - f.Px(SpeakerHeight), f.Px(SpeakerWidth), f.Px(SpeakerHeight));
    }

    /// <summary>
    /// The answer's balloon (<paramref name="comment"/>: the advisor's comment's), <paramref name="textHeight"/> canvas units of
    /// text in it: Rance's right of his portrait, hers between his and her portrait; both grow up from their foot.
    /// </summary>
    public static PhoneFrame.Box ReplyBalloon(PhoneFrame f, bool comment, float textHeight)
    {
        float foot = f.Screen.H - f.Safe.Bottom - f.Px(ReplyFoot);
        float h = Math.Max(f.Px(ReplyTop - ReplyFoot), textHeight + f.Px(2 * TextTop));
        float x = SpeakerPortrait(f, false).Right + f.Px(ReportGap);
        if (!comment)
            return new PhoneFrame.Box(x, foot - h, f.Px(ReplyWidth), h);
        x += f.Px(ReplyWidth + CommentGap);
        return new PhoneFrame.Box(x, foot - h, SpeakerPortrait(f, true).X - f.Px(ReportGap) - x, h);
    }
}
