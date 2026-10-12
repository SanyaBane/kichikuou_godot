using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Shared helpers of the flow port: the four-copy slot variables, string commands and the
// presentation / sub-file calls that the flow files make.
public sealed partial class Battle
{
    private const string FigtFile = "FIGT.ADV";
    private const string FigtAFile = "FIGT_A.ADV";
    private const string FigtBFile = "FIGT_B.ADV";
    private const string StrategyFile = "FIGT作.ADV";

    /// <summary><c>Bt_PlayerSlot1..4</c> (01 §0.2 <c>PSlot[i]</c>).</summary>
    private ref Sys3Int PSlot(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_PlayerSlot1;
            case 2: return ref State.Bt_PlayerSlot2;
            case 3: return ref State.Bt_PlayerSlot3;
            case 4: return ref State.Bt_PlayerSlot4;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_EnemySlot1..4</c> (<c>ESlot[i]</c>).</summary>
    private ref Sys3Int ESlot(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_EnemySlot1;
            case 2: return ref State.Bt_EnemySlot2;
            case 3: return ref State.Bt_EnemySlot3;
            case 4: return ref State.Bt_EnemySlot4;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_PlayerParty1..4</c> (<c>PParty[i]</c>).</summary>
    private ref Sys3Int PParty(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_PlayerParty1;
            case 2: return ref State.Bt_PlayerParty2;
            case 3: return ref State.Bt_PlayerParty3;
            case 4: return ref State.Bt_PlayerParty4;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_EnemyParty1..4</c> (<c>EParty[i]</c>).</summary>
    private ref Sys3Int EParty(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_EnemyParty1;
            case 2: return ref State.Bt_EnemyParty2;
            case 3: return ref State.Bt_EnemyParty3;
            case 4: return ref State.Bt_EnemyParty4;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_PSlot1..4State</c> (<c>PState[i]</c>).</summary>
    private ref Sys3Int PState(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_PSlot1State;
            case 2: return ref State.Bt_PSlot2State;
            case 3: return ref State.Bt_PSlot3State;
            case 4: return ref State.Bt_PSlot4State;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_ESlot1..4State</c> (<c>EState[i]</c>).</summary>
    private ref Sys3Int EState(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_ESlot1State;
            case 2: return ref State.Bt_ESlot2State;
            case 3: return ref State.Bt_ESlot3State;
            case 4: return ref State.Bt_ESlot4State;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_PSlot1..4Focus</c> (<c>PFocus[i]</c>).</summary>
    private ref Sys3Int PFocus(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_PSlot1Focus;
            case 2: return ref State.Bt_PSlot2Focus;
            case 3: return ref State.Bt_PSlot3Focus;
            case 4: return ref State.Bt_PSlot4Focus;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary><c>Bt_PlayerOrder1..4</c> (<c>POrder[i]</c>).</summary>
    private ref Sys3Int POrder(int i)
    {
        switch (i)
        {
            case 1: return ref State.Bt_PlayerOrder1;
            case 2: return ref State.Bt_PlayerOrder2;
            case 3: return ref State.Bt_PlayerOrder3;
            case 4: return ref State.Bt_PlayerOrder4;
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary>The script's <c>{v = 1: …} … {v = 4: …}</c> chains: the slot number when v is 1..4.</summary>
    private static bool IsSlot(Sys3Int v, out int slot)
    {
        slot = v.Value;
        return slot is >= 1 and <= 4;
    }

    /// <summary><c>x ∈ PParty</c>: <c>(Bt_PlayerParty1 = x) + … + (Bt_PlayerParty4 = x)</c>.</summary>
    private bool InPlayerParty(Sys3Int id) =>
        State.Bt_PlayerParty1 == id || State.Bt_PlayerParty2 == id ||
        State.Bt_PlayerParty3 == id || State.Bt_PlayerParty4 == id;

    /// <summary><c>x ∈ EParty</c>.</summary>
    private bool InEnemyParty(Sys3Int id) =>
        State.Bt_EnemyParty1 == id || State.Bt_EnemyParty2 == id ||
        State.Bt_EnemyParty3 == id || State.Bt_EnemyParty4 == id;

    /// <summary><c>MS n, "text"</c>: sets string variable n.</summary>
    private void FlowSetString(int n, string text) => State.Strings[n] = text;

    /// <summary>
    /// <c>MM dst, src</c>: copies string variable src into dst. Indices outside the engine's
    /// string table are ignored (the script never produces them for valid records).
    /// </summary>
    private void FlowCopyString(Sys3Int dst, Sys3Int src)
    {
        var strings = State.Strings;
        if (dst.Value < strings.Length && src.Value < strings.Length)
            strings[dst.Value] = strings[src.Value];
    }

    /// <summary>The first string variable of the record selected by <c>Unit_Cur</c> (its name).</summary>
    private Sys3Int UnitNameString(Sys3Int unit) => State.Unit_StrBase + (unit - 1) * 3;

    /// <summary>The first string variable of the record selected by <c>Cmdr_Cur</c> (its name).</summary>
    private Sys3Int CmdrNameString(Sys3Int cmdr) => State.Cmdr_StrBase + (cmdr - 1) * 3;

    // The shared services in the battle's own words (OriginalScriptContext holds the ports).

    /// <summary><c>!Ui_WaoOp : op! %#WAO_SUB.ADV:</c> (<see cref="OriginalScriptContext.WaoAsync"/>).</summary>
    private Task WaoAsync(Sys3Int op) => Script.WaoAsync(op);

    /// <summary>A text run of <paramref name="file"/> starting at <paramref name="line"/>.</summary>
    private Task FlowTextAsync(string file, int line) => Script.TextAsync(file, line);

    /// <summary>A text run that prints numbers.</summary>
    private Task FlowValueTextAsync(string file, int line, params Sys3Int[] values) =>
        View.ShowAsync(new ValueMessageEvent(new ScriptRef(file, line), values));

    /// <summary>WAO op 11, the key wait that leaves the key in <c>RetVal</c> (<see cref="OriginalScriptContext.WaoWaitKeyAsync"/>).</summary>
    private Task FlowWaitKeyAsync() => Script.WaoWaitKeyAsync();

    /// <summary>WAO op 1, the key wait that keeps <c>RetVal</c> (<see cref="OriginalScriptContext.WaoWaitKeyKeepRetValAsync"/>).</summary>
    private Task FlowWaitKeyKeepRetValAsync() => Script.WaoWaitKeyKeepRetValAsync();

    /// <summary>
    /// The FIGT message box: <c>\L_0509c</c> (open, op 143), the text, <c>\L_05119</c> (wait,
    /// op 11), <c>\L_0510c</c> (close, op 10).
    /// </summary>
    private async Task FigtBoxAsync(int line)
    {
        await WaoAsync(143);
        await FlowTextAsync(FigtFile, line);
        await FlowWaitKeyAsync();
        await WaoAsync(10);
    }

    /// <summary><c>%#PCMPLAY.ADV</c> (<see cref="OriginalScriptContext.SoundAsync"/>).</summary>
    private Task FlowSoundAsync(Sys3Int mode) => Script.SoundAsync(mode);

    /// <summary><c>!CallArg : mode! %#M.ADV</c> (<see cref="OriginalScriptContext.MapCallAsync"/>).</summary>
    private Task FlowMapAsync(Sys3Int mode) => Script.MapCallAsync(mode);

    /// <summary><c>Y 3, n</c> (<see cref="ScriptContext.PauseAsync"/>).</summary>
    private Task PauseAsync(Sys3Int ticks) => Script.PauseAsync(ticks);

    /// <summary>
    /// <c>{BgmEnabled = 1: SR0, Tmp0001: SS 0:}</c> — remembers the playing track and stops the music.
    /// </summary>
    private async Task FlowSuspendMusicAsync()
    {
        if (State.BgmEnabled == 1)
        {
            var answer = await Script.GetPlayingTrackAsync();
            State.Tmp0001 = answer;
            await Script.MusicAsync(0);
        }
    }

    /// <summary><c>{BgmEnabled = 1: SS Tmp0001 - 1:}</c> — resumes the remembered track.</summary>
    private Task FlowResumeMusicAsync() =>
        State.BgmEnabled == 1 ? Script.MusicAsync(State.Tmp0001 - 1) : Task.CompletedTask;

    /// <summary><c>Ev_Entry := entry; call file</c> (<see cref="ScriptContext.EventAsync"/>).</summary>
    private Task EventAsync(string file, Sys3Int entry) => Script.EventAsync(file, entry);

    /// <summary><c>%#file</c> without an entry number (<see cref="ScriptContext.CallFileAsync"/>).</summary>
    private Task CallFileAsync(string file) => Script.CallFileAsync(file);

    /// <summary>
    /// <c>!EndingId : id! &amp;#OVER.ADV:</c> — the script jumps to the game-over file and never
    /// returns; the battle is abandoned by throwing <see cref="ScenarioJumpException"/>.
    /// </summary>
    private void JumpToGameOver(Sys3Int endingId)
    {
        State.EndingId = endingId;
        ScriptContext.JumpTo("OVER.ADV");
    }
}
