using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// What an entry of the screen gallery builds its screen from (docs/ui/gallery.md, §2): a fresh state from a kept save,
/// read only, a presenter bound to it for what reads the scenario's text (no script runs), the game's font and the
/// pictures, and the stage its views go on.
/// </summary>
internal sealed class GalleryStage(Control host, GameFont font, FacePictures faces, GamePresenter presenter)
{
    private static readonly Dictionary<string, SaveData?> Saves = [];

    /// <summary>The game's font the views are built with (the gallery's switch).</summary>
    public GameFont Font { get; } = font;

    /// <summary>The pictures and faces our screens cut.</summary>
    public FacePictures Faces { get; } = faces;

    /// <summary>The kept saves missing, by name, as the entries asked for them.</summary>
    public HashSet<string> Missing { get; } = [];

    /// <summary>
    /// A fresh state from the kept save <paramref name="save"/> (<c>user://&lt;save&gt;/slot01.ksav</c>), or null when there
    /// is none. Read through <see cref="DirectorySaveStore"/>, never a <c>DirectorySaveLibrary</c>, which would move the
    /// kept folder's slot into <c>legacy/</c>.
    /// </summary>
    public GameState? State(string save)
    {
        if (!Saves.TryGetValue(save, out var data))
            Saves[save] = data = new DirectorySaveStore(ProjectSettings.GlobalizePath($"user://{save}")).Read(1);
        var state = new GameState();
        if (data is null || data.Variables.Length != state.Memory.VariableCount)
        {
            Missing.Add(save);
            return null;
        }
        data.Restore(state);
        return state;
    }

    /// <summary>The gallery's presenter bound to <paramref name="state"/>: the scenario's tables and texts for it.</summary>
    public GamePresenter Bound(GameState state)
    {
        presenter.Bind(state);
        return presenter;
    }

    /// <summary>
    /// The map as <c>COMMAND</c> shows it for <paramref name="state"/>, then <paramref name="over"/> drawn on it (a city's
    /// or a dungeon's picture, <c>WAO_SUB</c> 331), composed by the presenter and laid at the stage's bottom (docs/ui/
    /// gallery.md, §2): <c>M</c> 3 marks the markers changed, so <c>M</c> 0 draws the tiles and shows the view at the
    /// camera. Placed with the picture by <see cref="Fill"/>.
    /// </summary>
    public TextureRect Backdrop(GameState state, params ScriptEvent[] over)
    {
        var p = Bound(state);
        p.Show(new MapEvent(3));
        p.Show(new MapEvent(0));
        foreach (var e in over)
            p.Show(e);
        p.Compose();
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, p.Screen.ReadScreen());
        var backdrop = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        host.AddChild(backdrop);
        host.MoveChild(backdrop, 0);
        return backdrop;
    }

    /// <summary>Lays <paramref name="control"/> over the picture's <paramref name="area"/>.</summary>
    public static void Fill(Control control, Rect2 area)
    {
        control.Position = area.Position;
        control.Size = area.Size;
    }

    /// <summary>Adds <paramref name="view"/> to the stage, over what is there.</summary>
    public T Add<T>(T view) where T : Control
    {
        host.AddChild(view);
        return view;
    }
}
