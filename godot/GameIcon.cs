using System.IO;
using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game;

/// <summary>
/// <c>--make-icon</c>: the game's icon (owner, 2026-11-09; docs/release.md, "The icon"), the title's Start character without
/// its word (<see cref="PhoneTitleArt.Character"/>), cut from the player's own CGs into <c>godot/icon</c>, which the repository
/// keeps as the one exception to the rule against the original's assets. project.godot and the export presets name its
/// files, and Godot's exporters read them as they are (its <c>.gdignore</c> keeps them out of the import).
/// </summary>
internal static class GameIcon
{
    private const string Folder = "res://icon";

    /// <summary>The desktop's icon: the largest size Windows keeps, the figure with this margin in CG pixels.</summary>
    private const int DesktopSide = 256, DesktopMargin = 2;

    /// <summary>The running window's icon: Windows stretches what the window is given to this size unsmoothed, so it is made at it.</summary>
    private const int WindowSide = 32;

    /// <summary>Android's adaptive layers: 108 dp at 4 pixels a dp; a launcher's mask spares the middle 66 dp.</summary>
    private const int AdaptiveSide = 432, SafeRadius = 132;

    /// <summary>Each CG pixel is made this many before the scale down, so the figure keeps its pixels' edges.</summary>
    private const int Sharpen = 4;

    public static bool Make()
    {
        if (!GameData.UsesExtract && GameData.Imported is null && !GameData.OpenImport(out string? problem))
        {
            GD.PrintErr($"The icon: no game data: {problem}");
            return false;
        }
        var cgs = GameData.Cgs();
        if (cgs.Get(PhoneTitleArt.BackgroundCg) is not { } background || cgs.Get(PhoneTitleArt.LitCg) is not { } lit)
        {
            GD.PrintErr("The icon: no CG 301 or 302 in the game's data.");
            return false;
        }
        var start = PhoneTitleArt.Character(background, lit, PhoneTitlePortrait.Start);
        var figure = Image.CreateFromData(start.Width, start.Height, false, Image.Format.Rgba8, start.Rgba);
        figure.FixAlphaEdges();                             // the clear pixels take their neighbours' colour: no dark edge when scaled

        string folder = ProjectSettings.GlobalizePath(Folder);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ".gdignore"), "");

        var back = Image.CreateEmpty(AdaptiveSide, AdaptiveSide, false, Image.Format.Rgba8);
        back.Fill(UiPalette.Lacquer.Backdrop);
        float desktop = (float)DesktopSide / (Math.Max(start.Width, start.Height) + 2 * DesktopMargin);
        var icon = Centred(figure, DesktopSide, desktop);
        var window = (Image)icon.Duplicate();
        window.Resize(WindowSide, WindowSide, Image.Interpolation.Lanczos);
        var foreground = Centred(figure, AdaptiveSide, SafeRadius / Reach(start));
        bool saved = Save(icon, folder, "icon.png")
            && Save(window, folder, "window.png")
            && Save(foreground, folder, "android-foreground.png")
            && Save(back, folder, "android-background.png")
            && Save(Monochrome(foreground), folder, "android-monochrome.png");
        if (saved)
            GD.Print($"The icon: saved into {folder}.");
        return saved;
    }

    /// <summary>The figure at <paramref name="scale"/> in the middle of a clear square.</summary>
    private static Image Centred(Image figure, int side, float scale)
    {
        int w = Mathf.RoundToInt(figure.GetWidth() * scale), h = Mathf.RoundToInt(figure.GetHeight() * scale);
        var scaled = (Image)figure.Duplicate();
        scaled.Resize(figure.GetWidth() * Sharpen, figure.GetHeight() * Sharpen, Image.Interpolation.Nearest);
        scaled.Resize(w, h, Image.Interpolation.Lanczos);
        var canvas = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
        canvas.BlitRect(scaled, new Rect2I(0, 0, w, h), new Vector2I((side - w) / 2, (side - h) / 2));
        canvas.FixAlphaEdges();
        return canvas;
    }

    /// <summary>
    /// Android's themed layer, which it tints by its alpha alone (without it the template's Godot logo shows): the figure
    /// white, its dark lines clear, so the face and the armour still read.
    /// </summary>
    private static Image Monochrome(Image foreground)
    {
        var layer = (Image)foreground.Duplicate();
        for (int y = 0; y < layer.GetHeight(); y++)
            for (int x = 0; x < layer.GetWidth(); x++)
            {
                var c = layer.GetPixel(x, y);
                layer.SetPixel(x, y, new Color(1, 1, 1, c.A * Mathf.SmoothStep(DarkLine, DarkLine + 0.15f, c.Luminance)));
            }
        return layer;
    }

    /// <summary>The luminance under which <see cref="Monochrome"/> leaves a pixel clear.</summary>
    private const float DarkLine = 0.25f;

    /// <summary>How far the picture's farthest opaque pixel lies from its middle, in its own pixels.</summary>
    private static float Reach(PhoneTitleArt.Picture p)
    {
        float most = 0;
        for (int y = 0; y < p.Height; y++)
            for (int x = 0; x < p.Width; x++)
                if (p.Rgba[(y * p.Width + x) * 4 + 3] != 0)
                    most = Math.Max(most, new Vector2(x + 0.5f - p.Width / 2f, y + 0.5f - p.Height / 2f).Length());
        return most;
    }

    private static bool Save(Image image, string folder, string name)
    {
        var result = image.SavePng(Path.Combine(folder, name));
        if (result != Error.Ok)
            GD.PrintErr($"The icon: {name} not saved: {result}.");
        return result == Error.Ok;
    }
}
