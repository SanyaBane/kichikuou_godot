using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Shizuka's (unit 17) White D. Beam: the white beam (<see cref="WhiteBeamEffect"/>) moved 76 units left, onto her
/// sprite (CG 1522; owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class ShizukaBeamEffect
{
    private static readonly Vector2 Offset = new(-76, 0);

    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Placed(seed, Offset);
}
