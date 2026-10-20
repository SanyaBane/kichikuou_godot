using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Portraits (ours, development): the faces our screens show, cut as <see cref="UnitFaces"/>
/// cuts them, for the owner to judge and set before they are used (docs/ui/deployment-table.md,
/// third part). Three lists: the units and the commanders of the running game (their names and
/// <c>Unit_FaceCg</c> / <c>Cmdr_FaceCg</c> as the state has them now, so unit 48 once a scene has
/// filled it; a commander without a face has its battle sprite, <see cref="RecordFace.StandIn"/>)
/// and every face CG in the player's files (each 192×256 CG). A tile is the square at the
/// size the party panel shows it in a 1280×800 window, its title yellow when the owner edited it; a
/// click shows the whole face twice as large with the square framed (<see cref="FaceCropEditor"/>:
/// drag it, size it with the wheel) and the square below as the panel will show it. Each edit is
/// saved at once (<see cref="FaceCropFile"/>); Reset drops it.
/// </summary>
internal sealed partial class PortraitViewer : Window
{
    /// <summary>A square's size on the screen in a 1280×800 window (the party panel's).</summary>
    private const int TileSize = 90;

    private readonly FacePictures _pictures;
    private readonly IReadOnlyList<RecordFace> _units;
    private readonly IReadOnlyList<RecordFace> _commanders;
    private readonly FaceCrops _crops = FaceCropFile.Load();
    private readonly Dictionary<int, List<Button>> _tiles = [];      // the tiles shown, by face CG
    private List<int>? _allFaces;
    private OptionButton _list = null!;
    private GridContainer _grid = null!;
    private Label _count = null!;
    private Label _about = null!;
    private FaceCropEditor _editor = null!;
    private TextureRect _square = null!;
    private Button _reset = null!;
    private Label _file = null!;
    private (string Title, int Cg)? _shown;

    /// <param name="cgs">The player's CGs.</param>
    /// <param name="state">The running game's state, or null before the boot (no units or commanders then).</param>
    public PortraitViewer(CgLibrary cgs, GameState? state)
    {
        _pictures = new FacePictures(cgs);
        _units = state is null ? [] : UnitFaces.Units(state);
        _commanders = state is null ? [] : UnitFaces.Commanders(state);
        Title = "Portraits (debug)";
        Size = new Vector2I(1240, 800);
        Exclusive = true;
        Transient = true;
    }

    public override void _Ready()
    {
        var background = new Panel();
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);
        var root = new HBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 12);
        root.AddThemeConstantOverride("separation", 16);
        AddChild(root);

        var left = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var header = new HBoxContainer();
        _list = new OptionButton();
        _list.AddItem("Units of this game");
        _list.AddItem("Commanders of this game");
        _list.AddItem("Every face CG (192×256)");
        _list.ItemSelected += _ => Fill();
        header.AddChild(_list);
        _count = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        header.AddChild(_count);
        left.AddChild(header);
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _grid = new GridContainer { Columns = 7 };
        _grid.AddThemeConstantOverride("h_separation", 6);
        _grid.AddThemeConstantOverride("v_separation", 6);
        scroll.AddChild(_grid);
        left.AddChild(scroll);
        _file = new Label { Text = $"Edits are saved in {FaceCropFile.GlobalPath}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        left.AddChild(_file);
        root.AddChild(left);

        var right = new VBoxContainer { CustomMinimumSize = new Vector2(UnitFaces.Width * 2, 0) };
        _about = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(UnitFaces.Width * 2, 0) };
        right.AddChild(_about);
        _editor = new FaceCropEditor();
        _editor.Changed += crop => Preview(crop);
        _editor.Finished += Edited;
        right.AddChild(_editor);
        var below = new HBoxContainer();
        _square = new TextureRect { CustomMinimumSize = new Vector2(TileSize, TileSize), StretchMode = TextureRect.StretchModeEnum.Keep };
        below.AddChild(_square);
        var notes = new VBoxContainer();
        notes.AddChild(new Label { Text = "Drag the square; the wheel sizes it.\nLeft: as the panel will show it." });
        _reset = new Button { Text = "Reset this face", Disabled = true, FocusMode = Control.FocusModeEnum.None };
        _reset.Pressed += Reset;
        notes.AddChild(_reset);
        below.AddChild(notes);
        right.AddChild(below);
        root.AddChild(right);

        CloseRequested += QueueFree;
        Fill();
    }

    /// <summary>The tiles of the list chosen.</summary>
    private void Fill()
    {
        foreach (var child in _grid.GetChildren())
            child.QueueFree();
        _tiles.Clear();
        static string Named(RecordFace f, string kind) =>
            $"{(f.Name.Length > 0 ? f.Name : "(empty)")} ({kind} {f.Record})";
        var tiles = _list.Selected switch
        {
            0 => _units.Select(u => ($"{u.Record} {u.Name}", Named(u, "unit"), u.Picture)).ToList(),
            1 => _commanders.Select(c => c.StandIn == 0
                ? ($"{c.Record} {c.Name}", Named(c, "commander"), c.Picture)
                : ($"{c.Record} {c.Name} (sprite)", $"{Named(c, "commander")}: no face, its battle sprite stands in", c.Picture)).ToList(),
            _ => AllFaces().Select(cg => ($"CG {cg}", $"CG {cg}{UsedBy(cg)}", cg)).ToList(),
        };
        _count.Text = _list.Selected < 2 && _units.Count == 0
            ? "  no game booted yet"
            : $"  {tiles.Count} tiles; the usual square {Describe(UnitFaces.Default)}; {_crops.Edits.Count} edited";
        foreach (var (title, about, cg) in tiles)
            _grid.AddChild(Tile(title, about, cg));
    }

    private Button Tile(string title, string about, int cg)
    {
        var button = new Button
        {
            Text = cg == 0 ? $"{title} (no face)" : title,
            TooltipText = about,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            CustomMinimumSize = new Vector2(TileSize + 24, TileSize + 34),
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Top,
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.Pressed += () => ShowFace(about, cg);
        if (!_tiles.TryGetValue(cg, out var list))
            _tiles[cg] = list = [];
        list.Add(button);
        UpdateTile(button, cg);
        return button;
    }

    private void UpdateTile(Button button, int cg)
    {
        button.Icon = Square(cg, CropOf(cg)) is { } square ? ImageTexture.CreateFromImage(square) : null;
        if (_crops.IsEdited(cg))
            button.AddThemeColorOverride("font_color", Colors.Yellow);
        else
            button.RemoveThemeColorOverride("font_color");
    }

    /// <summary>The whole face twice as large with its square framed, and the square at the panel's size.</summary>
    private void ShowFace(string about, int cg)
    {
        _shown = (about, cg);
        if (Face(cg) is not { } face)
        {
            _about.Text = cg == 0 ? $"{about}\nNo picture." : $"{about}\nFace CG {cg} is not in the player's files.";
            _editor.SetPicture(null, default);
            _square.Texture = null;
            _reset.Disabled = true;
            return;
        }
        var crop = CropOf(cg);
        _editor.SetPicture(ImageTexture.CreateFromImage(face), crop);
        Preview(crop);
    }

    /// <summary>The shown face's square as the panel will show it, and its description.</summary>
    private void Preview(FaceCrop crop)
    {
        if (_shown is not { } shown)
            return;
        var (about, cg) = shown;
        string state = crop != BuiltIn(cg) ? " (edited)" : UnitFaces.Exceptions.ContainsKey(cg) ? " (an exception)" : "";
        string kind = SizeOf(cg) == (UnitFaces.Width, UnitFaces.Height) ? "Face" : "Battle sprite";
        _about.Text = $"{about}\n{kind} CG {cg}: square {Describe(crop)}{state}";
        _square.Texture = Square(cg, crop) is { } square ? ImageTexture.CreateFromImage(square) : null;
        _reset.Disabled = !_crops.IsEdited(cg) && crop == BuiltIn(cg);
    }

    /// <summary>An edit of the shown face: kept, saved, and its tiles redrawn.</summary>
    private void Edited(FaceCrop crop)
    {
        if (_shown is not { } shown)
            return;
        int cg = shown.Cg;
        var (width, height) = SizeOf(cg);
        _crops.Set(cg, crop, width, height);
        FaceCropFile.Save(_crops);
        if (_tiles.TryGetValue(cg, out var tiles))
            tiles.ForEach(t => UpdateTile(t, cg));
        _count.Text = $"  {_grid.GetChildCount()} tiles; the usual square {Describe(UnitFaces.Default)}; {_crops.Edits.Count} edited";
        Preview(CropOf(cg));
    }

    /// <summary>Drops the shown face's edit.</summary>
    private void Reset()
    {
        if (_shown is not { } shown)
            return;
        var (about, cg) = shown;
        _crops.Reset(cg);
        Edited(BuiltIn(cg));
        ShowFace(about, cg);
    }

    /// <summary>
    /// Development: saves the window with the units' list, Asuka's face (unit 11, the exception)
    /// shown, as <c>portraits-units.png</c> in <paramref name="dir"/>, then with the first stand-in
    /// shown (Aladdin's sprite, commander 21) as <c>portraits-stand-in.png</c>. The other lists
    /// are never saved: some faces are adult pictures, which the agent's dumps leave out.
    /// </summary>
    public async Task SaveUnitsAsync(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        _list.Select(0);
        Fill();
        if (_units.Count > 10)
            ShowFace($"{_units[10].Name} (unit 11)", _units[10].FaceCg);
        await SaveAsync(System.IO.Path.Combine(dir, "portraits-units.png"));
        if (_commanders.FirstOrDefault(c => c.StandIn != 0) is { } standIn)
        {
            ShowFace($"{standIn.Name} (commander {standIn.Record}): no face, its battle sprite stands in", standIn.Picture);
            await SaveAsync(System.IO.Path.Combine(dir, "portraits-stand-in.png"));
        }
    }

    private async Task SaveAsync(string path)
    {
        for (int i = 0; i < 3; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTexture().GetImage().SavePng(path);
    }

    private static string Describe(FaceCrop c) => $"({c.X}, {c.Y}) {c.Size}×{c.Size}";

    private (int Width, int Height) SizeOf(int cg) => _pictures.SizeOf(cg);

    /// <summary>The square in use for CG <paramref name="cg"/>: the owner's edit, else the built-in one.</summary>
    private FaceCrop CropOf(int cg) => _pictures.CropOf(cg, _crops);

    /// <summary>The built-in square for CG <paramref name="cg"/> (<see cref="UnitFaces.CropOf"/>).</summary>
    private FaceCrop BuiltIn(int cg)
    {
        var (width, height) = SizeOf(cg);
        return UnitFaces.CropOf(cg, width, height);
    }

    /// <summary>The units and commanders of the running game whose face is <paramref name="cg"/>, for the third list.</summary>
    private string UsedBy(int cg)
    {
        var names = _units.Where(u => u.FaceCg == cg).Select(u => $"{u.Name} (unit {u.Record})")
            .Concat(_commanders.Where(c => c.Picture == cg).Select(c => $"{c.Name} (commander {c.Record})"))
            .ToList();
        return names.Count switch
        {
            0 => "",
            <= 4 => ": " + string.Join(", ", names),
            _ => ": " + string.Join(", ", names.Take(4)) + $" and {names.Count - 4} more",
        };
    }

    private Image? Face(int cg) => _pictures.Picture(cg);

    /// <summary>The <paramref name="crop"/> of CG <paramref name="cg"/> at <see cref="TileSize"/>, or null.</summary>
    private Image? Square(int cg, FaceCrop crop) => _pictures.Square(cg, crop, TileSize);

    /// <summary>Every CG of a face's size in the player's files, read from the CGs' headers.</summary>
    private List<int> AllFaces() =>
        _allFaces ??= [.. _pictures.Cgs.Numbers.Where(cg => _pictures.Cgs.SizeOf(cg) == (UnitFaces.Width, UnitFaces.Height))];
}
