using Kichikuou.Engine.Script;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §5, §8 step 6): the original's patches inside the scenes run from
/// the player's files (<see cref="ISceneHooks"/>, given to the game's <see cref="SceneRunner"/> by
/// <see cref="GameSession"/>). Each acts only while the context's <see cref="OriginalScriptContext.NewScenesFirst"/> is
/// on, read at the moment; off — as on every context built by hand, the differential tests' and the dry run's — the
/// scenes run as the original.
/// <list type="bullet">
/// <item><b>Kanami chooses in Code's event 4</b> (<see cref="KanamiTest"/>, <c>Gｺｰﾄﾞ:1457</c> <c>{Tmp0001 &gt; 1:</c>,
/// her four flags counted at <c>:1444-1456</c>): with two or more, our menu "Stop Code" / "Go with Code" answers the test
/// — Stop the fewer-than-two branch (<c>:1460</c>, she stops him, <c>F_Code_State</c> stays 4), Go the test's block
/// (<c>:1458</c> <c>@L_021f7</c>: <c>F_Code_State</c> 5, ending 3). It comes where the scene's own menus come: after her
/// doubts and "The clock struck one." with its key wait, the window closed (<c>:1441-1443</c>). With fewer than two the
/// test runs as the original.</item>
/// <item><b>Kentarou's hint</b> (<see cref="KentarouStep"/>, after <c>Gｹﾝﾀﾛｳ:610</c>, the training's
/// <c>F_Kentarou_State + 1</c>): when the state moved to 6 or 7, our narration — window 113 opened through
/// <c>WAO_SUB</c>'s ops as the scene's narration is, the line as <see cref="PortTextEvent"/>s broken where the scene's
/// lines break, a key wait, the window closed — after the scene's last line and its closed window (<c>:603-607</c>),
/// before the map comes back (<c>:612-613</c> <c>M 0</c>).</item>
/// </list>
/// </summary>
/// <remarks>Stateless: one <see cref="Instance"/> serves every game.</remarks>
public sealed class ScenePatches : ISceneHooks
{
    /// <summary>Code's event 4: the test of Kanami's count (<c>{Tmp0001 &gt; 1:</c>), which her choice answers.</summary>
    public static readonly ScriptRef KanamiTest = new("Gｺｰﾄﾞ.ADV", 1457);

    /// <summary>"Stop Code": the place the fewer-than-two branch goes on (<c>\L_026b5</c>, "...No...").</summary>
    public static readonly ScriptRef StopCodeAt = new("Gｺｰﾄﾞ.ADV", 1460);

    /// <summary>"Go with Code": the test's block (<c>@L_021f7</c>, the branch of ending 3).</summary>
    public static readonly ScriptRef GoWithCodeAt = new("Gｺｰﾄﾞ.ADV", 1458);

    /// <summary>Kentarou's training: the state's step (<c>!F_Kentarou_State : F_Kentarou_State + 1!</c>), after which his hint comes.</summary>
    public static readonly ScriptRef KentarouStep = new("Gｹﾝﾀﾛｳ.ADV", 610);

    /// <summary>Kanami's two items, the owner's words (docs/ui/talk-marks.md §5).</summary>
    public const string StopCode = "Stop Code", GoWithCode = "Go with Code";

    /// <summary>Kentarou's hint at the state 6 (5 → 6), the owner's words.</summary>
    public const string KentarouCatchingOn = "Kentarou seems to be catching on to the move.";

    /// <summary>Kentarou's hint at the state 7 (6 → 7), the owner's words.</summary>
    public const string KentarouNearlyCaughtOn = "Kentarou has nearly caught on to the move.";

    /// <summary>The hooks every game's runner gets (after the places: a static field is set in the order written).</summary>
    public static readonly ScenePatches Instance = new();

    private ScenePatches()
    {
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ScriptRef> Conditions { get; } = [KanamiTest];

    /// <inheritdoc />
    public IReadOnlyCollection<ScriptRef> Steps { get; } = [KentarouStep];

    /// <inheritdoc />
    public async Task<bool> ConditionAsync(ScriptContext script, ScriptRef at, bool holds)
    {
        if (at != KanamiTest || !holds || script is not OriginalScriptContext { NewScenesFirst: true } original)
            return holds;
        return await KanamiChoosesAsync(original);
    }

    /// <inheritdoc />
    public async Task AfterAsync(ScriptContext script, ScriptRef at)
    {
        if (at != KentarouStep || script is not OriginalScriptContext { NewScenesFirst: true } original)
            return;
        string[]? lines = original.State.F_Kentarou_State.Value switch
        {
            6 => KentarouLines(KentarouCatchingOn),
            7 => KentarouLines(KentarouNearlyCaughtOn),
            _ => null,
        };
        if (lines is not null)
            await NarrateAsync(original, lines);
    }

    /// <summary>
    /// Kanami's menu, the window cleared after it as the runner's <c>]</c> does; a cancel (a right click) asks again — a
    /// story choice must be made. True for "Go with Code".
    /// </summary>
    private static async Task<bool> KanamiChoosesAsync(OriginalScriptContext script)
    {
        SceneMenuItem[] items = [SceneMenuItem.OfPort(StopCodeAt, StopCode), SceneMenuItem.OfPort(GoWithCodeAt, GoWithCode)];
        while (true)
        {
            int choice = await script.SelectAsync(items);
            await script.ShowAsync(new MessageClearEvent());
            if (choice is 1 or 2)
                return choice == 2;
        }
    }

    /// <summary>
    /// A hint's two lines, broken at the last space within the scene's 30 cells as the scene breaks its lines with <c>R</c>
    /// ("Kentarou seems to be catching" / "on to the move.").
    /// </summary>
    private static string[] KentarouLines(string text)
    {
        int split = text.LastIndexOf(' ', Math.Min(text.Length - 1, 30));
        return [text[..split], text[(split + 1)..]];
    }

    /// <summary>The scene's narration in our words: window 113 (<c>\L_022ad</c>), the lines, the key wait (op 1, <c>RetVal</c> kept), the window closed (op 0).</summary>
    private static async Task NarrateAsync(OriginalScriptContext script, string[] lines)
    {
        await script.WaoAsync(113);
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                await script.ShowAsync(new LineBreakEvent());
            await script.ShowAsync(new PortTextEvent(lines[i], KentarouStep));
        }
        await script.WaoAsync(1);
        await script.WaoAsync(0);
    }
}
