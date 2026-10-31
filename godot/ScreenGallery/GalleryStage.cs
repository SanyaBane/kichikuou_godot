using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Screen;
using Kichikuou.Content.Scripts;
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
/// pictures, and the stage its views go on; a fresh presenter for a battle replayed into it (<see cref="GalleryBattle"/>).
/// </summary>
internal sealed class GalleryStage(Control host, GameFont font, FacePictures faces, GamePresenter presenter, Func<GamePresenter> fresh)
{
    /// <summary>A presenter of the entry's own, not bound yet: a battle replayed into it stays its.</summary>
    public GamePresenter Fresh() => fresh();

    /// <summary>Runs <paramref name="tick"/> every frame while the entry is shown (views that follow a presenter each frame).</summary>
    public void EachFrame(Action tick)
    {
        var tree = host.GetTree();
        Action run = () => tick();
        tree.ProcessFrame += run;
        host.TreeExiting += () => tree.ProcessFrame -= run;
    }

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

    /// <summary>The kept save <paramref name="save"/> as read, or null when there is none (<see cref="State"/>).</summary>
    public SaveData? Data(string save) => State(save) is null ? null : Saves[save];

    /// <summary>
    /// Runs <paramref name="then"/> two frames from now, once the views are laid out (a tree's cells have their places
    /// only after a draw), unless the entry was left meanwhile.
    /// </summary>
    public async void AfterLayout(Action then)
    {
        var tree = host.GetTree();
        for (int i = 0; i < 2; i++)
            await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (GodotObject.IsInstanceValid(host) && !host.IsQueuedForDeletion())
            then();
    }

    /// <summary>
    /// A left click (press and release) at <paramref name="global"/> given to <paramref name="view"/> as its input, the
    /// mouse moved there first: the view answers it as the player's, and the system pointer stays where it is.
    /// </summary>
    public static void Click(Control view, Vector2 global, MouseButton button = MouseButton.Left)
    {
        var at = view.GetGlobalTransformWithCanvas().AffineInverse() * global;
        view._GuiInput(new InputEventMouseMotion { Position = at, GlobalPosition = global });
        foreach (bool pressed in new[] { true, false })
            view._GuiInput(new InputEventMouseButton { ButtonIndex = button, Pressed = pressed, Position = at, GlobalPosition = global });
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
    /// camera (<see cref="Picture"/>).
    /// </summary>
    public TextureRect Backdrop(GameState state, params ScriptEvent[] over)
    {
        var p = Bound(state);
        p.Show(new MapEvent(3));
        p.Show(new MapEvent(0));
        foreach (var e in over)
            p.Show(e);
        return Picture(p);
    }

    /// <summary>
    /// <paramref name="p"/>'s screen as it composes it now (the original's drawing and what the presenter draws by hand
    /// over it, as the game's view shows it), laid at the stage's bottom; placed with the picture by <see cref="Fill"/>.
    /// </summary>
    public TextureRect Picture(GamePresenter p)
    {
        p.Compose();
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, p.Screen.ReadScreen());
        var picture = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        host.AddChild(picture);
        host.MoveChild(picture, 0);
        return picture;
    }

    private static ScenarioText? _texts;
    private static ScenarioScripts? _scripts;

    /// <summary>The text run at <paramref name="at"/> as printed now: its numbers as zeros, its string variables <paramref name="state"/>'s.</summary>
    public static string Text(ScriptRef at, GameState state) =>
        (_texts ??= new ScenarioText(GameData.ScenarioDirectory)).Read(at).Format(strings: i => state.Strings[i]);

    /// <summary>The text of <c>MS n, "text"</c> at <paramref name="at"/> in the player's files, as the script stores it.</summary>
    public static string Literal(ScriptRef at) =>
        ((_scripts ??= new ScenarioScripts(GameData.ScenarioDirectory)).ReadStatement(at) as SetStringStatement)?.Text
        ?? throw new InvalidOperationException($"{at}: expected MS n, \"text\".");

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

    /// <summary>
    /// Adds <paramref name="view"/> over the whole stage, outside the room an entry's screen margins leave (the margins'
    /// calibration, which the game puts over its whole screen); it goes with the entry.
    /// </summary>
    public T Over<T>(T view) where T : Control
    {
        host.GetParent().AddChild(view);
        host.TreeExiting += view.QueueFree;
        return view;
    }
}
