using System.Globalization;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A square part of a face CG (192×256) or of a stand-in's battle sprite, in the CG's own pixels.</summary>
public readonly record struct FaceCrop(int X, int Y, int Size)
{
    /// <summary>This square moved and sized to lie inside a CG of <paramref name="width"/>×<paramref name="height"/> (at least 32 pixels).</summary>
    public FaceCrop Clamped(int width = UnitFaces.Width, int height = UnitFaces.Height)
    {
        int size = Math.Clamp(Size, 32, Math.Min(width, height));
        return new(Math.Clamp(X, 0, width - size), Math.Clamp(Y, 0, height - size), size);
    }
}

/// <summary>
/// A record's face for our screens: a unit or a commander, its name, its face CG (0 = none) and, for
/// a commander without a face, the battle sprite that stands in for it (<c>Cmdr_CgAlone</c>, the
/// commander without troops; 0 otherwise).
/// </summary>
public sealed record RecordFace(int Record, string Name, int FaceCg, int StandIn = 0)
{
    /// <summary>The CG our screens cut: the face, else the stand-in (0: none).</summary>
    public int Picture => FaceCg != 0 ? FaceCg : StandIn;
}

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

    /// <summary>
    /// The square shown for CG <paramref name="cg"/> of <paramref name="width"/>×<paramref name="height"/>:
    /// its exception, else <see cref="Default"/> for a face, else 144 pixels in the middle (a stand-in's
    /// battle sprite, 320×256).
    /// </summary>
    public static FaceCrop CropOf(int cg, int width = Width, int height = Height) =>
        Exceptions.TryGetValue(cg, out var crop) ? crop
        : (width, height) == (Width, Height) ? Default
        : new FaceCrop((width - 144) / 2, (height - 144) / 2, 144).Clamped(width, height);

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
    /// fills 1-199; scenes fill commander 100 before their battles). One without a face — monsters,
    /// which never speak in a window: the Hannies, the Kaiju, Aladdin, PG-X mk2 — has its battle sprite
    /// as a stand-in (owner, 2026-10-12), the one without troops (<c>Cmdr_CgAlone</c>, else <c>Cmdr_CgFull</c>).
    /// </summary>
    public static IReadOnlyList<RecordFace> Commanders(GameState state)
    {
        var faces = new List<RecordFace>();
        for (int n = 1; n < state.Cmdrs.Count; n++)
        {
            var c = state.Cmdrs[n];
            string name = DeploymentTable.StringAt(state, (state.Cmdr_StrBase + (Sys3Int)((n - 1) * 3)).Value).Trim();
            int face = c.FaceCg.Value;
            int standIn = face != 0 ? 0 : c.CgAlone != 0 ? c.CgAlone.Value : c.CgFull.Value;
            if (name.Length > 0 || face != 0)
                faces.Add(new RecordFace(n, name, face, standIn));
        }
        return faces;
    }
}

/// <summary>
/// The squares in use: <see cref="UnitFaces"/>' own and the owner's edits from Debug → Portraits,
/// which a text file keeps (one line a CG: <c>cg x y size</c>, and the CG's width and height when it is
/// not a face's 192×256) until they join the exceptions.
/// </summary>
public sealed class FaceCrops
{
    private readonly SortedDictionary<int, (FaceCrop Crop, int Width, int Height)> _edits = [];

    /// <summary>The owner's edits, by CG.</summary>
    public IReadOnlyDictionary<int, FaceCrop> Edits => _edits.ToDictionary(e => e.Key, e => e.Value.Crop);

    /// <summary>The square shown for CG <paramref name="cg"/> of the size given: the edit, else <see cref="UnitFaces.CropOf"/>.</summary>
    public FaceCrop CropOf(int cg, int width = UnitFaces.Width, int height = UnitFaces.Height) =>
        _edits.TryGetValue(cg, out var edit) ? edit.Crop : UnitFaces.CropOf(cg, width, height);

    /// <summary>Whether CG <paramref name="cg"/> has an edit.</summary>
    public bool IsEdited(int cg) => _edits.ContainsKey(cg);

    /// <summary>
    /// Edits CG <paramref name="cg"/>'s square, kept inside the CG of the size given; an edit back to
    /// the built-in square is dropped.
    /// </summary>
    public void Set(int cg, FaceCrop crop, int width = UnitFaces.Width, int height = UnitFaces.Height)
    {
        crop = crop.Clamped(width, height);
        if (crop == UnitFaces.CropOf(cg, width, height))
            _edits.Remove(cg);
        else
            _edits[cg] = (crop, width, height);
    }

    /// <summary>Drops CG <paramref name="cg"/>'s edit.</summary>
    public void Reset(int cg) => _edits.Remove(cg);

    /// <summary>The edits as the file keeps them.</summary>
    public string Write() =>
        string.Concat(_edits.Select(e =>
        {
            var (c, w, h) = e.Value;
            string size = (w, h) == (UnitFaces.Width, UnitFaces.Height) ? "" : FormattableString.Invariant($" {w} {h}");
            return FormattableString.Invariant($"{e.Key} {c.X} {c.Y} {c.Size}{size}\n");
        }));

    /// <summary>The edits of a file written by <see cref="Write"/>; lines that do not read are skipped.</summary>
    public static FaceCrops Read(string text)
    {
        var crops = new FaceCrops();
        foreach (string line in text.Split('\n'))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is 4 or 6 && parts.All(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                var n = parts.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
                if (n.Length == 6 && (n[4] <= 0 || n[5] <= 0))
                    continue;
                crops.Set(n[0], new FaceCrop(n[1], n[2], n[3]), n.Length == 6 ? n[4] : UnitFaces.Width, n.Length == 6 ? n[5] : UnitFaces.Height);
            }
        }
        return crops;
    }
}
