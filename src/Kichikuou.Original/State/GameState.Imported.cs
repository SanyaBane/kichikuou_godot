namespace Kichikuou.Original.State;

public sealed partial class GameState
{
    /// <summary>The key of <see cref="ImportedFromOriginal"/> in <see cref="Engine.Script.ScriptState.Extras"/>.</summary>
    public const string ImportedFromOriginalKey = "imported-original";

    /// <summary>
    /// Ours (owner, 2026-11-10; docs/save-import.md, §7): whether the game was imported from a save of the original
    /// (File → Import Original Save…). The import writes the mark, every save of the game carries it on and a load brings
    /// it back with the rest of <see cref="Engine.Script.ScriptState.Extras"/>; a new game starts without it. An original
    /// save keeps no record of the lines read, so in a marked game "New scenes first" holds not
    /// (<see cref="Script.OriginalScriptContext.NewScenesFirst"/>) and our screens show no talk marks; no rule of the
    /// original reads it.
    /// </summary>
    public bool ImportedFromOriginal
    {
        get => Extras.ContainsKey(ImportedFromOriginalKey);
        set
        {
            if (value)
                Extras[ImportedFromOriginalKey] = [1];
            else
                Extras.Remove(ImportedFromOriginalKey);
        }
    }
}
