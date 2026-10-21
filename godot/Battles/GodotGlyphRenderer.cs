using Godot;
using Kichikuou.Content.Screen;

namespace Kichikuou.Game.Battles;

/// <summary>The fonts the surface's text can be drawn in (Option → Display, docs/window-menu.md).</summary>
public enum GameFont
{
    /// <summary>MS Gothic, face 0 of <c>msgothic.ttc</c>: the original's font (docs/assets.md §6); Windows only.</summary>
    MsGothic,

    /// <summary>BIZ UDGothic (SIL OFL 1.1, <c>godot/fonts</c>): bundled, the same cells, on every platform.</summary>
    BizUdGothic,
}

/// <summary>
/// The game font for the emulated surface, bold, as xsystem35 uses it (docs/assets.md §6):
/// the bundled BIZ UDGothic, or MS Gothic where it is installed — whichever Option → Display chose —
/// rasterised by Godot's TextServer at the surface's resolution. Every character takes a fixed
/// cell (<see cref="MessageWriter.CellWidth"/>), so the font changes how the glyphs look, never the
/// layout. Glyph bitmaps are read back from the TextServer's cache texture and kept.
/// </summary>
public sealed class GodotGlyphRenderer : IGlyphRenderer, IDisposable
{
    internal const string MsGothicPath = @"C:\Windows\Fonts\msgothic.ttc";

    /// <summary>Kept as is in an export (its <c>.import</c> says <c>keep</c>), so <c>FileAccess</c> reads it there too.</summary>
    internal const string BizUdGothicPath = "res://fonts/BIZUDGothic-Regular.ttf";

    /// <summary>Synthetic bold strength (TextServer <c>embolden</c>), standing in for GDI's bold.</summary>
    private const double Embolden = 0.6;

    private readonly TextServer _ts = TextServerManager.GetPrimaryInterface();
    private readonly Dictionary<(char, int), Glyph?> _cache = [];
    private Rid _font;

    public GodotGlyphRenderer() => Font = Chosen;

    /// <summary>Whether MS Gothic is installed (on a Windows without the Japanese fonts it is not).</summary>
    public static bool MsGothicInstalled => System.IO.File.Exists(MsGothicPath);

    /// <summary>
    /// The font the player chose: <c>--font ms-gothic|biz-ud-gothic</c> for the launch (development),
    /// else <c>[display] font</c> in <c>user://settings.cfg</c>, else BIZ UDGothic (owner, 2026-10-21).
    /// </summary>
    public static GameFont Chosen
    {
        get
        {
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--font");
            if (at >= 0 && at + 1 < args.Length && Parse(args[at + 1]) is { } given)
                return given;
            var config = new ConfigFile();
            if (config.Load(GameData.SettingsPath) == Error.Ok && Parse((string)config.GetValue("display", "font", "")) is { } saved)
                return saved;
            return GameFont.BizUdGothic;
        }
        set
        {
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "font", value == GameFont.MsGothic ? "ms-gothic" : "biz-ud-gothic");
            config.Save(GameData.SettingsPath);
        }
    }

    private static GameFont? Parse(string name) => name switch
    {
        "ms-gothic" => GameFont.MsGothic,
        "biz-ud-gothic" => GameFont.BizUdGothic,
        _ => null,
    };

    /// <summary>
    /// The font in use; MS Gothic where it is not installed is BIZ UDGothic. Setting it drops the
    /// glyphs rendered so far: what is drawn from now on uses the new font.
    /// </summary>
    public GameFont Font
    {
        get;
        set
        {
            if (value == GameFont.MsGothic && !MsGothicInstalled)
                value = GameFont.BizUdGothic;
            byte[] data = value == GameFont.MsGothic
                ? System.IO.File.ReadAllBytes(MsGothicPath)
                : Godot.FileAccess.GetFileAsBytes(BizUdGothicPath);
            if (data.Length == 0)
                throw new InvalidOperationException($"The bundled font {BizUdGothicPath} cannot be read.");
            var font = _ts.CreateFont();
            _ts.FontSetData(font, data);
            _ts.FontSetFaceIndex(font, 0);
            _ts.FontSetAntialiasing(font, TextServer.FontAntialiasing.Gray);
            _ts.FontSetHinting(font, TextServer.Hinting.Light);
            _ts.FontSetDisableEmbeddedBitmaps(font, true);
            _ts.FontSetEmbolden(font, Embolden);
            if (_font.IsValid)
                _ts.FreeRid(_font);
            _font = font;
            _cache.Clear();
            field = value;
        }
    }

    /// <summary>Which font is in use.</summary>
    public string FaceName => Font == GameFont.MsGothic ? "MS Gothic" : "BIZ UDGothic";

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
        if (_font.IsValid)
            _ts.FreeRid(_font);
    }
}
