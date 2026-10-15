using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Chizuko's (unit 38) Black D. Beam: the beam (<see cref="WhiteBeamEffect"/>, laid over dark) moved 27 units left and
/// 20 up, onto her sprite (CG 1526; owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class ChizukoBeamEffect
{
    private static readonly Vector2 Offset = new(-27, -20);

    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Placed(seed, Offset);
}
