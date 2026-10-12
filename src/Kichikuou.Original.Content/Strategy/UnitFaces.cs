using System.Globalization;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A square part of a face CG (192×256), in the CG's own pixels.</summary>
public readonly record struct FaceCrop(int X, int Y, int Size)
{
    /// <summary>This square moved and sized to lie inside a face CG (at least 32 pixels).</summary>
    public FaceCrop Clamped()
    {
        int size = Math.Clamp(Size, 32, UnitFaces.Width);
        return new(Math.Clamp(X, 0, UnitFaces.Width - size), Math.Clamp(Y, 0, UnitFaces.Height - size), size);
    }
}

/// <summary>A record's face for our screens: a unit or a commander, its name and its face CG (0 = none).</summary>
public sealed record RecordFace(int Record, string Name, int FaceCg);

/// <summary>
/// The faces on our own screens (docs/ui/deployment-table.md, third part): the face CG a unit shows
/// in the dialogues (<c>Unit_FaceCg</c>, which scenes change at run time — unit 48 becomes the Hanny
/// King or the Kaiju Prince) or a commander in the battle (<c>Cmdr_FaceCg</c>, the same 192×256
/// pictures) cut to a square: the top 144 pixels of its middle, the owner's choice (2026-10-12), but
/// where the face sits elsewhere (<see cref="Exceptions"/>; the owner's own in Debug → Portraits,
/// <see cref="FaceCrops"/>).
/// </summary>
public static class UnitFaces
{
    /// <summary>A face CG's size: every <c>WAO_SUB</c> portrait is 192×256.</summary>
    public const int Width = 192, Height = 256;

    /// <summary>The usual square: 144 pixels, centred, from the top.</summary>
    public static FaceCrop Default { get; } = new(24, 0, 144);

    /// <summary>Faces the usual square misses, by CG: Asuka's (unit 11) sits under Pigu's hat.</summary>
    public static IReadOnlyDictionary<int, FaceCrop> Exceptions { get; } = new Dictionary<int, FaceCrop>
    {
        [430] = new(24, 64, 144),
    };

    /// <summary>The square shown for face CG <paramref name="faceCg"/>.</summary>
    public static FaceCrop CropOf(int faceCg) => Exceptions.TryGetValue(faceCg, out var crop) ? crop : Default;

    /// <summary>Units 1-50 (the deployment screen's), each with its name and face as the state has them now.</summary>
    public static IReadOnlyList<RecordFace> Units(GameState state)
    {
        var faces = new List<RecordFace>();
        for (int n = 1; n <= DeploymentTable.LastUnit; n++)
            faces.Add(new RecordFace(n, DeploymentTable.UnitName(state, n).Trim(), state.Units[n].FaceCg.Value));
        return faces;
    }

    /// <summary>
    /// The commanders with a name or a face as the state has them now (the boot's <c>SET_CHR</c>
    /// fills 1-199; scenes fill commander 100 before their battles).
    /// </summary>
    public static IReadOnlyList<RecordFace> Commanders(GameState state)
    {
        var faces = new List<RecordFace>();
        for (int n = 1; n < state.Cmdrs.Count; n++)
        {
            string name = DeploymentTable.StringAt(state, (state.Cmdr_StrBase + (Sys3Int)((n - 1) * 3)).Value).Trim();
            int face = state.Cmdrs[n].FaceCg.Value;
            if (name.Length > 0 || face != 0)
                faces.Add(new RecordFace(n, name, face));
        }
        return faces;
    }
}

/// <summary>
/// The squares in use: <see cref="UnitFaces"/>' own and the owner's edits from Debug → Portraits,
/// which a text file keeps (one line a face: <c>cg x y size</c>) until they join the exceptions.
/// </summary>
public sealed class FaceCrops
{
    private readonly SortedDictionary<int, FaceCrop> _edits = [];

    /// <summary>The owner's edits, by CG.</summary>
    public IReadOnlyDictionary<int, FaceCrop> Edits => _edits;

    /// <summary>The square shown for face CG <paramref name="faceCg"/>: the edit, else <see cref="UnitFaces.CropOf"/>.</summary>
    public FaceCrop CropOf(int faceCg) => _edits.TryGetValue(faceCg, out var crop) ? crop : UnitFaces.CropOf(faceCg);

    /// <summary>Whether face CG <paramref name="faceCg"/> has an edit.</summary>
    public bool IsEdited(int faceCg) => _edits.ContainsKey(faceCg);

    /// <summary>Edits face CG <paramref name="faceCg"/>'s square (kept inside the CG); an edit back to the built-in square is dropped.</summary>
    public void Set(int faceCg, FaceCrop crop)
    {
        crop = crop.Clamped();
        if (crop == UnitFaces.CropOf(faceCg))
            _edits.Remove(faceCg);
        else
            _edits[faceCg] = crop;
    }

    /// <summary>Drops face CG <paramref name="faceCg"/>'s edit.</summary>
    public void Reset(int faceCg) => _edits.Remove(faceCg);

    /// <summary>The edits as the file keeps them.</summary>
    public string Write() =>
        string.Concat(_edits.Select(e => FormattableString.Invariant($"{e.Key} {e.Value.X} {e.Value.Y} {e.Value.Size}\n")));

    /// <summary>The edits of a file written by <see cref="Write"/>; lines that do not read are skipped.</summary>
    public static FaceCrops Read(string text)
    {
        var crops = new FaceCrops();
        foreach (string line in text.Split('\n'))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 4 && parts.All(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                var n = parts.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
                crops.Set(n[0], new FaceCrop(n[1], n[2], n[3]));
            }
        }
        return crops;
    }
}
