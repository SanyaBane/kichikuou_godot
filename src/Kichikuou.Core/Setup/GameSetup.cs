using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Setup;

/// <summary>
/// Scenario code the port reads from the player's decompiled scenario at run time instead of
/// committing it: the record data files and the literals of hand-ported files.
/// </summary>
public interface IScenarioSource
{
    /// <summary>
    /// The statements of a straight-line data file (<c>SET_MAP.ADV</c>, <c>SET_CHR.ADV</c>,
    /// <c>SET_ITEM.ADV</c>), up to and including its return.
    /// </summary>
    /// <exception cref="ScenarioFormatException">The file holds a statement outside the supported forms.</exception>
    IReadOnlyList<ScriptStatement> ReadFile(string file);

    /// <summary>The single statement at <paramref name="at"/> (e.g. an <c>MS n, "text"</c> of a hand-ported file).</summary>
    ScriptStatement ReadStatement(ScriptRef at);

    /// <summary>
    /// The straight-line body of a label from <paramref name="at"/> up to and including its return
    /// (<c>\0:</c>) — record data such as <c>Cﾒｲｷｭｳ</c>'s commander setters: assignments, <c>MS</c>,
    /// <c>MM</c> and <c>R</c> (a <see cref="LineBreakStatement"/>); <c>ZH n:</c> is left out.
    /// </summary>
    /// <exception cref="ScenarioFormatException">The body holds another statement, or no return.</exception>
    IReadOnlyList<ScriptStatement> ReadBlock(ScriptRef at) =>
        throw new InvalidOperationException($"{at}: this scenario source reads no blocks.");
}

/// <summary>
/// The start of a game: the boot (<c>ｽﾀｰﾄ.ADV</c>'s state lines and <c>SET.ADV</c>, which builds
/// the initial world once per process) and the new game of the title menu (<c>ｽﾀMENU.ADV</c>).
/// See <c>docs/strategy/survey-data.md</c>.
/// </summary>
public sealed partial class GameSetup(ScriptContext script, IScenarioSource scenario)
{
    private const string SetFile = "SET.ADV";

    /// <summary>The shared services the setup runs on.</summary>
    public ScriptContext Script { get; } = script;

    private IScenarioSource Scenario { get; } = scenario;

    private GameState State => Script.State;

    /// <summary>
    /// The boot: <c>ｽﾀｰﾄ.ADV</c>'s state effects, then <c>SET.ADV</c>, on a fresh state.
    /// </summary>
    /// <remarks>
    /// <c>ｽﾀｰﾄ.ADV:1-846</c> zero <c>VAR0000-0804</c> (a fresh <see cref="GameState"/> is zero);
    /// <c>:851</c> runs <c>XSPEC.ADV</c>, a machine benchmark whose results (<c>SpecBaseline</c>,
    /// <c>SpecGraphics</c>, <c>SpecCpu</c> and scratch) only choose <c>Ui_ScrollWait</c> and the
    /// opening's speed (QUIRK: DAT-04) — not run, so they stay 0; the rest of <c>:847-868</c> sets
    /// up the screen (the presenter starts so), but for <c>:858</c>, <c>Y 3, 10000</c>: from the boot
    /// on, a key does not cut a <c>Y 3</c> pause short until a scene says so (the logo does, and
    /// leaves them so again at its end, <c>ﾌﾟﾛﾛｰｸﾞ.ADV:1, 151</c>). The script then jumps to the
    /// title (<c>:872</c>), which the caller runs.
    /// </remarks>
    public async Task BootAsync()
    {
        await Script.PauseAsync((Sys3Int)10000);   // ｽﾀｰﾄ.ADV:858
        State.VAR0041 = 1280;                       // :869
        State.VAR0042 = 200;                        // :870
        await SetAsync();                           // :871
    }

    /// <summary>
    /// "Start" on the title menu with <c>DebugMode = 0</c> without the title (development:
    /// <c>--newgame</c>, <c>--auto</c>, the tests): the state the title menu leaves on that path
    /// (below), <c>ｽﾀMENU.ADV:244-247</c> (<c>L_005ef</c>) and the new game <c>L_00670</c>
    /// (<c>:279-290</c>). The script then jumps to <c>MAIN.ADV</c> (<c>:291</c>); the caller runs
    /// the week loop next.
    /// </summary>
    /// <remarks>
    /// The title itself runs from the player's files (owner's decision 19,
    /// <c>GameSession.TitleAsync</c>), its debug starts From 2-4 too; this is its state lines on the
    /// way to "Start" on a fresh boot, where <c>DebugMode</c> is 0 (the debug menu needs Alice's
    /// secret first, QUIRK: EVT-09): checked against the <c>enter</c> dumps of the owner's
    /// <c>Iﾌﾟﾛﾛｰｸﾞ</c> captures (<c>NewGameStateTests</c>). What the port cannot know is left: the
    /// persistent page 4 (from the player's slots), the benchmark's results and the title's scratch:
    /// the display's depth (<c>Tmp0003</c>), where the logo was clicked away (<c>Tmp0020</c>) and the
    /// target of a map scroll watched on the title first (<c>Tmp0039</c> [I: Alice's opening]).
    /// </remarks>
    public async Task NewGameAsync()
    {
        Script.SetString(1, "");                    // :8 MS 1, "" (L_00035, the title screen)
        State.Glob_FateCur = 79;                    // :55 the clear counter's slot
        State.Glob_CgCur = 240;                     // :84
        State.Tmp0001 = 6;                          // :103-116 (L_0028d, L_00e91 :695-698) the item under the pointer: Start
        State.Tmp0002 = 6;                          //   :116 the item highlighted
        State.Glob_MenuCur = 6;                     //   :112
        State.RetVal = 0;                           // :131-134 the release wait L_0036f
        State.MapTargetX = 1000;                    // :136-139 the camera on the middle of the map
        State.MapTargetY = 400;
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        State.CurrentBgm = 4;                       // :245
        // :246-248 DebugMode 0: on to L_00670
        // :280-282 PF1 1, EC 0, PF0 1: fade out, clear, fade in (display only)
        await Script.WaoMapCallAsync(3);            // :283-284 redraw the map and the markers
        await Script.MapCallAsync(0);               // :285-286
        State.Week = 8;                             // :287
        State.StoryStage = 0;                       // :288
        State.Gold = 50;                            // :289
        await Script.CallFileAsync("Iﾌﾟﾛﾛｰｸﾞ.ADV"); // :290 the prologue scene (no Ev_Entry)
    }
}
