using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// Semantic screen events (G0, owner's decision 9): what is open, for a UI that draws its screens
// by hand from the events and GameState instead of emulating the engine's drawing commands. They
// have no statement in the script, draw nothing and change no state; the differential
// comparisons of a port against its file run by SceneRunner skip them.

/// <summary>A semantic screen event: which screen opened or closed. Raised next to the script's own events.</summary>
public abstract record ScreenEvent : ScriptEvent;

/// <summary>
/// <c>COMMAND</c>'s map loop (<c>COMMAND.ADV:49-172</c>) waits for the player
/// (<paramref name="Waiting"/>), or has left the wait for a province, a dungeon or the command
/// menu. While it waits, the pointer reads it makes are the map's: clicks, right clicks, drags.
/// </summary>
public sealed record MapScreenEvent(bool Waiting) : ScreenEvent;

/// <summary>The picker screens of the original (<c>Sﾌﾞﾀｲ</c>, <c>Sﾌﾞｶ</c>, <c>Sﾊｰﾚﾑ</c>).</summary>
public enum PickerKind
{
    /// <summary><c>Sﾌﾞﾀｲ.ADV</c>: the party for a battle or a dungeon search.</summary>
    Deployment,

    /// <summary><c>Sﾌﾞｶ.ADV</c>: a subordinate for the Subordinates command.</summary>
    Subordinates,

    /// <summary><c>Sﾊｰﾚﾑ.ADV</c>: a girl for the Harem command.</summary>
    Harem,
}

/// <summary>
/// A picker screen opened (<paramref name="Open"/>; raised before its first drawing command) or
/// closed (raised after its last), with the mode it was called in (<c>Tmp0022</c> of
/// <c>Sﾌﾞﾀｲ</c>, <c>CallArg</c> of <c>Sﾌﾞｶ</c>; 0 for <c>Sﾊｰﾚﾑ</c>). The screen's state is in
/// GameState: the grid (unit status, <c>Buf_Val</c>), the party (<c>Bt_PlayerParty1-4</c>), the
/// entry under the pointer (<c>Tmp0033</c>) and the bar button under it (<c>Tmp0029</c>).
/// </summary>
public sealed record PickerScreenEvent(PickerKind Kind, Sys3Int Mode, bool Open) : ScreenEvent;

/// <summary>
/// The save screen's lists and menu (<c>ﾛｰﾄﾞｾｰﾌﾞ</c> modes 0, 1, 2) opened or closed; a load
/// leaves it by <see cref="GameLoadedException"/> and closes it with "Load Complete".
/// </summary>
public sealed record SaveScreenEvent(Sys3Int Mode, bool Open) : ScreenEvent;

/// <summary>
/// A battle file (<c>FIGT_A</c>, <c>FIGT_B</c>, <c>FIGT</c>, <c>死亡捕獲</c>) started or returned; in
/// between, the battle's own events draw the screen (they may nest: <c>死亡捕獲</c> runs inside a battle).
/// </summary>
public sealed record BattleScreenEvent(string File, bool Open) : ScreenEvent;
