namespace Kichikuou.Engine.Script;

/// <summary>
/// xsystem35's message skip (<c>msgskip.c</c>, the menu bar's <c>MsgSkip[off]</c>, F1): an engine
/// switch, not a rule. While it is on (<see cref="IsSkipping"/>), the skippable waits — the
/// message waits and the <c>Y 3</c> pauses — return at once with no key, so the script runs on as
/// if nothing was pressed. It turns itself off on a click or key during a skipped wait (the
/// input's part, <c>STOP_ON_CLICK</c>), when a selection menu opens (<see cref="OnMenu"/>,
/// <c>STOP_ON_MENU</c>) and, unless <see cref="SkipUnseen"/>, at a message not seen before
/// (<see cref="OnMessage"/>, <c>STOP_ON_UNSEEN</c>), which also disables it (<see cref="Enabled"/>)
/// until a seen message comes. docs/window-menu.md.
/// </summary>
/// <remarks>
/// xsystem35 keeps the seen messages in a Bloom filter keyed by the message's scenario address;
/// here they are the exact set of <see cref="ScriptRef"/>s, one per line of a text file.
/// </remarks>
public sealed class MessageSkip
{
    private readonly HashSet<string> _seen;
    private readonly Action<string>? _recordSeen;
    private readonly Action? _forgetSeen;
    private bool _skipUnseen = true;

    /// <param name="seen">The messages seen before, as <see cref="Key"/>s.</param>
    /// <param name="recordSeen">Called with each message seen for the first time.</param>
    /// <param name="forgetSeen">Called when the seen messages are forgotten (<see cref="ForgetSeen"/>).</param>
    public MessageSkip(IEnumerable<string> seen, Action<string>? recordSeen = null, Action? forgetSeen = null)
    {
        _seen = new HashSet<string>(seen, StringComparer.Ordinal);
        _recordSeen = recordSeen;
        _forgetSeen = forgetSeen;
    }

    /// <summary>
    /// <c>MSGSKIP_SKIP_UNSEEN</c>: messages not seen before are skipped too (on by default: the
    /// owner's xsystem35 for Windows keeps no seen messages without a <c>.gr</c> file, so it skips
    /// everything). Turning it on allows skipping again at once.
    /// </summary>
    public bool SkipUnseen
    {
        get => _skipUnseen;
        set
        {
            _skipUnseen = value;
            if (value && !Enabled)
            {
                Enabled = true;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Ours (Option → MsgSkip Speeds Up Fades; on by default, for the launch, not saved): while
    /// skipping (<see cref="IsSkipping"/>), the palette fades are cut short. xsystem35 never shortens
    /// them (<c>ags_runEffect</c> waits without <c>KEYWAIT_SKIPPABLE</c>); off, they keep their length
    /// as there. Display only: no fade changes what the script reads.
    /// </summary>
    public bool FastFades { get; set; } = true;

    /// <summary>The longest a fade lasts while <see cref="FastFades"/> cuts it short.</summary>
    public static readonly TimeSpan SkippedFade = TimeSpan.FromMilliseconds(150);

    /// <summary>How long a fade of the engine's <paramref name="duration"/> lasts now (<see cref="FastFades"/>).</summary>
    public TimeSpan FadeDuration(TimeSpan duration) =>
        FastFades && IsSkipping && duration > SkippedFade ? SkippedFade : duration;

    /// <summary>
    /// Whether the skip is on inside one of the scenes it skips whole (the variant's
    /// <see cref="VariantProfile.WholeScenes"/>), given the scene files running now (outermost first).
    /// </summary>
    public bool SkipsScene(IReadOnlyList<string> runningScenes, IReadOnlySet<string> wholeScenes)
    {
        if (!IsSkipping)
            return false;
        foreach (string file in runningScenes)
            if (wholeScenes.Contains(file))
                return true;
        return false;
    }

    /// <summary>Whether skipping is allowed now (false at a message not seen before; the menu item is greyed out).</summary>
    public bool Enabled { get; private set; } = true;

    /// <summary>Whether the player turned skipping on.</summary>
    public bool Activated { get; private set; }

    /// <summary>Whether a selection menu turns skipping off (<c>IY 2</c> / <c>IY 3</c>; the scenario never changes it).</summary>
    public bool StopOnMenu { get; set; } = true;

    /// <summary><c>msgskip_isSkipping</c>: the skippable waits return at once.</summary>
    public bool IsSkipping => Enabled && Activated;

    /// <summary>The number of messages seen.</summary>
    public int SeenCount => _seen.Count;

    /// <summary><see cref="Enabled"/> or <see cref="Activated"/> changed.</summary>
    public event Action? Changed;

    /// <summary><c>msgskip_activate</c>: turns skipping on or off.</summary>
    public void Activate(bool on)
    {
        if (Activated == on)
            return;
        Activated = on;
        Changed?.Invoke();
    }

    /// <summary>The menu item or F1: on ↔ off.</summary>
    public void Toggle() => Activate(!Activated);

    /// <summary>
    /// <c>msgskip_onMessage</c>: a text was printed at <paramref name="at"/>. A message not seen
    /// before is recorded, turns skipping off and disables it; a seen one enables it.
    /// </summary>
    public void OnMessage(ScriptRef at)
    {
        string key = Key(at);
        bool unseen = _seen.Add(key);
        if (unseen)
            _recordSeen?.Invoke(key);
        bool stop = unseen && !SkipUnseen;          // msgskip_action
        if (stop)
            Activate(false);
        if (Enabled == stop)
        {
            Enabled = !stop;
            Changed?.Invoke();
        }
    }

    /// <summary>Forgets every message seen (the menu's "Forget Seen Messages").</summary>
    public void ForgetSeen()
    {
        _seen.Clear();
        _forgetSeen?.Invoke();
    }

    /// <summary><c>sel_select</c>: a selection menu opens.</summary>
    public void OnMenu()
    {
        if (StopOnMenu)
            Activate(false);
    }

    /// <summary>The key of a message: its file and line in the decompiled scenario.</summary>
    public static string Key(ScriptRef at) => $"{at.File}:{at.Line}";

    /// <summary>
    /// The skip state over a text file of seen messages (created when missing): read now, each
    /// newly seen message appended at once, deleted when they are forgotten.
    /// </summary>
    public static MessageSkip Open(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } directory)
            Directory.CreateDirectory(directory);
        var seen = File.Exists(path) ? File.ReadLines(path).Where(line => line.Length > 0) : [];
        return new MessageSkip(seen.ToList(), key => File.AppendAllText(path, key + "\n"), () => File.Delete(path));
    }
}
