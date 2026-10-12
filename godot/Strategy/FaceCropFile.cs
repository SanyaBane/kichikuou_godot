using Godot;
using Kichikuou.Original.Content.Strategy;
using FileAccess = Godot.FileAccess;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The owner's edits of the faces' squares (Debug → Portraits, <see cref="FaceCrops"/>), kept in
/// <c>user://face-crops.txt</c> until the agent moves them into <see cref="UnitFaces.Exceptions"/>.
/// </summary>
internal static class FaceCropFile
{
    public const string Path = "user://face-crops.txt";

    /// <summary>The edits saved so far (none without the file).</summary>
    public static FaceCrops Load() =>
        FileAccess.FileExists(Path) && FileAccess.GetFileAsString(Path) is { } text ? FaceCrops.Read(text) : new FaceCrops();

    /// <summary>Saves <paramref name="crops"/>' edits.</summary>
    public static void Save(FaceCrops crops)
    {
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file?.StoreString(crops.Write());
    }

    /// <summary>The file's place on disk, for the viewer to show.</summary>
    public static string GlobalPath => ProjectSettings.GlobalizePath(Path);
}
