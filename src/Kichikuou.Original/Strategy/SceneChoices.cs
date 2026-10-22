using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// One draw of a real talk or visit while "New scenes first" acts (<see cref="SceneChoices"/>): its place and count,
/// the value the generator drew, the value the script got and the rule that gave it (<see cref="DrawRule.Odds"/>: the
/// drawn value kept).
/// </summary>
public readonly record struct SceneDraw(ScriptRef? At, int Count, int Drawn, int Value, DrawRule Rule)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"{Value} of {Count}{(At is { } at ? $" at {at}" : "")} ({Rule}{(Drawn != Value ? $", drawn {Drawn}" : "")})";
}

/// <summary>
/// Ours (owner's decision 30, the third rule change on purpose; docs/ui/talk-marks.md §5, §8 step 5): "New scenes
/// first" during a real talk (<c>Cﾌﾞｶ</c>) or visit (<c>Cﾊｰﾚﾑ</c>). The command marks the talk's or the visit's start
/// (<see cref="Begin"/>, a copy of the state taken there) and its end (<see cref="End"/>); in between every decision
/// the run makes is noted — each draw of 2 or more as the value the script got, each menu's choice — and at each draw,
/// after the game's generator has drawn as the original does, the dry run runs the talk or the visit from the copy with
/// those decisions forced (<see cref="SceneMarks.Explore"/>) and the tree's choice at this draw
/// (<see cref="MarkTree.Choose"/>, "New scenes first" on) decides the value: (a) a harmless unread variant or (b) at a
/// listed draw whose usual scene is read, the outcome that changes the state, among the values it gives; (c) else the
/// value drawn. Inactive unless the context's <see cref="OriginalScriptContext.NewScenesFirst"/> is on and it has
/// <see cref="OriginalScriptContext.Marks"/>; so the differential tests, which build their contexts by hand, run the
/// files as the original.
/// </summary>
/// <remarks>
/// <para><b>The generator is never touched</b>: the game's draw is made first, once, as the original makes it; the
/// replacement only changes <c>RetVal</c> (<see cref="ScriptContext.Rnd"/>), so every later draw — battles, events — is
/// the same. Among the values the rule gives, the drawn one is kept when it is one of them, else the
/// ((drawn − 1) mod count)-th: chance among them without a draw of its own, the same for the same seed.</para>
/// <para><b>The copy and the dry run line up</b>: the talk starts where Talk is chosen (<c>Cﾌﾞｶ</c>'s
/// <c>L_014b2</c>, or the prologue's pick, <c>:716</c>), the visit where the girl is picked (after <c>Pick</c>), so the
/// dry run's start (<see cref="SubordinatesCommand.PreviewTalkAsync"/>, <see cref="HaremCommand.PreviewVisitAsync"/>)
/// repeats on the copy only the command's writes the real run has just made (the picker's answer, the names, the pick's
/// bookkeeping) and then runs the same code. The decisions are noted as the dry run makes them: the draws of 2 or more
/// with their place (<see cref="ScriptContext.Rnd"/>), the menus as their item or 0 (<see cref="ScriptContext.SelectAsync"/>);
/// <c>FIGTATTK</c>'s draws (the exchange's animation, from <c>FIGT表示</c>) are none, as the dry run gives them 1
/// (<see cref="MutedAsync"/>). Suzume's and Wendy's draw (<c>Cﾊｰﾚﾑ:6</c>) comes before the visit's start and keeps its odds.</para>
/// <para><b>Never in the game's way</b>: where the dry run cannot follow the real run — the forced decisions no longer
/// fit (after a battle, which the dry run stops at), the draw it reaches is not the real one (a key answered otherwise
/// than its click, a scene that reads the clock), anything it throws — the rest of that talk or visit keeps the values
/// drawn, and the reason is kept (<see cref="LastFallback"/>, <see cref="Fallbacks"/>). A draw the dry run ends as
/// repeating an earlier one (a redraw) keeps its value and the rule goes on.</para>
/// <para>Cost: one dry run per draw of a talk or visit (docs/ui/talk-marks.md §9: about 10 ms each once the scenes are
/// read). Not thread-safe; it runs on the game's thread, inside the draw.</para>
/// </remarks>
public sealed class SceneChoices(OriginalScriptContext script)
{
    private readonly List<PreviewDecision> _decisions = [];
    private readonly List<SceneDraw> _draws = [];
    private GameState? _start;
    private GameState? _copy;
    private int _muted;

    /// <summary>Whether a talk or a visit is under way with the rule acting (between <see cref="Begin"/> and <see cref="End"/>, until a fallback).</summary>
    public bool Active => _start is not null;

    /// <summary>The talk or the visit begun last (kept after its end, for a look).</summary>
    public MarkTarget? Target { get; private set; }

    /// <summary>The decisions noted since the last <see cref="Begin"/> while the rule acted, as the dry run takes them.</summary>
    public IReadOnlyList<PreviewDecision> Decisions => _decisions;

    /// <summary>The draws of the talk or the visit begun last while the rule acted, with what each gave.</summary>
    public IReadOnlyList<SceneDraw> Draws => _draws;

    /// <summary>How many talks and visits the rule stopped acting in (a fallback), since the context was made.</summary>
    public int Fallbacks { get; private set; }

    /// <summary>Why it stopped last (the talk or visit and the reason), null before any fallback.</summary>
    public string? LastFallback { get; private set; }

    /// <summary>
    /// A talk (<see cref="MarkTargetKind.Talk"/>) or a visit starts in the real game: the state copied as it is now, the
    /// decisions counted from here. Nothing unless <see cref="OriginalScriptContext.NewScenesFirst"/> is on and the
    /// context has its <see cref="OriginalScriptContext.Marks"/>.
    /// </summary>
    public void Begin(MarkTarget target)
    {
        End();
        _decisions.Clear();
        _draws.Clear();
        Target = target;
        if (!script.NewScenesFirst || script.Marks is null)
            return;
        _copy ??= new GameState();
        script.State.CopyTo(_copy);
        _start = _copy;
    }

    /// <summary>The talk or the visit is over (also when it leaves by a jump or a load): the rule stops acting.</summary>
    public void End()
    {
        _start = null;
        _muted = 0;
    }

    /// <summary>
    /// Runs <paramref name="presentation"/> with its draws noted as no decision: <c>FIGTATTK</c>, whose draws the dry run
    /// gives 1 without a decision (<see cref="SceneMarks"/>).
    /// </summary>
    public async Task MutedAsync(Func<Task> presentation)
    {
        _muted++;
        try
        {
            await presentation();
        }
        finally
        {
            if (_muted > 0)
                _muted--;
        }
    }

    /// <summary>A draw made (<see cref="ScriptContext.Rnd"/>): the value the script gets.</summary>
    internal Sys3Int Draw(Sys3Int n, ScriptRef? at, Sys3Int drawn)
    {
        if (_start is null || n.Value <= 1 || _muted > 0)
            return drawn;
        var (value, rule) = Choose(_start, n.Value, at, drawn.Value);
        if (_start is null)
            return drawn;                                           // the fallback: this draw and the rest keep their values
        _decisions.Add(PreviewDecision.Draw(n.Value, value, at));
        _draws.Add(new SceneDraw(at, n.Value, drawn.Value, value, rule));
        return (Sys3Int)value;
    }

    /// <summary>A menu answered (<see cref="ScriptContext.SelectAsync"/>): noted as the dry run's decision.</summary>
    internal void Selected(IReadOnlyList<SceneMenuItem> items, int choice)
    {
        if (_start is null)
            return;
        _decisions.Add(PreviewDecision.Menu(items.Count, choice, items.Count > 0 ? items[0].At : null));
    }

    /// <summary>The value of a draw of <paramref name="n"/> at <paramref name="at"/> that drew <paramref name="drawn"/>, by the tree's choice.</summary>
    private (int Value, DrawRule Rule) Choose(GameState start, int n, ScriptRef? at, int drawn)
    {
        try
        {
            var tree = script.Marks!.Explore(start, Target!.Value, _decisions);
            var node = tree.NodeAt(_decisions);
            if (node?.Leaf is { Repeats: true })
                return (drawn, DrawRule.Odds);                      // a redraw the dry run cuts: from here as from the first time
            if (node?.Decision is not { Kind: PreviewDecisionKind.Draw } decision || decision.Count != n || decision.At != at)
            {
                Fallback($"the dry run meets {(node?.Decision?.ToString() ?? node?.Leaf?.Path.End.ToString() ?? "no path")}, "
                    + $"the game a draw of {n}{(at is { } a ? $" at {a}" : "")}");
                return (drawn, DrawRule.Odds);
            }
            var choice = tree.Choose(node, SceneMarks.Rules(true));
            if (choice.Rule == DrawRule.Odds || choice.Values.Count == 0 || choice.Values.Contains(drawn))
                return (drawn, choice.Rule);
            return (choice.Values[(drawn - 1) % choice.Values.Count], choice.Rule);
        }
        catch (Exception e)
        {
            Fallback($"{e.GetType().Name}: {e.Message}");
            return (drawn, DrawRule.Odds);
        }
    }

    /// <summary>The rule stops acting for the rest of this talk or visit.</summary>
    private void Fallback(string reason)
    {
        Fallbacks++;
        LastFallback = $"{Target}: {reason}";
        End();
    }
}
