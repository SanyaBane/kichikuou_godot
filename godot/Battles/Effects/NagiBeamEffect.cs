namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Nagi's (unit 40) Black D. Beam: the beam (<see cref="WhiteBeamEffect"/>, laid over dark) without its opening orb —
/// the flash, the sphere it turns into and the sparkles round its rim (owner, 2026-10-15) — so that it starts with the
/// needle from her sprite (CG 1525). The same seed makes the same effect.
/// </summary>
internal static class NagiBeamEffect
{
    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Build(seed, orb: false);
}
