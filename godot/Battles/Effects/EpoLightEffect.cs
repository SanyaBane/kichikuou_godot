using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Gandhi's (unit 37) EPO Light: the white beam (<see cref="WhiteBeamEffect"/>) moved 150 units left and 40 up, onto
/// his sprite (CG 1524; owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class EpoLightEffect
{
    private static readonly Vector2 Offset = new(-150, -40);

    public static IReadOnlyList<Particle> Build(int seed) => WhiteBeamEffect.Placed(seed, Offset);
}
