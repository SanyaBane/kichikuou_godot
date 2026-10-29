namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our battle screen on the phone held upright (docs/ui/phone-build.md, step P3; docs/ui/phone.md, §6, PB3, PB3r, PB3x): the
/// board laid out <see cref="Width"/> layout pixels wide over the room's width, its corner at the room's (<see cref="Scale"/>,
/// <see cref="Height"/>) — from the top the scene's picture at the width; our army's name and its two rows, the back row
/// first; the round's gauge turned across where the front rows face each other; the enemy's front row, its back row and its
/// name at the thumb; the battle's Skip on a strip of its own; the deck at the foot: the acting unit's commands in the left
/// column from the foot up (Retreat on top), the target's attacks in the right one, the first at the thumb; the retreat
/// question over the deck's right column, its tip at Retreat. The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>,
/// <c>pb3_board</c>, <c>slim_card</c>, <c>battle_deck</c>, <c>pb3r</c>) in the reference phone's pixels, given in the board's
/// layout pixels (<see cref="Layout"/>). Interface only. Engine-independent.
/// </summary>
public static class PhoneBattleUpright
{
    /// <summary>The board's width in layout pixels: the PC's, so that what spans the PC's screen spans the phone's width.</summary>
    public const float Width = 1280;

    /// <summary>The board's layout pixels a reference pixel: the board at the reference phone's width (1280 / 1220).</summary>
    public const float LayoutPerReference = Width / PhoneFrame.ReferenceShort;

    /// <summary>A length of <paramref name="reference"/> reference pixels in the board's layout pixels.</summary>
    public static float Layout(float reference) => reference * LayoutPerReference;

    /// <summary>The canvas's units a layout pixel of the board: the room's width over <see cref="Width"/> (the frame's own scale).</summary>
    public static float Scale(PhoneFrame f) => f.Room.W / Width;

    /// <summary>The board's height in its layout pixels: the room's, down to the gesture zone.</summary>
    public static float Height(PhoneFrame f) => f.Room.H / Scale(f);

    /// <summary><paramref name="board"/>, a box of the board's layout pixels, in the canvas's units (the board's corner the room's).</summary>
    public static PhoneFrame.Box OnScreen(PhoneFrame f, PhoneFrame.Box board)
    {
        float k = Scale(f);
        return new PhoneFrame.Box(f.Room.X + board.X * k, f.Room.Y + board.Y * k, board.W * k, board.H * k);
    }

    // ------------------------------------------------------------------ the board (layout pixels)

    /// <summary>How much larger the scene's own 1024-wide picture is than the PC's: the board's width (1.25).</summary>
    public const float SceneScale = Width / 1024;

    /// <summary>The scene's foot: its 410 at <see cref="SceneScale"/>.</summary>
    public const float SceneFoot = 410 * SceneScale;

    /// <summary>The scene's picture at the board's top, the whole width (no blurred sides).</summary>
    public static PhoneFrame.Box Scene => new(0, 0, Width, SceneFoot);

    /// <summary>
    /// The board under it (<c>pb3_board</c>), reference pixels: the armies' names in a band 40 high, 24 in from the sides,
    /// ours 6 under the scene, the enemy's 8 under its back row; the cards 586 × 230 (33.3 × 13.1 mm) from 52 under the
    /// scene, 16 in from the sides and 16 apart, an army's rows 10 apart; the gauge's band 72 high between the front rows, 10
    /// from each; the battle's Skip 300 × 120, 16 in, 54 under the enemy's back row; the deck from 186 under it.
    /// </summary>
    private const float NamesTop = 6, NamesHeight = 40, NamesEdge = 24, EnemyNamesTop = 8, CardsTop = 52, CardWidth = 586,
        CardHeight = 230, Edge = 16, RowGap = 10, GaugeHeight = 72, SkipTop = 54, SkipWidth = 300, SkipHeight = 120, DeckTop = 186;

    /// <summary>
    /// A card's insides (<c>slim_card</c>), reference pixels: its padding, the face's square at the top left with the four
    /// figures in a row of 40 under it at 28, the text right of it — the name at 36, the troop type and the state at 28, the
    /// troops and the HP at 32 (1.6-2.0 mm) —; an own card's order toggles across the text column's foot, 38 high, 8 in, their
    /// words at 24; the exchange's action pill 42 high. The padding, the face and its figures fill the card's height.
    /// </summary>
    public const float CardPad = 8, Face = 174, StatsRow = 40, StatsText = 28, NameText = 36, CardText = 32, QuietText = 28,
        TogglesHeight = 38, TogglesInset = 8, TogglesText = 24, PillHeight = 42;

    /// <summary>The row of cards <paramref name="row"/>'s top: 0 our back row, 1 our front row, 2 the enemy's front row, 3 its back row.</summary>
    private static float RowTop(int row) =>
        SceneFoot + Layout(CardsTop + row * (CardHeight + RowGap) + (row >= 2 ? GaugeHeight + RowGap : 0));

    /// <summary>
    /// The card in <paramref name="row"/> (0 our back row on top, 1 our front row, 2 the enemy's front row, 3 its back row)
    /// and <paramref name="column"/> (0 the left).
    /// </summary>
    public static PhoneFrame.Box Card(int row, int column) =>
        new(Layout(Edge + column * (CardWidth + Edge)), RowTop(row), Layout(CardWidth), Layout(CardHeight));

    /// <summary>An army's name line: ours over its back row, the enemy's under its own.</summary>
    public static PhoneFrame.Box Names(bool player) =>
        new(Layout(NamesEdge), player ? SceneFoot + Layout(NamesTop) : Card(3, 0).Bottom + Layout(EnemyNamesTop),
            Width - 2 * Layout(NamesEdge), Layout(NamesHeight));

    /// <summary>The round's gauge, turned across, in the band between the front rows (the board's width less its edges).</summary>
    public static PhoneFrame.Box Gauge => new(Layout(Edge), Card(1, 0).Bottom + Layout(RowGap), Width - 2 * Layout(Edge), Layout(GaugeHeight));

    /// <summary>
    /// The gauge's insides across (reference pixels, from its band's left and top): the tube, its segments' padding, and
    /// the caption's left — its number at 44 and "TURNS LEFT" at 28 right of it, on the band's middle.
    /// </summary>
    public const float TubeLeft = 314, TubeWidth = 480, TubeTop = 8, TubeHeight = 56, CaptionLeft = 824, CaptionNumber = 44,
        CaptionWords = 28;

    /// <summary>The battle's Skip on its strip, at the left (the tablet's corner; the agent's choice, confirmed 2026-10-28).</summary>
    public static PhoneFrame.Box Skip => new(Layout(Edge), Card(3, 0).Bottom + Layout(SkipTop), Layout(SkipWidth), Layout(SkipHeight));

    // ------------------------------------------------------------------ the deck (layout pixels; its foot the board's)

    /// <summary>The deck's top.</summary>
    public static float DeckTopY => Card(3, 0).Bottom + Layout(DeckTop);

    /// <summary>The deck's panel: the board's width, from its top to the board's foot (<paramref name="height"/>).</summary>
    public static PhoneFrame.Box Deck(float height) => new(0, DeckTopY, Width, Math.Max(0, height - DeckTopY));

    /// <summary>
    /// The deck's heading (<c>battle_deck</c>), reference pixels from its top: the acting unit's name in the brush face at 44
    /// 20 into the left column; TARGET at 26 10 into the right one, the target's name at 44 160 into it; their middle 46
    /// under the top; the rule across at 88, 24 in.
    /// </summary>
    public const float HeadingMiddle = 46, ActingIn = 20, ActingText = 44, TargetTitleIn = 10, TargetTitleText = 26, TargetIn = 160,
        TargetText = 44, HeadingRule = 88, RuleIn = 24;

    /// <summary>
    /// The deck's rows, reference pixels: the commands at most 132 high (7.5 mm), the attacks 250 (14.2 mm, the damage
    /// forecast at the thumb), 12 apart, lower when too many — the gap kept —, the lowest 20 over the board's foot, the
    /// highest 110 under the deck's top.
    /// </summary>
    private const float CommandHeight = 132, AttackHeight = 250, DeckGap = 12, DeckFoot = 20, RowsTop = 110;

    /// <summary>The deck's left column (the acting unit's commands) and its right one (the target's attacks): x and width.</summary>
    public static (float X, float W) Column(bool right) => (Layout(Edge + (right ? CardWidth + Edge : 0)), Layout(CardWidth));

    /// <summary>A line across a column of the deck, its middle <paramref name="middle"/> reference pixels under the deck's top.</summary>
    public static PhoneFrame.Box HeadingLine(bool right, float inset, float middle, float height)
    {
        var (x, w) = Column(right);
        return new PhoneFrame.Box(x + Layout(inset), DeckTopY + Layout(middle - height / 2), w - Layout(inset), Layout(height));
    }

    /// <summary>The rule under the heading, across the deck.</summary>
    public static float RuleY => DeckTopY + Layout(HeadingRule);

    /// <summary>The acting unit's commands in the left column, index 0 at the foot (Defend, …, Retreat on top).</summary>
    public static IReadOnlyList<PhoneFrame.Box> CommandRows(float height, int count) => Rows(height, false, count, CommandHeight);

    /// <summary>The target's attacks in the right column, index 0 (the first, Melee) at the thumb.</summary>
    public static IReadOnlyList<PhoneFrame.Box> AttackRows(float height, int count) => Rows(height, true, count, AttackHeight);

    private static IReadOnlyList<PhoneFrame.Box> Rows(float height, bool right, int count, float most)
    {
        if (count <= 0)
            return [];
        var (x, w) = Column(right);
        float foot = height - Layout(DeckFoot), top = DeckTopY + Layout(RowsTop), gap = Layout(DeckGap);
        float h = Math.Max(0, Math.Min(Layout(most), (foot - top - (count - 1) * gap) / count));
        return [.. Enumerable.Range(0, count).Select(i => new PhoneFrame.Box(x, foot - (i + 1) * h - i * gap, w, h))];
    }

    // ------------------------------------------------------------------ the retreat question (PB3r)

    /// <summary>
    /// The question's box over the deck's right column (<c>pb3r</c>), reference pixels: 6 left of the column, 16 short of the
    /// board's edge, 330 high, its top level with Retreat's; its tip at its left edge, 22 each way and 24 long, at Retreat's
    /// middle.
    /// </summary>
    private const float QuestionOut = 6, QuestionHeight = 330;

    /// <summary>
    /// Its insides, reference pixels from its corner: the question's line (36 in, its top and type) and the note's, the
    /// rule, the answers' row (its top, height, the room at its sides and between the answers), the answers' type and
    /// Retreat's, the tip's half height and length.
    /// </summary>
    public const float QuestionIn = 36, QuestionTop = 36, QuestionText = 38, NoteTop = 90, NoteText = 32, QuestionRule = 146,
        AnswersTop = 168, AnswerHeight = 138, AnswersSide = 20, AnswerGap = 12, AnswerText = 34, RetreatText = 40, TipHalf = 22,
        TipLength = 24;

    /// <summary>
    /// The retreat question beside <paramref name="retreat"/>, the left column's button: over the right column, its top level
    /// with the button's, kept over the board's foot (<paramref name="height"/>).
    /// </summary>
    public static PhoneFrame.Box RetreatQuestion(float height, PhoneFrame.Box retreat)
    {
        var (x, _) = Column(true);
        float left = x - Layout(QuestionOut), h = Layout(QuestionHeight);
        float y = Math.Min(retreat.Y, height - Layout(DeckFoot) - h);
        return new PhoneFrame.Box(left, Math.Max(DeckTopY, y), Width - Layout(Edge) - left, h);
    }

    /// <summary>The question's answers in one row (<paramref name="count"/> of them, left to right), the warning one last.</summary>
    public static IReadOnlyList<PhoneFrame.Box> RetreatAnswers(PhoneFrame.Box question, int count)
    {
        count = Math.Max(1, count);
        float side = Layout(AnswersSide), gap = Layout(AnswerGap);
        float w = (question.W - 2 * side - (count - 1) * gap) / count;
        return [.. Enumerable.Range(0, count).Select(i =>
            new PhoneFrame.Box(question.X + side + i * (w + gap), question.Y + Layout(AnswersTop), w, Layout(AnswerHeight)))];
    }
}
