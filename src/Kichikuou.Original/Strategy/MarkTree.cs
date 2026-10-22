using Kichikuou.Engine.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>A talk's or a visit's mark (owner's decision 30, docs/ui/talk-marks.md §2): the cell's NEW, ??? or nothing.</summary>
public enum SceneMark
{
    /// <summary>Nothing new on any path: no mark.</summary>
    Read,

    /// <summary>"???": something new is only possible — a draw decides.</summary>
    Maybe,

    /// <summary>NEW: something new is certain, or the player reaches it by a menu.</summary>
    New,
}

/// <summary>How a draw's outcome is chosen (docs/ui/talk-marks.md §5, §8 step 5).</summary>
public enum DrawRule
{
    /// <summary>(c) The draw keeps its odds: any value may come.</summary>
    Odds,

    /// <summary>(a) A harmless unread variant comes: one of the values whose continuation shows a line not read and changes nothing that counts.</summary>
    HarmlessUnread,

    /// <summary>(b) A listed draw that changes the state, its usual scene read: one of the values that change it.</summary>
    Listed,
}

/// <summary>
/// The choice at a draw (<see cref="MarkTree.Choose"/>): the rule and the values the outcome is drawn among, at random.
/// For <see cref="DrawRule.Odds"/> they are the draw's outcomes (<see cref="MarkNode.Outcomes"/>) and a real draw keeps
/// the value it drew, whatever it is.
/// </summary>
public sealed record DrawChoice(DrawRule Rule, IReadOnlyList<int> Values);

/// <summary>
/// What the marks are computed with: whether "New scenes first" (the rule change, docs/ui/talk-marks.md §5) is on, and
/// the draws that change the state with the mark each gets once its usual scene is read (<see cref="SceneMarks.ListedDraws"/>).
/// </summary>
public sealed record MarkRules(bool NewScenesFirst, IReadOnlyDictionary<ScriptRef, SceneMark> ListedDraws);

/// <summary>
/// One path of the dry run with what the marks read of it: its counted changes from the start
/// (<see cref="SceneMarkState.Changes"/>) and, as of the start, whether each of its lines was unread.
/// </summary>
public sealed class MarkLeaf
{
    /// <summary>
    /// A path with its changes; <paramref name="unread"/> tells a line not read at the start (the command's own lines
    /// excluded by the caller), asked once per line here.
    /// </summary>
    public MarkLeaf(PreviewPath path, IReadOnlyCollection<StateChange> changes, Func<ScriptRef, bool> unread)
    {
        Path = path;
        Changes = changes;
        _unread = [.. path.Lines.Select(unread)];
        FirstUnread = Array.IndexOf(_unread, true);
        LastUnread = Array.LastIndexOf(_unread, true);
    }

    private readonly bool[] _unread;

    /// <summary>The path.</summary>
    public PreviewPath Path { get; }

    /// <summary>What counts that differs at its end from the start.</summary>
    public IReadOnlyCollection<StateChange> Changes { get; }

    /// <summary>The index of its first line unread at the start, -1 for none.</summary>
    public int FirstUnread { get; }

    /// <summary>The index of its last line unread at the start, -1 for none.</summary>
    public int LastUnread { get; }

    /// <summary>
    /// A path that ends the game (a jump out — an ending, a game over —, or a quit): left out whole
    /// (docs/ui/talk-marks.md §2: the mark gives no reason to end the game).
    /// </summary>
    public bool LeftOut => Path.End is PreviewEnd.Jumped or PreviewEnd.Quit;

    /// <summary>
    /// A path cut where it met a decision again with the same memory (<see cref="PreviewEnd.Repeated"/>): the value that led
    /// there only comes back to an earlier decision (a menu's cancel, a draw drawn again), so it is no outcome of its own.
    /// </summary>
    public bool Repeats => Path.End == PreviewEnd.Repeated;

    /// <summary>Whether the path returned or was cut at a repeat (<see cref="Repeats"/>): neither ends the game nor stops.</summary>
    internal bool Plain => Path.End is PreviewEnd.Returned or PreviewEnd.Repeated;

    /// <summary>A path the preview stopped (a battle, the party picker, save data, a load): something happens there.</summary>
    public bool Stopped => Path.End is PreviewEnd.Stopped or PreviewEnd.Loaded;

    /// <summary>Whether lines <paramref name="from"/>..<paramref name="to"/> (exclusive) hold one unread at the start.</summary>
    public bool UnreadIn(int from, int to)
    {
        if (FirstUnread < 0 || LastUnread < from || FirstUnread >= to)
            return false;
        for (int i = Math.Max(from, FirstUnread); i < to && i <= LastUnread; i++)
            if (_unread[i])
                return true;
        return false;
    }

    /// <summary>Why the path is new, or null: an unread line, a change, a stop; or why it is left out.</summary>
    public string? Why => Path.End switch
    {
        PreviewEnd.Jumped or PreviewEnd.Quit => $"left out ({Path.End}{(Path.Reason is { } r ? $" {r}" : "")})",
        PreviewEnd.Stopped or PreviewEnd.Loaded => $"stopped ({Path.Reason})",
        PreviewEnd.Aborted => $"aborted ({Path.Reason})",
        PreviewEnd.Repeated => "repeats",
        _ when FirstUnread >= 0 => $"unread {Path.Lines[FirstUnread]}",
        _ when Changes.Count > 0 => $"changes {string.Join(", ", Changes)}",
        _ => null,
    };
}

/// <summary>
/// A node of the paths' tree (<see cref="MarkTree"/>): a decision (a draw or a menu) with a child per value, or a
/// path's end (<see cref="Leaf"/>). Every path through a node shows the same lines up to its decision; its own
/// lines (its segment) start at <see cref="SegmentStart"/> — the lines its parent's value led to.
/// </summary>
public sealed class MarkNode
{
    private readonly Dictionary<int, MarkNode> _children = [];
    private readonly List<MarkLeaf> _leaves = [];

    internal MarkNode(int depth, int segmentStart)
    {
        Depth = depth;
        SegmentStart = segmentStart;
    }

    /// <summary>How many decisions come before it.</summary>
    public int Depth { get; }

    /// <summary>The index in its paths' lines where its own lines start.</summary>
    public int SegmentStart { get; }

    /// <summary>The index in its paths' lines of its decision (its lines' end), for a decision.</summary>
    public int DecisionLine { get; private set; }

    /// <summary>Its decision (kind, count, place; its value is the first path's), or null for a leaf.</summary>
    public PreviewDecision? Decision { get; private set; }

    /// <summary>The path that ends here, for a leaf.</summary>
    public MarkLeaf? Leaf { get; private set; }

    /// <summary>Its children by value.</summary>
    public IReadOnlyDictionary<int, MarkNode> Children => _children;

    /// <summary>Every path through it.</summary>
    public IReadOnlyList<MarkLeaf> Leaves => _leaves;

    /// <summary>The values its decision can take: a draw's 1..n, a menu's items 1..n and its cancel 0.</summary>
    public IEnumerable<int> Values => Decision is not { } d ? []
        : d.Kind == PreviewDecisionKind.Draw ? Enumerable.Range(1, d.Count) : Enumerable.Range(1, d.Count).Append(0);

    /// <summary>
    /// Its outcomes: the <see cref="Values"/> but those that only come back to an earlier decision (their path is cut
    /// right there, <see cref="MarkLeaf.Repeats"/>) — a cancel that shows the menu again is no choice, a draw drawn again
    /// until another value comes ends with another value.
    /// </summary>
    public IEnumerable<int> Outcomes => Values.Where(v => !(_children.TryGetValue(v, out var child) && child.Leaf is { Repeats: true }));

    /// <summary>Whether a value of a decision has no path (past the preview's cap) here or below.</summary>
    public bool Incomplete { get; private set; }

    internal void Add(MarkLeaf leaf, IReadOnlyList<int> decisionLines)
    {
        _leaves.Add(leaf);
        var path = leaf.Path;
        if (Depth == path.Decisions.Count)
        {
            if (Leaf is not null || Decision is not null)
                throw new InvalidOperationException($"Two paths end at one node: {string.Join(", ", path.Decisions)}.");
            Leaf = leaf;
            return;
        }
        var decision = path.Decisions[Depth];
        if (Leaf is not null)
            throw new InvalidOperationException($"A path goes on past another's end: {string.Join(", ", path.Decisions)}.");
        if (Decision is { } known)
        {
            if (known.Kind != decision.Kind || known.Count != decision.Count || known.At != decision.At)
                throw new InvalidOperationException($"Two paths meet different decisions at one node: {known} and {decision}.");
        }
        else
        {
            Decision = decision;
            DecisionLine = decisionLines[Depth];
        }
        if (!_children.TryGetValue(decision.Value, out var child))
            _children[decision.Value] = child = new MarkNode(Depth + 1, decisionLines[Depth]);
        child.Add(leaf, decisionLines);
    }

    internal bool Complete()
    {
        bool complete = true;
        foreach (var child in _children.Values)
            complete &= child.Complete();
        if (Decision is not null && Values.Any(v => !_children.ContainsKey(v)))
            complete = false;
        Incomplete = !complete;
        return complete;
    }
}

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §2, §5, §8 step 4): the paths of a talk's or a visit's dry run
/// as a tree of decisions — a menu is the player's choice, a draw is chance — and its mark. A path is new when it
/// shows a line unread at the start, changes what counts (<see cref="SceneMarkState"/>) or is stopped (a battle:
/// something happens); a path that ends the game is left out (counts as read); a path that never ends (aborted) or a
/// value past the preview's cap makes the mark at least ???; a path cut at a repeat is no outcome of its decision.
/// </summary>
/// <remarks>
/// The evaluation, bottom up: a leaf is New or Read as above; a menu takes its best item (the cancel included, unless it
/// only shows the menu again, <see cref="MarkNode.Outcomes"/>); a draw
/// takes what <see cref="Choose"/> gives — (a) a harmless unread variant: New; (b) a listed draw: the listed mark, no
/// better than the chosen values' worst; (c) the odds: New when every value is, Read when every value is, else ???.
/// The rule change is in <see cref="Choose"/> alone, which step 5 calls at a real draw.
/// </remarks>
public sealed class MarkTree
{
    private readonly Dictionary<(MarkNode Node, int From, bool IgnoreChanges, MarkRules Rules), SceneMark> _marks = [];

    private MarkTree(MarkNode root, IReadOnlyList<MarkLeaf> leaves, bool truncated)
    {
        Root = root;
        Leaves = leaves;
        Truncated = truncated;
    }

    /// <summary>The root: the start's first decision, or its only path's end.</summary>
    public MarkNode Root { get; }

    /// <summary>Every path, in the preview's order.</summary>
    public IReadOnlyList<MarkLeaf> Leaves { get; }

    /// <summary>Whether the preview stopped at its cap with paths left (the mark is then at least ???).</summary>
    public bool Truncated { get; }

    /// <summary>
    /// The tree of <paramref name="leaves"/> (every path of one preview, in its order); <paramref name="truncated"/> as the
    /// preview's result says. A path without <see cref="PreviewPath.DecisionLines"/> shows all its lines before its first
    /// decision.
    /// </summary>
    public static MarkTree Build(IReadOnlyList<MarkLeaf> leaves, bool truncated)
    {
        if (leaves.Count == 0)
            throw new ArgumentException("A preview has at least one path.", nameof(leaves));
        var root = new MarkNode(0, 0);
        foreach (var leaf in leaves)
        {
            var path = leaf.Path;
            var lines = path.DecisionLines.Count == path.Decisions.Count
                ? path.DecisionLines
                : [.. Enumerable.Repeat(path.Lines.Count, path.Decisions.Count)];
            root.Add(leaf, lines);
        }
        root.Complete();
        return new MarkTree(root, leaves, truncated);
    }

    /// <summary>The mark under <paramref name="rules"/>.</summary>
    public SceneMark Mark(MarkRules rules)
    {
        var mark = Eval(Root, 0, false, rules);
        return Truncated ? Max(mark, SceneMark.Maybe) : mark;
    }

    /// <summary>The node the decisions <paramref name="prefix"/> lead to, or null when no path takes them.</summary>
    public MarkNode? NodeAt(IReadOnlyList<PreviewDecision> prefix)
    {
        var node = Root;
        foreach (var decision in prefix)
            if (!node.Children.TryGetValue(decision.Value, out node!))
                return null;
        return node;
    }

    /// <summary>
    /// The choice at the draw <paramref name="node"/> (docs/ui/talk-marks.md §8 step 5), the one function the mark and the
    /// real draws use. With "New scenes first" off, the odds. On:
    /// <list type="number">
    /// <item>(a) the values that are harmless (<see cref="IsHarmless"/>) and whose continuation — from the lines the value
    /// leads to on, every decision below evaluated by this same rule, the changes ignored — is New: one of them comes;</item>
    /// <item>(b) at a listed draw, the values that change the state (<see cref="ChangesState"/>), when the lines up to the
    /// draw and every other value's continuation hold no line unread at the start (the usual scene read) and no value is
    /// missing: one of them comes;</item>
    /// <item>(c) otherwise the odds.</item>
    /// </list>
    /// </summary>
    public DrawChoice Choose(MarkNode node, MarkRules rules)
    {
        if (node.Decision is not { Kind: PreviewDecisionKind.Draw } draw)
            throw new ArgumentException("Only a draw is chosen.", nameof(node));
        var all = node.Outcomes.ToArray();
        if (!rules.NewScenesFirst)
            return new DrawChoice(DrawRule.Odds, all);

        var harmless = all.Where(v => IsHarmless(node, v)
            && Eval(node.Children[v], node.Children[v].SegmentStart, true, rules) == SceneMark.New).ToArray();
        if (harmless.Length > 0)
            return new DrawChoice(DrawRule.HarmlessUnread, harmless);

        if (draw.At is { } at && rules.ListedDraws.ContainsKey(at) && !node.Incomplete
            && !node.Leaves.Any(l => l.UnreadIn(0, node.DecisionLine)))
        {
            var changing = all.Where(v => ChangesState(node, v)).ToArray();
            if (changing.Length > 0
                && all.Except(changing).All(v => !node.Children[v].Leaves.Any(l => !l.LeftOut && l.UnreadIn(node.DecisionLine, l.Path.Lines.Count))))
                return new DrawChoice(DrawRule.Listed, changing);
        }
        return new DrawChoice(DrawRule.Odds, all);
    }

    /// <summary>
    /// Whether value <paramref name="value"/> of <paramref name="node"/> is harmless: every path under it returns (no
    /// ending, stop or abort, no value missing) and changes nothing that counts beyond what every path through the node
    /// changes (its prefix's writes, <see cref="Common"/>).
    /// </summary>
    public static bool IsHarmless(MarkNode node, int value)
    {
        if (!node.Children.TryGetValue(value, out var child) || child.Incomplete)
            return false;
        var common = Common(node);
        return child.Leaves.All(l => l.Repeats || l.Path.End == PreviewEnd.Returned && l.Changes.All(common.Contains));
    }

    /// <summary>
    /// Whether value <paramref name="value"/> of <paramref name="node"/> changes the state: no path under it ends the game,
    /// stops or aborts (the rule change never forces those, nor a value missing), and one changes something that counts
    /// beyond <see cref="Common"/>.
    /// </summary>
    public static bool ChangesState(MarkNode node, int value)
    {
        if (!node.Children.TryGetValue(value, out var child) || child.Incomplete)
            return false;
        if (child.Leaves.Any(l => !l.Plain))
            return false;
        var common = Common(node);
        return child.Leaves.Any(l => !l.Changes.All(common.Contains));
    }

    /// <summary>The changes every returning path through <paramref name="node"/> makes alike (the same part, the same value).</summary>
    public static IReadOnlySet<StateChange> Common(MarkNode node)
    {
        HashSet<StateChange>? common = null;
        foreach (var leaf in node.Leaves)
        {
            if (leaf.Path.End != PreviewEnd.Returned)
                continue;
            if (common is null)
                common = [.. leaf.Changes];
            else
                common.IntersectWith(leaf.Changes);
        }
        return common ?? [];
    }

    /// <summary>
    /// The mark of <paramref name="node"/>, counting the unread lines from index <paramref name="from"/> on and, unless
    /// <paramref name="ignoreChanges"/>, the changes.
    /// </summary>
    private SceneMark Eval(MarkNode node, int from, bool ignoreChanges, MarkRules rules)
    {
        var key = (node, from, ignoreChanges, rules);
        if (_marks.TryGetValue(key, out var known))
            return known;
        var mark = node.Leaf is { } leaf ? LeafMark(leaf, from, ignoreChanges) : DecisionMark(node, from, ignoreChanges, rules);
        _marks[key] = mark;
        return mark;
    }

    private static SceneMark LeafMark(MarkLeaf leaf, int from, bool ignoreChanges) => leaf.Path.End switch
    {
        PreviewEnd.Jumped or PreviewEnd.Quit => SceneMark.Read,
        PreviewEnd.Aborted or PreviewEnd.Repeated => SceneMark.Maybe,
        PreviewEnd.Stopped or PreviewEnd.Loaded => SceneMark.New,
        _ => leaf.UnreadIn(from, leaf.Path.Lines.Count) || !ignoreChanges && leaf.Changes.Count > 0 ? SceneMark.New : SceneMark.Read,
    };

    private SceneMark DecisionMark(MarkNode node, int from, bool ignoreChanges, MarkRules rules)
    {
        SceneMark Child(int value) =>
            node.Children.TryGetValue(value, out var child) ? Eval(child, from, ignoreChanges, rules) : SceneMark.Maybe;

        if (node.Decision!.Value.Kind == PreviewDecisionKind.Menu)
            return node.Outcomes.Select(Child).DefaultIfEmpty(SceneMark.Maybe).Max();
        var choice = Choose(node, rules);
        switch (choice.Rule)
        {
            case DrawRule.HarmlessUnread:
                return SceneMark.New;
            case DrawRule.Listed:
                var cap = rules.ListedDraws[node.Decision.Value.At!.Value];
                return Min(choice.Values.Select(Child).Min(), cap);
            default:
                var marks = choice.Values.Select(Child).ToArray();
                return marks.Length == 0 ? SceneMark.Maybe
                    : marks.All(m => m == SceneMark.New) ? SceneMark.New
                    : marks.All(m => m == SceneMark.Read) ? SceneMark.Read
                    : SceneMark.Maybe;
        }
    }

    private static SceneMark Max(SceneMark a, SceneMark b) => a > b ? a : b;

    private static SceneMark Min(SceneMark a, SceneMark b) => a < b ? a : b;
}
