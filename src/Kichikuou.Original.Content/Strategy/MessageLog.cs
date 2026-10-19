namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a page of the message log is.</summary>
public enum LogKind
{
    /// <summary>A speech: 111/121, or 131 with <c>Name「…」</c>.</summary>
    Speech,

    /// <summary>A thought: 112/122, or 131 with <c>Name（…）</c>.</summary>
    Thought,

    /// <summary>Narration: 113/123, 131 without a name (the status windows too, kept at their cells).</summary>
    Narration,

    /// <summary>A place's title (113 at <c>ZM 32</c>): the place and its room.</summary>
    Place,

    /// <summary>A question's menu answered over a message window: its items, the chosen one.</summary>
    Choice,
}

/// <summary>
/// A page of the message log: one key wait of a message window — its kind, its speaker (empty when none is known: narration,
/// a window without its portrait), its lines at the script's breaks — or a menu's answer (<see cref="Lines"/> its items,
/// <see cref="Chosen"/> the chosen one, from 1).
/// </summary>
public sealed record LogPage(LogKind Kind, string Speaker, IReadOnlyList<string> Lines, int Chosen = 0);

/// <summary>
/// A ruled line of the log as our chronicle shows it (mock-up L3a): <see cref="Label"/> in the margin (the speaker's name at a
/// run's first line, "CHOICE" at a choice), the text — of a choice the chosen item alone, <see cref="Chosen"/>.
/// </summary>
public readonly record struct LogLine(LogKind Kind, string Label, string Text, bool Chosen = false);

/// <summary>
/// The message log (docs/ui/messages.md, §5 step 6; the owner's choices of 2026-10-19): the pages of the message windows the
/// player was asked a key for, the message skip's included, and the answers of the questions asked over them, kept for the
/// launch (the last <see cref="Capacity"/> pages; not saved). Presentation only: the script's waits are untouched.
/// </summary>
public sealed class MessageLog
{
    /// <summary>The pages kept: the oldest go first.</summary>
    public const int Capacity = 500;

    /// <summary>The label of a choice's first item.</summary>
    public const string ChoiceLabel = "CHOICE";

    private readonly List<LogPage> _pages = [];

    /// <summary>The pages, the oldest first.</summary>
    public IReadOnlyList<LogPage> Pages => _pages;

    /// <summary>Raised when a page is added.</summary>
    public event Action? Changed;

    /// <summary>Adds <paramref name="page"/>, dropping the oldest past <see cref="Capacity"/>.</summary>
    public void Add(LogPage page)
    {
        _pages.Add(page);
        if (_pages.Count > Capacity)
            _pages.RemoveRange(0, _pages.Count - Capacity);
        Changed?.Invoke();
    }

    /// <summary>Forgets every page.</summary>
    public void Clear()
    {
        _pages.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Whether <paramref name="file"/> is an event scene (<c>G*</c>, <c>I*</c>), whose narration and menus belong to the log —
    /// unlike the play's files (owner, 2026-10-19): <c>MAIN</c>'s week banner, revenue and expenses, <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s load
    /// report and slots, <c>COMMAND</c>'s menu, the command files' <c>C*</c>, the pickers' <c>S*</c>, <c>各種DATA</c>, the
    /// battle's <c>FIGT*</c>, the title's <c>ｽﾀMENU</c>. The play's files have dialogues too (<c>Cﾃｲﾄ</c>'s Maris): their
    /// speeches are kept (<see cref="Keeps"/>), and a menu asked under a speech (<c>GamePresenter</c>).
    /// </summary>
    public static bool IsEventScene(string file) => file.Length > 0 && file[0] is 'G' or 'I';

    /// <summary>
    /// Whether <paramref name="page"/>, its text from <paramref name="file"/> (null: not known), belongs to the log: a speech or a
    /// thought from any file; narration and a place's title from an event scene only (<see cref="IsEventScene"/>).
    /// </summary>
    public static bool Keeps(LogPage page, string? file) =>
        page.Kind is LogKind.Speech or LogKind.Thought or LogKind.Choice || file is not null && IsEventScene(file);

    /// <summary>
    /// The page a message window shows at its key wait: <paramref name="window"/>'s kind and lines, the speaker of 111/112 the
    /// left portrait's (<paramref name="left"/>, <c>Ui_PortraitL</c>), of 121/122 the right one's, named by
    /// <paramref name="name"/> (empty when unknown or no portrait is up); 131 names its own. Null when the window holds nothing.
    /// </summary>
    public static LogPage? PageOf(MessageWindow window, int left, int right, Func<int, string> name)
    {
        if (window.Kind == MessageKind.PlaceTitle)
            return new LogPage(LogKind.Place, "", window.Subtitle.Length > 0 ? [window.Title, window.Subtitle] : [window.Title]);
        var lines = window.Lines.ToList();
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);
        while (lines.Count > 0 && lines[0].Trim().Length == 0)
            lines.RemoveAt(0);
        if (lines.Count == 0)
            return null;
        string Portrait(int cg) => cg > 0 ? name(cg) : "";
        return window.Kind switch
        {
            MessageKind.SpeechLeft => new LogPage(LogKind.Speech, Portrait(left), lines),
            MessageKind.SpeechRight => new LogPage(LogKind.Speech, Portrait(right), lines),
            MessageKind.ThoughtLeft => new LogPage(LogKind.Thought, Portrait(left), lines),
            MessageKind.ThoughtRight => new LogPage(LogKind.Thought, Portrait(right), lines),
            MessageKind.CgBar when window.Speaker.Length > 0 =>
                new LogPage(window.Thinks ? LogKind.Thought : LogKind.Speech, window.Speaker, lines),
            _ => new LogPage(LogKind.Narration, "", lines),
        };
    }

    /// <summary>
    /// The pages as ruled lines (mock-up L3a): the lines close, no blank line between pages; the speaker's name at the first
    /// line of a run of pages of one speaker and kind; a place's title as one heading line ("Leazas Castle — Audience Room");
    /// the chosen item of a choice under the label "CHOICE" (owner, 2026-10-19: not the whole menu).
    /// </summary>
    public IReadOnlyList<LogLine> Lines()
    {
        var lines = new List<LogLine>();
        LogPage? previous = null;
        foreach (var page in _pages)
        {
            switch (page.Kind)
            {
                case LogKind.Place:
                    lines.Add(new LogLine(LogKind.Place, "", string.Join(" — ", page.Lines)));
                    break;
                case LogKind.Choice when page.Chosen >= 1 && page.Chosen <= page.Lines.Count:
                    lines.Add(new LogLine(LogKind.Choice, ChoiceLabel, page.Lines[page.Chosen - 1], true));   // the chosen item alone (owner, 2026-10-19)
                    break;
                case LogKind.Choice:
                    break;
                default:
                    bool run = previous is { } p && p.Kind == page.Kind && p.Speaker == page.Speaker;
                    for (int i = 0; i < page.Lines.Count; i++)
                        lines.Add(new LogLine(page.Kind, i == 0 && !run ? page.Speaker : "", page.Lines[i]));
                    break;
            }
            previous = page;
        }
        return lines;
    }
}
