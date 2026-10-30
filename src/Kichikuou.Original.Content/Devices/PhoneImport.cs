namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The import screen on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23: LI, LIp, LIr, the
/// owner's choice as drawn): our paper sheet in the frame at the phone's type, the sides empty; the steps in two columns —
/// the first, with the folder's field, at the left (<see cref="Column"/> 0), the others at the right —, the red button at
/// the right column's foot (<see cref="Button"/>: Import, or Choose another folder once refused) and Back to the title
/// right of it at the right thumb (<see cref="Back"/>: a way back stands right of the action, owner, 2026-10-30; without
/// a way back the red button alone at the thumb). Refused, GameFiles' message runs
/// across the sheet, its hashes in two lines under it (<see cref="HashLines"/>), the field at the left, the stamp at the
/// right (<see cref="StampX"/>). A folder is shown as its name over its place (<see cref="FolderLines"/>), a path as its
/// parts with Android's storage named (<see cref="Readable(string)"/>). The figures are the mock-ups'
/// (<c>tools/ui/mock_phone.py</c>, <c>import_land</c>, <c>sheet_head</c>, <c>step_mark</c>, <c>step_text</c>,
/// <c>folder_field</c>, <c>hash_lines</c>, <c>stamp</c>) in the reference phone's pixels, the boxes in the canvas's units; a
/// longer screen keeps the sheet in the frame, its sides wider. Interface only. Engine-independent.
/// </summary>
public static class PhoneImport
{
    /// <summary>The lacquer panel: in from the frame's sides, from the screen's top, its foot this far over the gesture zone; the paper in from the panel.</summary>
    private const float PanelIn = 10, PanelTop = 16, PanelFootOver = 4, PaperIn = 26;

    /// <summary>The lacquer panel round the sheet.</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f)
    {
        float x = f.Frame.X + f.Px(PanelIn), y = f.Px(PanelTop), bottom = f.Screen.H - f.Safe.Bottom - f.Px(PanelFootOver);
        return new PhoneFrame.Box(x, y, f.Frame.Right - f.Px(PanelIn) - x, bottom - y);
    }

    /// <summary>The paper sheet.</summary>
    public static PhoneFrame.Box Paper(PhoneFrame f)
    {
        var panel = Panel(f);
        float d = f.Px(PaperIn);
        return new PhoneFrame.Box(panel.X + d, panel.Y + d, panel.W - 2 * d, panel.H - 2 * d);
    }

    // ------------------------------------------------------------------ the sheet's head (sheet_head)

    /// <summary>The heading (the brush face, shrunk to the sheet's width less <see cref="HeadingRoom"/>) and the version under it (Cinzel).</summary>
    public const float HeadingText = 66, HeadingRoom = 80, VersionText = 28;

    /// <summary>The heading's and the version's middles, the rule under them, the steps' first line: this far under the sheet's top.</summary>
    private const float HeadingDown = 90, VersionDown = 166, RuleDown = 206, StepsDown = 262;

    /// <summary>The rule's ends in from the sheet's sides; its width.</summary>
    public const float RuleIn = 40, RuleWidth = 3;

    /// <summary>The heading's middle.</summary>
    public static float HeadingMiddle(PhoneFrame f) => Paper(f).Y + f.Px(HeadingDown);

    /// <summary>The version's middle.</summary>
    public static float VersionMiddle(PhoneFrame f) => Paper(f).Y + f.Px(VersionDown);

    /// <summary>The rule under the head: its ends and its height.</summary>
    public static (float Left, float Right, float Y) Rule(PhoneFrame f)
    {
        var paper = Paper(f);
        return (paper.X + f.Px(RuleIn), paper.Right - f.Px(RuleIn), paper.Y + f.Px(RuleDown));
    }

    /// <summary>The first step's first line's middle, in both columns.</summary>
    public static float StepsTop(PhoneFrame f) => Paper(f).Y + f.Px(StepsDown);

    // ------------------------------------------------------------------ the columns and a step (step_mark, step_text)

    /// <summary>The columns: in from the sheet's sides, this far either side of the frame's middle.</summary>
    private const float ColumnIn = 50, MiddleGap = 40;

    /// <summary>Column <paramref name="index"/>'s ends: 0 the left (the folder), 1 the right (the check, Play).</summary>
    public static (float Left, float Right) Column(PhoneFrame f, int index)
    {
        var paper = Paper(f);
        float middle = f.Frame.X + f.Frame.W / 2;
        return index == 0
            ? (paper.X + f.Px(ColumnIn), middle - f.Px(MiddleGap))
            : (middle + f.Px(MiddleGap), paper.Right - f.Px(ColumnIn));
    }

    /// <summary>A step's seal: its centre in from the column's left, its radius, its numeral's type, its ring's width.</summary>
    public const float MarkIn = 34, MarkRadius = 34, MarkText = 36, MarkLine = 3;

    /// <summary>A step's words in from the column's left.</summary>
    private const float TextIn = 100;

    /// <summary>A step's seal's centre x in column <paramref name="index"/>.</summary>
    public static float MarkX(PhoneFrame f, int index) => Column(f, index).Left + f.Px(MarkIn);

    /// <summary>A step's words' left in column <paramref name="index"/>.</summary>
    public static float TextLeft(PhoneFrame f, int index) => Column(f, index).Left + f.Px(TextIn);

    /// <summary>A step's title and its lines under it: their type and the distance from a line's middle to the next's.</summary>
    public const float TitleText = 48, TitleLine = 62, BodyText = 38, BodyLine = 50;

    /// <summary>The room under a step's last line; the room before Play under the check's step (a folder chosen).</summary>
    public const float StepGap = 14, PlayGap = 30;

    // ------------------------------------------------------------------ the folder's field (folder_field)

    /// <summary>The field's height; its name's, its place's and Choose…'s type.</summary>
    public const float FieldHeight = 150, FieldName = 44, FieldPlace = 30, FieldChoose = 40;

    /// <summary>The field's icon in from its left; its words in from its left; Choose…'s right in from its right; the name's and the place's middles from the field's.</summary>
    public const float FieldIconIn = 30, FieldTextIn = 110, FieldChooseIn = 28, FieldNameUp = 22, FieldPlaceDown = 30;

    /// <summary>The field from <paramref name="top"/>, under the first step's words in the left column.</summary>
    public static PhoneFrame.Box Field(PhoneFrame f, float top)
    {
        float x = TextLeft(f, 0);
        return new PhoneFrame.Box(x, top, Column(f, 0).Right - x, f.Px(FieldHeight));
    }

    // ------------------------------------------------------------------ importing (LIp)

    /// <summary>A step done's tick: its middle in from its column's right; the step's words stop this far from it.</summary>
    private const float TickIn = 20, TickRoom = 60;

    /// <summary>The tick's centre x of a step done in column <paramref name="index"/> (the folder's, LIp; the scenario read).</summary>
    public static float TickX(PhoneFrame f, int index) => Column(f, index).Right - f.Px(TickIn);

    /// <summary>A step done's words' right in column <paramref name="index"/>, left of its tick.</summary>
    public static float TickedRight(PhoneFrame f, int index) => Column(f, index).Right - f.Px(TickRoom);

    /// <summary>The decompile's bar: its height; the share's middle under its top and its type; Play's middle under its top.</summary>
    public const float BarHeight = 44, ShareDown = 92, ShareText = 38, PlayUnderBar = 200;

    // ------------------------------------------------------------------ refused (LIr)

    /// <summary>The hashes' type and the distance between their lines' middles; the room under them.</summary>
    public const float HashText = 30, HashLine = 40, HashGap = 10;

    /// <summary>The stamp: its centre in from the right column's left (level with the field), its radius, its ring's width.</summary>
    public const float StampIn = 260, StampRadius = 110, StampLine = 7;

    /// <summary>The stamp's centre x.</summary>
    public static float StampX(PhoneFrame f) => Column(f, 1).Left + f.Px(StampIn);

    // ------------------------------------------------------------------ the foot: the red button and Back

    /// <summary>The red button: this far over the sheet's foot, as high; Import's width and Choose another folder's.</summary>
    private const float ButtonsUp = 30, ButtonHeight = 130, ImportWidth = 460, ChooseWidth = 640;

    /// <summary>The red button's word (the brush face): Import's, Choose another folder's.</summary>
    public const float ImportText = 54, ChooseText = 50;

    /// <summary>
    /// The red button at the right column's foot: Import, or (<paramref name="refused"/>) Choose another folder; at the
    /// column's left with a way back right of it (<paramref name="back"/>), else alone at its right, the right thumb.
    /// </summary>
    public static PhoneFrame.Box Button(PhoneFrame f, bool refused, bool back = true)
    {
        var (left, right) = Column(f, 1);
        float w = f.Px(refused ? ChooseWidth : ImportWidth), bottom = Paper(f).Bottom - f.Px(ButtonsUp);
        return new PhoneFrame.Box(back ? left : right - w, bottom - f.Px(ButtonHeight), w, f.Px(ButtonHeight));
    }

    /// <summary>Back: its arrow's width and half height, its word in from the arrow's tip (Cinzel); the box a finger takes.</summary>
    public const float ArrowWidth = 24, ArrowHalf = 16, BackTextIn = 44, BackText = 34;

    private const float BackGap = 20;

    /// <summary>
    /// Back, level with the red button: from 20 right of it to the right column's right, the right thumb ("Back to the
    /// title", or once <paramref name="refused"/> "Back"). Its arrow's tip is the box's left, at its middle.
    /// </summary>
    public static PhoneFrame.Box Back(PhoneFrame f, bool refused)
    {
        var button = Button(f, refused);
        float x = button.Right + f.Px(BackGap);
        return new PhoneFrame.Box(x, button.Y, Column(f, 1).Right - x, button.H);
    }

    /// <summary>Back's word: the whole way beside Import, short beside Choose another folder.</summary>
    public static string BackWord(bool refused) => refused ? "Back" : "Back to the title";

    // ------------------------------------------------------------------ the words

    /// <summary>Where Android keeps the phone's own storage, and what it is called (<c>/sdcard</c> is the same place).</summary>
    private static readonly string[] Storage = ["/storage/emulated/0", "/sdcard"];

    private const string StorageName = "Internal storage", Between = " › ";

    /// <summary>
    /// <paramref name="path"/> as the phone shows a place: its folders joined by " › ", Android's own storage named "Internal
    /// storage" (<c>/storage/emulated/0/Games/Kichikuou Rance</c> is "Internal storage › Games › Kichikuou Rance").
    /// </summary>
    public static string Readable(string path) => string.Join(Between, Parts(path));

    /// <summary><paramref name="text"/> (a refusal) with <paramref name="folder"/>'s path in it as <see cref="Readable(string)"/> shows it.</summary>
    public static string Readable(string text, string folder) =>
        folder.Length == 0 ? text : text.Replace(folder, Readable(folder), StringComparison.Ordinal);

    /// <summary>A folder in its field: its name, and the place it is in under it (empty for a root).</summary>
    public static (string Name, string Place) FolderLines(string path)
    {
        var parts = Parts(path);
        return parts.Count switch
        {
            0 => (path, ""),
            1 => (parts[0], ""),
            _ => (parts[^1], string.Join(Between, parts.Take(parts.Count - 1))),
        };
    }

    private static List<string> Parts(string path)
    {
        string rest = path.Replace('\\', '/');
        var parts = new List<string>();
        foreach (string root in Storage)
        {
            if (rest == root || rest.StartsWith(root + "/", StringComparison.Ordinal))
            {
                parts.Add(StorageName);
                rest = rest[root.Length..];
                break;
            }
        }
        parts.AddRange(rest.Split('/', StringSplitOptions.RemoveEmptyEntries));
        return parts;
    }

    /// <summary>
    /// The refusal's hashes (as the import screen keeps them: "its SHA-256 is …, … is expected") in two lines across the
    /// sheet — the file's, the expected one —, each ending as a sentence's part does; one line when it is not of that form.
    /// </summary>
    public static IReadOnlyList<string> HashLines(string hashes)
    {
        int at = hashes.IndexOf(", ", StringComparison.Ordinal);
        if (at < 0)
            return hashes.Length == 0 ? [] : [hashes];
        string second = hashes[(at + 2)..].TrimEnd('.');
        return [hashes[..(at + 1)], second + "."];
    }
}
