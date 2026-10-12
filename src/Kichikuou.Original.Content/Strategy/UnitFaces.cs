using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A square part of a face CG (192×256), in the CG's own pixels.</summary>
public readonly record struct FaceCrop(int X, int Y, int Size);

/// <summary>A unit's face for our screens: the unit, its name, its face CG (<c>Unit_FaceCg</c>, 0 = none) and status.</summary>
public sealed record UnitFace(int Unit, string Name, int FaceCg, int Status);

/// <summary>
/// The units' faces on our own screens (docs/ui/deployment-table.md, third part): the face CG a unit
/// shows in the dialogues (<c>Unit_FaceCg</c>, which scenes change at run time — unit 48 becomes the
/// Hanny King or the Kaiju Prince) cut to a square: the top 144 pixels of its middle, the owner's
/// choice (2026-10-12), but where the face sits lower (<see cref="Exceptions"/>).
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
    public static IReadOnlyList<UnitFace> Units(GameState state)
    {
        var faces = new List<UnitFace>();
        for (int n = 1; n <= DeploymentTable.LastUnit; n++)
        {
            var u = state.Units[n];
            faces.Add(new UnitFace(n, DeploymentTable.UnitName(state, n).Trim(), u.FaceCg.Value, u.Status.Value));
        }
        return faces;
    }
}
