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
/// pictures) cut to a square: 144 pixels of its middle from just below the gold frame (y 7), the
/// owner's choice (2026-10-12), but where the face sits elsewhere (<see cref="Exceptions"/>; the
/// owner's own in Debug → Portraits, <see cref="FaceCrops"/>).
/// </summary>
public static class UnitFaces
{
    /// <summary>A face CG's size: every <c>WAO_SUB</c> portrait is 192×256.</summary>
    public const int Width = 192, Height = 256;

    /// <summary>The usual square: 144 pixels, centred, from y 7 — the portrait's gold frame stays out (owner, 2026-10-12).</summary>
    public static FaceCrop Default { get; } = new(24, 7, 144);

    /// <summary>
    /// The owner's own squares, by CG, set in Debug → Portraits (2026-10-12): closer or moved where the
    /// usual square shows too much or misses the face (Asuka's, unit 11, sits under Pigu's hat). A
    /// face many commanders share is named by one or two of them.
    /// </summary>
    public static IReadOnlyDictionary<int, FaceCrop> Exceptions { get; } = new Dictionary<int, FaceCrop>
    {
        [1] = new(33, 7, 128),     // Rance (unit 1)
        [2] = new(32, 7, 136),     // Sill (unit 2)
        [3] = new(28, 7, 136),     // Bound (unit 3)
        [4] = new(25, 7, 136),     // Soul (unit 4)
        [7] = new(28, 7, 136),     // Leila (unit 5)
        [8] = new(31, 7, 128),     // Menad (unit 10)
        [10] = new(32, 7, 128),    // Haurein (unit 12), Haurein (commander 2)
        [11] = new(31, 7, 136),    // Melfeis (unit 13), Melfeis (commander 4)
        [24] = new(30, 10, 120),   // Isoroku (unit 27), Isoroku Yamamoto (commander 82)
        [26] = new(38, 7, 128),    // Cream (unit 28), Cream (commander 45)
        [28] = new(25, 7, 128),    // Katyusha (commander 51)
        [29] = new(44, 11, 120),   // Saulnia (commander 52)
        [42] = new(36, 7, 120),    // Gandhi (unit 37)
        [45] = new(28, 7, 128),    // Nagi (unit 40), ？？？？？？？ (unit 45), Nagi (commander 107)
        [46] = new(40, 7, 136),    // Somita (unit 46)
        [49] = new(31, 7, 136),    // Shizuka (unit 17)
        [55] = new(40, 7, 120),    // Kentarou (unit 31)
        [58] = new(31, 7, 136),    // Magic (unit 39), Magic (commander 110)
        [61] = new(20, 7, 144),    // Chizuko (unit 38)
        [66] = new(30, 7, 136),    // Eleanor (unit 23)
        [67] = new(38, 7, 128),    // Milli (unit 24)
        [68] = new(30, 29, 136),   // Mill (unit 25)
        [73] = new(28, 7, 128),    // Silky (commander 156)
        [75] = new(37, 6, 136),    // Satella (unit 29), Satella (commander 157)
        [76] = new(25, 7, 128),    // Aviator (unit 36)
        [84] = new(20, 21, 144),   // PG-7 (commander 136)
        [85] = new(66, 16, 120),   // PG-9 (commander 137)
        [89] = new(37, 7, 128),    // Cecil (unit 50)
        [402] = new(15, 9, 136),   // Barres (unit 6)
        [403] = new(44, 7, 136),   // Rick (unit 7)
        [404] = new(28, 7, 128),   // Cordoba (unit 8)
        [405] = new(31, 8, 120),   // Ex (unit 14), Ex (commander 1)
        [406] = new(38, 7, 112),   // Kinkaid (unit 9)
        [408] = new(33, 52, 144),  // Desert Guide (commander 18), Desert Guide (commander 19) and 1 more
        [412] = new(20, 7, 152),   // Hanna Hero (commander 9), Hanna Hero (commander 10) and 14 more
        [414] = new(35, 15, 120),  // Mars the Great (unit 26), Mars the Great (commander 71)
        [415] = new(20, 7, 160),   // Kayblis (commander 135)
        [418] = new(18, 6, 136),   // Kenichi Oda (commander 73), Yamato Akechi (commander 74) and 3 more
        [423] = new(22, 7, 128),   // Patton (unit 18), Patton (commander 54)
        [424] = new(32, 10, 120),  // Hanty (unit 19), Hanty (commander 57)
        [425] = new(46, 7, 128),   // Hubert (unit 21), Hubert (commander 55)
        [426] = new(28, 7, 144),   // Freak (unit 20), Freak (commander 56)
        [428] = new(28, 7, 120),   // Seizel Sisters (unit 42), La Seizel (commander 90)
        [430] = new(26, 45, 144),  // Asuka (unit 11)
        [432] = new(25, 7, 136),   // Aristoles (unit 43), Aristoles (commander 32)
        [434] = new(44, 7, 128),   // Lelyukov (unit 22), Lelyukov (commander 22)
        [435] = new(43, 7, 136),   // Tami (commander 40)
        [436] = new(40, 22, 136),  // Maha (commander 41)
        [439] = new(33, 7, 120),   // Gadman (commander 23), Jackal (commander 24) and 14 more
        [447] = new(63, 11, 120),  // Meglass (unit 30), Meglass (commander 158)
        [452] = new(37, 7, 128),   // Galtia (unit 41), Galtia (commander 131)
        [453] = new(9, 10, 152),   // Redeye (commander 130)
        [454] = new(9, 10, 176),   // Babolat (commander 129)
        [456] = new(30, 7, 144),   // Saias (commander 113)
        [459] = new(34, 7, 120),   // Alex (commander 125)
        [463] = new(16, 8, 152),   // Gurwin (unit 44), Temple Knight (commander 139) and 3 more
        [485] = new(42, 7, 120),   // Angel Knight (commander 146), Angel Knight (commander 147)
        [498] = new(28, 7, 136),   // Rolex (unit 47)
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
