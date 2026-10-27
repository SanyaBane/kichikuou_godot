using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The screen's picture shown lower in the map's room under our status strip (<see cref="MapHeadroom"/>): a material for
/// the picture's <see cref="TextureRect"/> that draws it a number of script pixels lower, the band over its top the map's
/// first rows mirrored (the strip's lacquer is a little see-through, so the map seems to go on under it).
/// </summary>
internal static class MapRoomPicture
{
    private const string Code = """
        shader_type canvas_item;

        uniform float shift = 0.0;

        void fragment() {
            COLOR = texture(TEXTURE, vec2(UV.x, abs(UV.y - shift)));
        }
        """;

    private static Shader? _shader;

    public static ShaderMaterial Material() => new() { Shader = _shader ??= new Shader { Code = Code } };

    /// <summary>Draws the picture <paramref name="shift"/> script pixels lower (0: as it is), to the screen's pixels (two a script pixel).</summary>
    public static void SetShift(ShaderMaterial material, float shift) =>
        material.SetShaderParameter("shift", MathF.Round(shift * Surface.Scale) / (Surface.ScreenHeight * Surface.Scale));
}
