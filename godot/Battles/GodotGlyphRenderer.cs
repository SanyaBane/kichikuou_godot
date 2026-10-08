using Godot;
using Kichikuou.Content.Screen;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The game font for the emulated surface: MS Gothic (face 0 of <c>msgothic.ttc</c>), bold, as
/// xsystem35 uses on Windows (docs/assets.md §6), rasterised by Godot's TextServer at the
/// surface's resolution. Falls back to Godot's default font when MS Gothic is missing. Glyph
/// bitmaps are read back from the TextServer's cache texture and kept.
/// </summary>
public sealed class GodotGlyphRenderer : IGlyphRenderer, IDisposable
{
    private const string MsGothic = @"C:\Windows\Fonts\msgothic.ttc";

    /// <summary>Synthetic bold strength (TextServer <c>embolden</c>), standing in for GDI's bold.</summary>
    private const double Embolden = 0.6;

    private readonly TextServer _ts = TextServerManager.GetPrimaryInterface();
    private readonly Rid _font;
    private readonly bool _ownsFont;
    private readonly Dictionary<(char, int), Glyph?> _cache = [];

    public GodotGlyphRenderer()
    {
        if (System.IO.File.Exists(MsGothic))
        {
            _font = _ts.CreateFont();
            _ownsFont = true;
            _ts.FontSetData(_font, System.IO.File.ReadAllBytes(MsGothic));
            _ts.FontSetFaceIndex(_font, 0);
            FaceName = "MS Gothic";
        }
        else
        {
            _font = ThemeDB.FallbackFont.GetRids()[0];
            FaceName = "fallback";
        }
        _ts.FontSetAntialiasing(_font, TextServer.FontAntialiasing.Gray);
        _ts.FontSetHinting(_font, TextServer.Hinting.Light);
        _ts.FontSetDisableEmbeddedBitmaps(_font, true);
        _ts.FontSetEmbolden(_font, Embolden);
    }

    /// <summary>Which font is in use.</summary>
    public string FaceName { get; }

    public Glyph? Render(char c, int pixelSize)
    {
        if (_cache.TryGetValue((c, pixelSize), out var cached))
            return cached;
        var glyph = Rasterise(c, pixelSize);
        _cache[(c, pixelSize)] = glyph;
        return glyph;
    }

    private Glyph? Rasterise(char c, int pixelSize)
    {
        var size = new Vector2I(pixelSize, 0);
        long index = _ts.FontGetGlyphIndex(_font, pixelSize, c, 0);
        if (index == 0)
            return null;
        _ts.FontRenderGlyph(_font, size, index);
        long texture = _ts.FontGetGlyphTextureIdx(_font, size, index);
        if (texture < 0)
            return null;
        var uv = _ts.FontGetGlyphUVRect(_font, size, index);
        var region = new Rect2I((Vector2I)uv.Position.Floor(), (Vector2I)uv.Size.Ceil());
        if (region.Size.X <= 0 || region.Size.Y <= 0)
            return null;
        var image = _ts.FontGetTextureImage(_font, size, texture).GetRegion(region);
        image.Convert(Image.Format.Rgba8);
        byte[] rgba = image.GetData();
        var coverage = new byte[region.Size.X * region.Size.Y];
        for (int i = 0; i < coverage.Length; i++)
            coverage[i] = rgba[i * 4 + 3];
        var offset = _ts.FontGetGlyphOffset(_font, size, index);
        double ascent = _ts.FontGetAscent(_font, pixelSize);
        return new Glyph(region.Size.X, region.Size.Y, coverage,
            (int)Math.Round(offset.X), (int)Math.Round(ascent + offset.Y));
    }

    public void Dispose()
    {
        if (_ownsFont)
            _ts.FreeRid(_font);
    }
}
