using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Battles.Effects;

namespace Kichikuou.Game.ScreenGallery;

internal static partial class GalleryEntries
{
    // ---------------------------------------------------------------- Combat VFX (owner, 2026-10-29; docs/ui/gallery.md, §11)

    /// <summary>
    /// The Combat VFX tab's entries, a pass of their own (<c>--combat-vfx</c>, <c>gallery.ps1 -Vfx</c>): each exchange effect
    /// play shows (the unused Ninja troop's left out) as a 2×3 grid of its frames (<see cref="EffectShow.GridFrames"/>) over
    /// the battlefield with its armies (<see cref="EffectCatalog"/>), seed 1, by group.
    /// </summary>
    public static IReadOnlyList<GalleryEntry> CombatVfx() =>
    [
        .. EffectCatalog.All.Where(e => !e.Unused).OrderBy(e => e.Group).Select(e => new GalleryEntry($"vfx-{e.Prefix}",
            e.Group.ToString(), e.Name, null,
            $"Left: {e.Users[0].Name}; right: {e.EnemyArmy.Name}{(e.EnemyCasts ? ", casting it (mirrored)" : "")}. "
                + $"Frames {string.Join(", ", e.GridFrames)} of {e.Frames}.",
            g => VfxGrid(g, e))),
    ];

    private static CgLibrary? _vfxCgs;

    /// <summary>The strip above the grid, in pixels of the 1280×800 screen; the grid's cells are 640×256 under it.</summary>
    private const int VfxStrip = 32;

    /// <summary>
    /// <paramref name="effect"/>'s six frames, each on a stage of its own (<see cref="EffectStage"/>) shown at half the
    /// scene's size, its frame's number in its corner, under a strip naming the effect and its armies.
    /// </summary>
    private static Action<Rect2>? VfxGrid(GalleryStage g, EffectShow effect)
    {
        var cgs = _vfxCgs ??= GameData.Cgs();
        var font = new FontFile { Data = Godot.FileAccess.GetFileAsBytes(GodotGlyphRenderer.BizUdGothicPath) };
        var grid = g.Add(new Control { MouseFilter = Control.MouseFilterEnum.Ignore });
        var strip = new ColorRect { Color = new Color(0.08f, 0.07f, 0.06f), MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddChild(strip);
        var title = Caption(font, $"{effect.Name}  ·  {effect.Users[0].Name}{(effect.EnemyCasts ? " ← " : " → ")}{effect.EnemyArmy.Name}");
        grid.AddChild(title);
        var cells = new List<(TextureRect Picture, Label Number)>();
        foreach (int frame in effect.GridFrames)
        {
            var stage = new EffectStage();
            grid.AddChild(stage);                               // in the tree first: the effect draws only while shown
            stage.Show(cgs, effect, frame);
            var picture = new TextureRect
            {
                Texture = stage.GetTexture(),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            grid.AddChild(picture);
            var number = Caption(font, frame.ToString());
            grid.AddChild(number);
            cells.Add((picture, number));
        }
        return area =>
        {
            float k = area.Size.X / 1280f;
            GalleryStage.Fill(grid, area);
            strip.Position = Vector2.Zero;
            strip.Size = new Vector2(1280, VfxStrip) * k;
            title.Position = new Vector2(12, 5) * k;
            title.AddThemeFontSizeOverride("font_size", (int)Math.Round(18 * k));
            for (int i = 0; i < cells.Count; i++)
            {
                var at = new Vector2(i % 2 * 640, VfxStrip + i / 2 * 256) * k;
                cells[i].Picture.Position = at;
                cells[i].Picture.Size = new Vector2(640, 256) * k;
                cells[i].Number.Position = at + new Vector2(8, 4) * k;
                cells[i].Number.AddThemeFontSizeOverride("font_size", (int)Math.Round(20 * k));
                cells[i].Number.AddThemeConstantOverride("outline_size", Math.Max(1, (int)Math.Round(4 * k)));
            }
        };
    }

    private static Label Caption(Font font, string text)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", font);
        label.AddThemeColorOverride("font_color", new Color(0.93f, 0.89f, 0.80f));
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        return label;
    }
}
