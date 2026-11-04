namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The import screen on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22: PI, PIp, PIr, the
/// owner's choice as drawn): our paper sheet as a card of the PC's kind in the room's middle at the phone's type, the steps
/// 一 二 三 one under another (<see cref="MarkX"/>, <see cref="TextLeft"/>, <see cref="Right"/>), the folder's field under
/// the first (<see cref="Field"/>), the next step <see cref="FieldUnder"/> under it; importing, the decompile's bar under the
/// second step's words; refused, the hashes under the message and the stamp under the field (<see cref="StampX"/>,
/// <see cref="StampDown"/>); the red button | Back in the bar, Back at the thumb, as Save/Load's (<see cref="Back"/>,
/// <see cref="Button"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>import_upright</c>,
/// <c>sheet_head</c>, <c>step_mark</c>, <c>step_text</c>, <c>folder_field</c>, <c>hash_lines</c>, <c>stamp</c>,
/// <c>back_and</c>) in the reference phone's pixels from the room and the bar; the boxes in the canvas's units. The type is
/// held landscape's (<see cref="PhoneImport"/>). Interface only. Engine-independent.
/// </summary>
public static class PhoneImportPortrait
{
    /// <summary>The card's distance from the room's sides, its height (less on a shorter screen), the paper in from it.</summary>
    private const float SideIn = 16, CardHeight = 1560, PaperIn = 26;

    /// <summary>The lacquer card round the sheet, in the middle between the room's top and the bar.</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f)
    {
        float h = Math.Min(f.Px(CardHeight), Math.Max(0, f.Bar.Y - f.Room.Y - f.Px(2 * SideIn)));
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), (f.Room.Y + f.Bar.Y) / 2 - h / 2, Math.Max(0, f.Room.W - f.Px(2 * SideIn)), h);
    }

    /// <summary>The paper sheet.</summary>
    public static PhoneFrame.Box Paper(PhoneFrame f)
    {
        var panel = Panel(f);
        float d = f.Px(PaperIn);
        return new PhoneFrame.Box(panel.X + d, panel.Y + d, Math.Max(0, panel.W - 2 * d), Math.Max(0, panel.H - 2 * d));
    }

    // ------------------------------------------------------------------ the sheet's head (sheet_head), as held landscape's

    private const float HeadingDown = 90, VersionDown = 166, RuleDown = 206, StepsDown = 262;

    /// <summary>The heading's middle under the paper's top.</summary>
    public static float HeadingMiddle(PhoneFrame f) => Paper(f).Y + f.Px(HeadingDown);

    /// <summary>The version's middle under the heading.</summary>
    public static float VersionMiddle(PhoneFrame f) => Paper(f).Y + f.Px(VersionDown);

    /// <summary>The rule under the version: its ends and its height.</summary>
    public static (float Left, float Right, float Y) Rule(PhoneFrame f)
    {
        var paper = Paper(f);
        return (paper.X + f.Px(PhoneImport.RuleIn), paper.Right - f.Px(PhoneImport.RuleIn), paper.Y + f.Px(RuleDown));
    }

    /// <summary>The first step's title's middle.</summary>
    public static float StepsTop(PhoneFrame f) => Paper(f).Y + f.Px(StepsDown);

    // ------------------------------------------------------------------ the steps, one under another

    /// <summary>A step's seal's centre and its words' left in from the paper's left; their right in from its right.</summary>
    private const float MarkIn = 76, TextIn = 140, RightIn = 40;

    /// <summary>A step's seal's centre x.</summary>
    public static float MarkX(PhoneFrame f) => Paper(f).X + f.Px(MarkIn);

    /// <summary>A step's words' left.</summary>
    public static float TextLeft(PhoneFrame f) => Paper(f).X + f.Px(TextIn);

    /// <summary>The steps' words' right, the field's, the bar's.</summary>
    public static float Right(PhoneFrame f) => Paper(f).Right - f.Px(RightIn);

    /// <summary>The folder's field from <paramref name="top"/>, under the first step's words.</summary>
    public static PhoneFrame.Box Field(PhoneFrame f, float top) =>
        new(TextLeft(f), top, Math.Max(0, Right(f) - TextLeft(f)), f.Px(PhoneImport.FieldHeight));

    /// <summary>The next step's middle under the field's foot.</summary>
    public const float FieldUnder = 70;

    /// <summary>The room between a step and the next where the mock-ups leave it (Play under the check, the stage under the folder ticked).</summary>
    public const float StepRoom = 30;

    // ------------------------------------------------------------------ importing (PIp)

    /// <summary>A step done's tick: its middle in from the words' right; the step's words stop this far from that right.</summary>
    private const float TickIn = 20, TickRoom = 60;

    /// <summary>The tick's centre x of a step done.</summary>
    public static float TickX(PhoneFrame f) => Right(f) - f.Px(TickIn);

    /// <summary>A step done's words' right, left of its tick.</summary>
    public static float TickedRight(PhoneFrame f) => Right(f) - f.Px(TickRoom);

    /// <summary>Play's middle under the decompile's bar's top (the share's is held landscape's).</summary>
    public const float PlayUnderBar = 180;

    // ------------------------------------------------------------------ refused (PIr)

    /// <summary>The stamp's centre in from the words' right and under the field's top.</summary>
    private const float StampIn = 140;

    /// <summary>The stamp's centre under the field's top.</summary>
    public const float StampDown = 360;

    /// <summary>The stamp's centre x.</summary>
    public static float StampX(PhoneFrame f) => Right(f) - f.Px(StampIn);

    // ------------------------------------------------------------------ the bar (back_and)

    /// <summary>Back's and the red button's type (the brush face).</summary>
    public const float BackText = PhoneSavesPortrait.BackText, ButtonText = PhoneSavesPortrait.ButtonText;

    /// <summary>Back at the bar's right, the right thumb, as Save/Load's.</summary>
    public static PhoneFrame.Box Back(PhoneFrame f) => PhoneSavesPortrait.Back(f);

    /// <summary>
    /// The red button (Import, Choose another folder) left of Back, as Save/Load's; without a way back
    /// (<paramref name="back"/> false: a first launch) as wide, alone at the bar's right, the right thumb.
    /// </summary>
    public static PhoneFrame.Box Button(PhoneFrame f, bool back = true)
    {
        var button = PhoneSavesPortrait.Button(f);
        return back ? button : new PhoneFrame.Box(f.Bar.Right - button.W, button.Y, button.W, button.H);
    }
}
