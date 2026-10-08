using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

/// <summary>
/// Player decisions and engine queries the battle waits for. Every method models one input
/// command of the script and answers with the value the script would receive; the battle
/// describes the options the script makes available and rejects an answer outside them
/// (<see cref="InvalidOperationException"/>), so the UI cannot pick an unavailable option.
/// </summary>
public interface IBattleInput
{
    /// <summary>
    /// <c>IK 0</c> (message waits, the round header): waits for a key or click and returns the
    /// key code the engine stores in <c>RetVal</c> (0 while message skip is on).
    /// </summary>
    Task<Sys3Int> WaitForKeyAsync();

    /// <summary>
    /// <c>Y 3, n</c>: a timed pause of <paramref name="ticks"/> × 16 ms that a key or click cuts
    /// short; returns the key code the engine stores in <c>RetVal</c> (0 when the pause ran out).
    /// </summary>
    Task<Sys3Int> PauseAsync(Sys3Int ticks);

    /// <summary>
    /// <c>FIGT_SEL.ADV</c>: the command and target selection of a player unit. Returns the
    /// script's result pair (<c>RetVal</c>, <c>Tmp0001</c>); it must be allowed by
    /// <see cref="FigtSelMenu.Allows"/>.
    /// </summary>
    Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu);

    /// <summary>
    /// A script menu (<c>$label$ 'item' $ … ]</c>). Returns the <see cref="BattleMenuOption.Code"/>
    /// of the chosen item, or <c>null</c> when the player cancels the menu (right click), in
    /// which case the script continues after the menu.
    /// </summary>
    Task<Sys3Int?> ChooseAsync(BattleMenu menu);

    /// <summary>
    /// <c>SR 0, var</c>: engine query of the music track that is playing, stored by the
    /// wrappers before a jingle and replayed afterwards with <c>SS var - 1</c>.
    /// </summary>
    Task<Sys3Int> GetPlayingTrackAsync();
}

/// <summary>The result pair of <c>FIGT_SEL.ADV</c>.</summary>
/// <param name="Command"><c>RetVal</c>: the command code (see <see cref="FigtSelCommand"/>).</param>
/// <param name="Argument">
/// <c>Tmp0001</c>: for an attack the clicked enemy button (4-7 = enemy slot 1-4); for the slot
/// popup and unit info the player slot (1-4); for enemy info the enemy slot (1-4); for the other
/// command-popup items the button under the cursor at the right click (4-7) or 99 (none).
/// </param>
public readonly record struct FigtSelChoice(Sys3Int Command, Sys3Int Argument);

/// <summary>One command the player may return from <c>FIGT_SEL.ADV</c>, with its allowed arguments.</summary>
/// <param name="Command">The <c>RetVal</c> code.</param>
/// <param name="Arguments">The <c>Tmp0001</c> values allowed with it.</param>
public sealed record FigtSelOption(Sys3Int Command, IReadOnlyList<Sys3Int> Arguments);

/// <summary>
/// What <c>FIGT_SEL.ADV</c> offers to the acting unit: the attack modes with their valid
/// targets, the command popup and the own-slot popup (<c>FIGT_SEL.ADV:161-697</c>, 01 §8.2).
/// </summary>
/// <param name="ActingSlot"><c>Bt_PlayerActSlot</c> (1-4).</param>
/// <param name="AttackType"><c>Bt_PlayerAtkType</c>: 0 melee, 1 bow, 2 magic.</param>
/// <param name="InitialMode"><c>Tmp0019</c> at entry: the targeting mode shown first (attack type + 1).</param>
/// <param name="PlayerSlotsUsed"><c>Bt_PlayerSlotsUsed</c>: own panels that can be clicked.</param>
/// <param name="EnemySlotsUsed"><c>Bt_EnemySlotsUsed</c>: enemy panels that can be hovered.</param>
/// <param name="Options">Every allowed command, in the order the script lists them.</param>
public sealed record FigtSelMenu(
    Sys3Int ActingSlot,
    Sys3Int AttackType,
    Sys3Int InitialMode,
    Sys3Int PlayerSlotsUsed,
    Sys3Int EnemySlotsUsed,
    IReadOnlyList<FigtSelOption> Options)
{
    /// <summary>Whether the script could return this pair.</summary>
    public bool Allows(FigtSelChoice choice) =>
        Options.Any(o => o.Command == choice.Command && o.Arguments.Contains(choice.Argument));

    /// <summary>The option for a command code, or <c>null</c> when it is not available.</summary>
    public FigtSelOption? Find(Sys3Int command) => Options.FirstOrDefault(o => o.Command == command);
}

/// <summary>Command codes returned by <c>FIGT_SEL.ADV</c> in <c>RetVal</c> (01 §8.3).</summary>
public static class FigtSelCommand
{
    /// <summary>Direct attack (targeting mode 1).</summary>
    public const ushort Direct = 1;
    /// <summary>Bow attack (mode 2).</summary>
    public const ushort Bow = 2;
    /// <summary>Magic attack (mode 3).</summary>
    public const ushort Magic = 3;
    /// <summary>Defend.</summary>
    public const ushort Defend = 4;
    /// <summary>Wait (gain one focus charge).</summary>
    public const ushort Wait = 5;
    /// <summary>Retreat.</summary>
    public const ushort Retreat = 6;
    /// <summary>Debug "Repel" (instant victory).</summary>
    public const ushort Repel = 7;
    /// <summary>Standing order "keep attacking" for the acting unit.</summary>
    public const ushort AutoAttack = 8;
    /// <summary>Standing order "keep defending" for the acting unit.</summary>
    public const ushort AutoDefend = 9;
    /// <summary>Debug Cheater Beam (mode 10).</summary>
    public const ushort CheaterBeam = 10;
    /// <summary>Rance Attack (mode 11).</summary>
    public const ushort RanceAttack = 11;
    /// <summary>By la Way (mode 12).</summary>
    public const ushort ByLaWay = 12;
    /// <summary>White Destruction Beam (mode 13).</summary>
    public const ushort WhiteBeam = 13;
    /// <summary>High Speed (mode 14).</summary>
    public const ushort HighSpeed = 14;
    /// <summary>Vanish Bomb (mode 15).</summary>
    public const ushort VanishBomb = 15;
    /// <summary>C. War Dance (mode 16).</summary>
    public const ushort WarDance = 16;
    /// <summary>Black Destruction Beam (mode 17).</summary>
    public const ushort BlackBeam = 17;
    /// <summary>Whirlwind Shot (mode 18).</summary>
    public const ushort WhirlwindShot = 18;
    /// <summary>Chuhai (mode 19).</summary>
    public const ushort Chuhai = 19;
    /// <summary>T2 Soaring Wave (mode 20).</summary>
    public const ushort SoaringWave = 20;
    /// <summary>EPO Light (mode 21).</summary>
    public const ushort EpoLight = 21;
    /// <summary>Summon the capsule kaiju Amoeba (item 27).</summary>
    public const ushort Amoeba = 30;
    /// <summary>Summon the capsule kaiju Jagi Jagi (item 26).</summary>
    public const ushort JagiJagi = 31;
    /// <summary>Use the Fog Card (item 28).</summary>
    public const ushort FogCard = 32;
    /// <summary>Slot popup: order "manual" for the slot in the argument.</summary>
    public const ushort OrderManual = 50;
    /// <summary>Slot popup: order "auto attack" for the slot in the argument.</summary>
    public const ushort OrderAttack = 51;
    /// <summary>Slot popup: order "auto defend" for the slot in the argument.</summary>
    public const ushort OrderDefend = 52;
    /// <summary>Own unit info for the slot in the argument.</summary>
    public const ushort UnitInfo = 53;
    /// <summary>Enemy info for the enemy slot in the argument.</summary>
    public const ushort EnemyInfo = 54;
}

/// <summary>The script menus of the battle.</summary>
public enum BattleMenuKind
{
    /// <summary><c>FIGT_B.ADV:51-64</c>: codes 1 Intercept, 2 Ninja Scout, 3 Abandon City.</summary>
    EnemyAttackResponse,

    /// <summary><c>FIGT_B.ADV:345-380</c>: codes 1-5 = the ninja (<c>Bt_ScoutNinja</c>: Kanami, Freya, Kaoru, Wichita, Saitou), 0 None.</summary>
    NinjaScout,

    /// <summary><c>FIGT.ADV:571-580</c>: codes 1 Retreat, 2 Cease Retreat.</summary>
    RetreatConfirm,
}

/// <summary>One item of a script menu.</summary>
/// <param name="Code">The value returned for this item (see the menu's documentation).</param>
/// <param name="Label">Where the item's text is in the scenario.</param>
public sealed record BattleMenuOption(Sys3Int Code, ScriptRef Label);

/// <summary>A script menu with the items the script lists, in order.</summary>
/// <param name="Kind">Which menu this is.</param>
/// <param name="Options">The listed items.</param>
public sealed record BattleMenu(BattleMenuKind Kind, IReadOnlyList<BattleMenuOption> Options)
{
    /// <summary>Whether an answer is valid (<c>null</c> = cancel is always allowed).</summary>
    public bool Allows(Sys3Int? code) => code is null || Options.Any(o => o.Code == code.Value);
}
