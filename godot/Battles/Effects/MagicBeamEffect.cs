using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Magic's (unit 39) White D. Beam: the white beam (<see cref="WhiteBeamEffect"/>) moved 140 units left and 120 up,
/// onto her sprite (CG 1523; owner, 2026-10-15), and turned down about its flash so that it runs into the enemy's army
/// instead of over its heads (owner): from the flash, now at (160, 48), toward the army's middle, about (640, 185).
/// Stretched right past the sphere, so that it runs on through the army instead of dying in its middle (owner).
/// The same seed makes the same effect.
/// </summary>
internal static class MagicBeamEffect
{
    private static readonly Vector2 Offset = new(-140, -120);

    /// <summary>The turn down, in radians (~16°).</summary>
    private const float Tilt = 0.28f;

    /// <summary>How far the beam is stretched past her sphere, to run on through the enemy's army (owner, 2026-10-15).</summary>
    private const float Reach = 1.5f;

    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Placed(seed, Offset, Tilt, Reach);
}
