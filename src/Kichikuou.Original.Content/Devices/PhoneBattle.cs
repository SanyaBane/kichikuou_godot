namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our battle screen on the phone held landscape (docs/ui/phone-build.md, step L3; docs/ui/phone.md, §12 LB2, §17 LB2r1 and
/// LB2x): the scene's own picture at 70% of its height, its sides blurred out to the frame, the board under it with taller
/// cards (25.5 × 19.0 mm) and the gauge between the front rows — the frame's places, given in its layout pixels (the
/// 1280×800 layout over the engine's picture: the mock-ups' figures over the reference's 1.525, <see cref="Layout"/>) —; the
/// rails in the black sides (the acting unit's commands left from the foot up, Retreat on top; the target's attacks right,
/// a thumb's height each, the first at the foot), the battle's Skip on the left rail's top, the retreat question beside
/// Retreat in the frame — the sides' places in the canvas's units (<see cref="PhoneFrame.Px"/>, as <see cref="PhoneMap"/>'s).
/// The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>land_board</c>, <c>land_rails</c>, <c>lb2r1</c>,
/// <c>exchange_counter</c>) in the reference phone's pixels. Interface only. Engine-independent.
/// </summary>
public static class PhoneBattle
{
    /// <summary>The frame's layout pixels a reference pixel: the layout fitted to the reference phone's height (1220 / 800).</summary>
    public const float LayoutPerReference = 800f / PhoneFrame.ReferenceShort;

    /// <summary>A length of <paramref name="reference"/> reference pixels in the frame's layout pixels.</summary>
    public static float Layout(float reference) => reference * LayoutPerReference;

    // ------------------------------------------------------------------ the frame (layout pixels)

    /// <summary>The scene's band: 70% of the PC's 410 (24.9 mm), its own 1024-wide picture as much smaller in the middle.</summary>
    private const float SceneHeight = 438, PcSceneHeight = 410, PcPictureWidth = 1024, FrameWidth = 1952;

    /// <summary>
    /// The board under it (<c>land_board</c>): the armies' names in a line 4 under the scene, 40 high and 16 in from the
    /// frame's sides; the cards from 50 under the scene, 447 × 334, 12 apart and 12 in from the frame's sides; the gauge
    /// 90 wide in the frame's middle between the front rows, 12 from them.
    /// </summary>
    private const float NamesTop = 4, NamesHeight = 40, NamesEdge = 16, CardsTop = 50, CardWidth = 447, CardHeight = 334, CardGap = 12,
        CardEdge = 12, GaugeWidth = 90;

    /// <summary>The scene band's height (layout pixels).</summary>
    public static float SceneBand => Layout(SceneHeight);

    /// <summary>How much smaller the scene's own picture is than the PC's (0.70).</summary>
    public static float SceneScale => SceneHeight * LayoutPerReference / PcSceneHeight;

    /// <summary>The scene's own picture, sharp, in the band's middle (layout pixels; its sides are the band's blur).</summary>
    public static PhoneFrame.Box ScenePicture
    {
        get
        {
            float w = PcPictureWidth * SceneScale;
            return new PhoneFrame.Box((1280 - w) / 2, 0, w, SceneBand);
        }
    }

    /// <summary>An army's name line over its cards (layout pixels): the player's at the left, the enemy's right-aligned at the right.</summary>
    public static PhoneFrame.Box Names(bool player)
    {
        float w = Layout(FrameWidth / 2 - NamesEdge - GaugeWidth / 2);
        return new PhoneFrame.Box(player ? Layout(NamesEdge) : 1280 - Layout(NamesEdge) - w, SceneBand + Layout(NamesTop), w,
            Layout(NamesHeight));
    }

    /// <summary>
    /// The card in <paramref name="column"/> (0 the player's back row at the far left, 1 its front row, 2 the enemy's front row,
    /// 3 its back row at the far right) and <paramref name="row"/> (0 the top), in layout pixels.
    /// </summary>
    public static PhoneFrame.Box Card(int column, int row)
    {
        float middle = FrameWidth / 2;
        float x = column switch
        {
            0 => CardEdge,
            1 => CardEdge + CardWidth + CardGap,
            2 => middle + GaugeWidth / 2 + CardGap,
            _ => middle + GaugeWidth / 2 + CardGap + CardWidth + CardGap,
        };
        return new PhoneFrame.Box(Layout(x), SceneBand + Layout(CardsTop + row * (CardHeight + CardGap)), Layout(CardWidth),
            Layout(CardHeight));
    }

    /// <summary>The rounds gauge between the front rows, as high as both rows (layout pixels).</summary>
    public static PhoneFrame.Box Gauge =>
        new(Layout(FrameWidth / 2 - GaugeWidth / 2), SceneBand + Layout(CardsTop), Layout(GaugeWidth), Layout(2 * CardHeight + CardGap));

    /// <summary>
    /// A card's insides (<c>land_card</c>), reference pixels: its padding, the face's square at the top left with the four
    /// figures in a row under it, the text right of it — the name at 45, the troop type and the state at 36, the troops and
    /// the HP at 40 (2.3-2.6 mm) —, the figures at 32; an own card's order toggles across its foot, 80 high (4.6 mm), 10 in.
    /// </summary>
    public const float CardPad = 10, Face = 160, StatsRow = 52, StatsText = 32, NameText = 45, CardText = 40, QuietText = 36,
        TogglesHeight = 80, TogglesInset = 10;

    /// <summary>
    /// The exchange's counters in the scene's blurred sides (LB2x), reference pixels: each side's beside the picture,
    /// this far from it, their baseline over the band's foot; the troops' figure, the HP's and the labels' type (5.9 mm,
    /// 3.3 mm, 1.5 mm), and the room kept from the frame's side.
    /// </summary>
    public const float CounterGap = 30, CounterFoot = 40, CounterTroops = 124, CounterHp = 70, CounterLabel = 26, CounterEdge = 16;

    // ------------------------------------------------------------------ the sides (the canvas's units)

    /// <summary>The rails' columns in the sides: PhoneMap's (14.5 mm); their panels this much wider each way, the rows this far in.</summary>
    private const float RailInner = 6, RailOuter = 10, PanelOut = 6, RowIn = 8;

    /// <summary>The battle's Skip on the left rail's top (<c>land_skip</c>): its top and height.</summary>
    private const float SkipTop = 20, SkipHeight = 100;

    /// <summary>The left rail's acting unit's name (its middle, the brush face's size) and the rule under it.</summary>
    public const float ActingMiddle = 180, ActingText = 46, LeftRule = 222;

    /// <summary>The right rail's TARGET (its middle and size), the target's name under it, the rule under them.</summary>
    public const float TargetTitleMiddle = 60, TargetTitleText = 28, TargetMiddle = 122, TargetText = 56, RightRule = 172;

    /// <summary>
    /// A rail's rows: the commands 123 high (7.0 mm), the attacks 260 (14.8 mm, the damage forecast at the thumb), 12 apart,
    /// the lowest 10 over the gesture zone, the highest this far under its rule.
    /// </summary>
    private const float CommandHeight = 123, AttackHeight = 260, RowGap = 12, RailFoot = 10, UnderRule = 18;

    /// <summary>The retreat question beside Retreat (LB2r1): its place in the frame, its size, its margin from the frame's edges.</summary>
    private const float QuestionLeft = 44, QuestionWidth = 660, QuestionHeight = 350, QuestionMargin = 20;

    /// <summary>
    /// Its insides, reference pixels: the question's line (its top and type) and the note's, the rule, the answers' row (its
    /// top, height, the room round it and between the answers), the tip's half height and length.
    /// </summary>
    public const float QuestionTop = 34, QuestionText = 44, NoteTop = 100, NoteText = 38, QuestionRule = 166, AnswersTop = 186,
        AnswerHeight = 138, AnswersSide = 40, AnswerGap = 16, AnswerText = 40, RetreatText = 50, TipHalf = 30, TipLength = 46;

    /// <summary>The left rail's column (the rows' room less <see cref="RowIn"/> each way).</summary>
    private static (float X, float W) LeftColumn(PhoneFrame f)
    {
        var room = f.LeftRoom;
        return (room.X + f.Px(RailInner), room.W - f.Px(RailInner + RailOuter));
    }

    private static (float X, float W) RightColumn(PhoneFrame f)
    {
        var room = f.RightRoom;
        return (room.X + f.Px(RailOuter), room.W - f.Px(RailInner + RailOuter));
    }

    /// <summary>The left rail's panel: its column and a little more, the side's whole height.</summary>
    public static PhoneFrame.Box LeftPanel(PhoneFrame f) => Panel(f, LeftColumn(f));

    /// <summary>The right rail's panel.</summary>
    public static PhoneFrame.Box RightPanel(PhoneFrame f) => Panel(f, RightColumn(f));

    /// <summary>The battle's Skip on the left rail's top, where it stays while the rails are gone (the whole battle).</summary>
    public static PhoneFrame.Box SkipCell(PhoneFrame f)
    {
        var (x, w) = LeftColumn(f);
        return new PhoneFrame.Box(x + f.Px(RowIn), f.LeftRoom.Y + f.Px(SkipTop), w - 2 * f.Px(RowIn), f.Px(SkipHeight));
    }

    /// <summary>The acting unit's commands in the left side, index 0 at the foot (Defend, …, Retreat on top), under the name's rule.</summary>
    public static IReadOnlyList<PhoneFrame.Box> CommandRows(PhoneFrame f, int count)
    {
        var (x, w) = LeftColumn(f);
        return PhoneMap.Stack(f, x + f.Px(RowIn), w - 2 * f.Px(RowIn), f.LeftRoom.Bottom - f.Px(RailFoot),
            f.LeftRoom.Y + f.Px(LeftRule + UnderRule), count, CommandHeight, RowGap);
    }

    /// <summary>The target's attacks in the right side, index 0 (the first, Melee) at the thumb, under TARGET's rule.</summary>
    public static IReadOnlyList<PhoneFrame.Box> AttackRows(PhoneFrame f, int count)
    {
        var (x, w) = RightColumn(f);
        return PhoneMap.Stack(f, x + f.Px(RowIn), w - 2 * f.Px(RowIn), f.RightRoom.Bottom - f.Px(RailFoot),
            f.RightRoom.Y + f.Px(RightRule + UnderRule), count, AttackHeight, RowGap);
    }

    /// <summary>A line's band across the left rail (its middle <paramref name="middle"/> reference pixels under the side's top), the rows' width.</summary>
    public static PhoneFrame.Box LeftLine(PhoneFrame f, float middle, float height) => Line(f, LeftColumn(f), middle, height);

    /// <summary>A line's band across the right rail.</summary>
    public static PhoneFrame.Box RightLine(PhoneFrame f, float middle, float height) => Line(f, RightColumn(f), middle, height);

    /// <summary>
    /// The retreat question (LB2r1): in the frame beside <paramref name="retreat"/>, the rail's button (its tip at it), its
    /// middle level with the button's, kept inside the frame over the gesture zone.
    /// </summary>
    public static PhoneFrame.Box RetreatQuestion(PhoneFrame f, PhoneFrame.Box retreat)
    {
        float h = f.Px(QuestionHeight), y = retreat.Y + retreat.H / 2 - h / 2;
        float top = Math.Max(f.Frame.Y, f.LeftRoom.Y) + f.Px(QuestionMargin), foot = f.LeftRoom.Bottom - f.Px(QuestionMargin);
        return new PhoneFrame.Box(f.Frame.X + f.Px(QuestionLeft), Math.Clamp(y, top, Math.Max(top, foot - h)), f.Px(QuestionWidth), h);
    }

    /// <summary>The question's answers in one row (<paramref name="count"/> of them, left to right), the warning one last.</summary>
    public static IReadOnlyList<PhoneFrame.Box> RetreatAnswers(PhoneFrame f, PhoneFrame.Box question, int count)
    {
        count = Math.Max(1, count);
        float side = f.Px(AnswersSide), gap = f.Px(AnswerGap);
        float w = (question.W - 2 * side - (count - 1) * gap) / count;
        return [.. Enumerable.Range(0, count).Select(i =>
            new PhoneFrame.Box(question.X + side + i * (w + gap), question.Y + f.Px(AnswersTop), w, f.Px(AnswerHeight)))];
    }

    private static PhoneFrame.Box Panel(PhoneFrame f, (float X, float W) column) =>
        new(column.X - f.Px(PanelOut), 0, column.W + 2 * f.Px(PanelOut), f.Screen.H);

    private static PhoneFrame.Box Line(PhoneFrame f, (float X, float W) column, float middle, float height) =>
        new(column.X + f.Px(RowIn), f.LeftRoom.Y + f.Px(middle - height / 2), column.W - 2 * f.Px(RowIn), f.Px(height));
}
