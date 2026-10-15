using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Nagi's (unit 40) Black D. Beam: the beam (<see cref="WhiteBeamEffect"/>, laid over dark) without its opening orb —
/// the flash, the sphere it turns into and the sparkles round its rim (owner, 2026-10-15) — so that it starts with the
/// needle from her sprite (CG 1525); moved 150 units left and stretched right past the flash so that it runs on through
/// the enemy's army, as Gandhi's (owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class NagiBeamEffect
{
    /// <summary>Where it is moved and how far it is stretched past the flash, through the enemy's army (owner, 2026-10-15).</summary>
    private static readonly Vector2 Offset = new(-150, 0);

    private const float Reach = 1.45f;

    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Placed(seed, Offset, reach: Reach, orb: false);
}
