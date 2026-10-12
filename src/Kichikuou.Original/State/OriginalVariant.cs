using Kichikuou.Engine.Script;

namespace Kichikuou.Original.State;

/// <summary>
/// The original game (Kichikuou Rance, 1996) as a variant of the engine (docs/variants.md): the facts
/// the runtime takes from it. <see cref="GameState"/> carries <see cref="Profile"/>.
/// </summary>
public static class OriginalVariant
{
    /// <summary>
    /// The files run from the decompile: the scenes (<c>G*</c>, <c>I*</c>), the opening, the battle
    /// inserts and the game frame (owner's decision 19: <c>OVER</c>, the ending; <c>ｽﾀMENU</c>, the
    /// title, with its logo <c>ﾌﾟﾛﾛｰｸﾞ</c>, its Fortune Check <c>HAPPY_C</c>, its gallery <c>CG</c> and
    /// its music room <c>MMODE</c>).
    /// </summary>
    public static bool IsSceneFile(string file) =>
        file.Length > 0 && file[0] is 'G' or 'I'
        || file is "ｵｰﾌﾟﾆﾝｸﾞ.ADV" or "FIGT表示.ADV" or "OVER.ADV" or "ｽﾀMENU.ADV" or "ﾌﾟﾛﾛｰｸﾞ.ADV" or "HAPPY_C.ADV"
            or "CG.ADV" or "MMODE.ADV";

    /// <summary>
    /// Ours (the owner's requests 2026-10-11): the scenes the message skip skips whole instead of
    /// fast-forwarding — the opening (<c>ｵｰﾌﾟﾆﾝｸﾞ</c>) between the prologue and the rebellion, up to
    /// the rebellion's first scene (<c>Iﾊﾝﾗﾝ.ADV:63-77</c>, Northern Leazas City on the map), from which
    /// the skip goes on as usual. While one runs and the skip is on (<see cref="MessageSkip.SkipsScene"/>),
    /// the game neither shows nor draws it: the script still runs every statement, so the state comes
    /// out as when it is watched (it has no menu); the waits and fades take no time and the engine
    /// timer ticks per read (<see cref="EngineTimer"/>). docs/window-menu.md, MsgSkip.
    /// </summary>
    public static IReadOnlySet<string> WholeScenes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "ｵｰﾌﾟﾆﾝｸﾞ.ADV",
    };

    /// <summary>The original's facts for the engine.</summary>
    // Declared last: static initializers run in text order, and this one reads the ones above.
    public static VariantProfile Profile { get; } = new()
    {
        VariableCount = VariableCatalog.Variables.Count,
        EventEntryVariable = 520,                                       // VAR0520 Ev_Entry
        // SET.ADV allocates these with the save flag (DC n, max, 1: :135, :241, :282, :320); page 3,
        // the scratch buffer (:269), is not saved.
        SavedPages = [1, 2, 4, 10],
        IsSceneFile = IsSceneFile,
        WholeScenes = WholeScenes,
    };
}
