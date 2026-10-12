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
/// filled it) and every face CG in the player's files (each 192×256 CG). A tile is the square at the
/// size the party panel shows it in a 1280×800 window, its title yellow when the owner edited it; a
/// click shows the whole face twice as large with the square framed (<see cref="FaceCropEditor"/>:
/// drag it, size it with the wheel) and the square below as the panel will show it. Each edit is
/// saved at once (<see cref="FaceCropFile"/>); Reset drops it.
/// </summary>
internal sealed partial class PortraitViewer : Window
{
    /// <summary>A square's size on the screen in a 1280×800 window (the party panel's).</summary>
    private const int TileSize = 90;

    private readonly CgLibrary _cgs;
    private readonly IReadOnlyList<RecordFace> _units;
    private readonly IReadOnlyList<RecordFace> _commanders;
    private readonly FaceCrops _crops = FaceCropFile.Load();
    private readonly Dictionary<int, Image> _images = [];
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
        _cgs = cgs;
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
            0 => _units.Select(u => ($"{u.Record} {u.Name}", Named(u, "unit"), u.FaceCg)).ToList(),
            1 => _commanders.Select(c => ($"{c.Record} {c.Name}", Named(c, "commander"), c.FaceCg)).ToList(),
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
        button.Icon = Square(cg, _crops.CropOf(cg)) is { } square ? ImageTexture.CreateFromImage(square) : null;
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
            _about.Text = cg == 0 ? $"{about}\nNo face." : $"{about}\nFace CG {cg} is not in the player's files.";
            _editor.SetFace(null, default);
            _square.Texture = null;
            _reset.Disabled = true;
            return;
        }
        var crop = _crops.CropOf(cg);
        _editor.SetFace(ImageTexture.CreateFromImage(face), crop);
        Preview(crop);
    }

    /// <summary>The shown face's square as the panel will show it, and its description.</summary>
    private void Preview(FaceCrop crop)
    {
        if (_shown is not { } shown)
            return;
        var (about, cg) = shown;
        string state = crop != UnitFaces.CropOf(cg) ? " (edited)" : UnitFaces.Exceptions.ContainsKey(cg) ? " (an exception)" : "";
        _about.Text = $"{about}\nFace CG {cg}: square {Describe(crop)}{state}";
        _square.Texture = Square(cg, crop) is { } square ? ImageTexture.CreateFromImage(square) : null;
        _reset.Disabled = !_crops.IsEdited(cg) && crop == UnitFaces.CropOf(cg);
    }

    /// <summary>An edit of the shown face: kept, saved, and its tiles redrawn.</summary>
    private void Edited(FaceCrop crop)
    {
        if (_shown is not { } shown)
            return;
        int cg = shown.Cg;
        _crops.Set(cg, crop);
        FaceCropFile.Save(_crops);
        if (_tiles.TryGetValue(cg, out var tiles))
            tiles.ForEach(t => UpdateTile(t, cg));
        _count.Text = $"  {_grid.GetChildCount()} tiles; the usual square {Describe(UnitFaces.Default)}; {_crops.Edits.Count} edited";
        Preview(_crops.CropOf(cg));
    }

    /// <summary>Drops the shown face's edit.</summary>
    private void Reset()
    {
        if (_shown is not { } shown)
            return;
        var (about, cg) = shown;
        _crops.Reset(cg);
        Edited(UnitFaces.CropOf(cg));
        ShowFace(about, cg);
    }

    /// <summary>
    /// Development: saves the window with the units' list, Asuka's face (unit 11, the exception)
    /// shown, as <c>portraits-units.png</c> in <paramref name="dir"/>. The list of every face CG is
    /// never saved: some faces are adult pictures, which the agent's dumps leave out.
    /// </summary>
    public async Task SaveUnitsAsync(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        _list.Select(0);
        Fill();
        if (_units.Count > 10)
            ShowFace($"{_units[10].Name} (unit 11)", _units[10].FaceCg);
        for (int i = 0; i < 3; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTexture().GetImage().SavePng(System.IO.Path.Combine(dir, "portraits-units.png"));
    }

    private static string Describe(FaceCrop c) => $"({c.X}, {c.Y}) {c.Size}×{c.Size}";

    /// <summary>The units and commanders of the running game whose face is <paramref name="cg"/>, for the third list.</summary>
    private string UsedBy(int cg)
    {
        var names = _units.Where(u => u.FaceCg == cg).Select(u => $"{u.Name} (unit {u.Record})")
            .Concat(_commanders.Where(c => c.FaceCg == cg).Select(c => $"{c.Name} (commander {c.Record})"))
            .ToList();
        return names.Count switch
        {
            0 => "",
            <= 4 => ": " + string.Join(", ", names),
            _ => ": " + string.Join(", ", names.Take(4)) + $" and {names.Count - 4} more",
        };
    }

    /// <summary>The face CG as <c>G n, 40</c> draws it (index 40 transparent), or null.</summary>
    private Image? Face(int cg)
    {
        if (cg == 0)
            return null;
        if (_images.TryGetValue(cg, out var image))
            return image;
        if (_cgs.Get(cg) is not { } face)
            return null;
        image = Image.CreateFromData(face.Width, face.Height, false, Image.Format.Rgba8, face.ToRgba(40));
        _images[cg] = image;
        return image;
    }

    /// <summary>The face's <paramref name="crop"/> at <see cref="TileSize"/>, or null.</summary>
    private Image? Square(int cg, FaceCrop crop) =>
        Face(cg) is { } face ? Shrink(face.GetRegion(new Rect2I(crop.X, crop.Y, crop.Size, crop.Size)), TileSize) : null;

    /// <summary>
    /// <paramref name="image"/> resized to <paramref name="size"/>² without the colour key's magenta
    /// bleeding in: the transparent pixels' colour is dropped (premultiplied) before the filter and
    /// the alpha divided out after it.
    /// </summary>
    internal static Image Shrink(Image image, int size)
    {
        image.PremultiplyAlpha();
        image.Resize(size, size, Image.Interpolation.Lanczos);
        byte[] rgba = image.GetData();
        for (int i = 0; i < rgba.Length; i += 4)
        {
            int a = rgba[i + 3];
            if (a is 0 or 255)
                continue;
            for (int c = 0; c < 3; c++)
                rgba[i + c] = (byte)Math.Min(255, rgba[i + c] * 255 / a);
        }
        return Image.CreateFromData(size, size, false, Image.Format.Rgba8, rgba);
    }

    /// <summary>Every CG of a face's size in the player's files, read from the PNG headers.</summary>
    private List<int> AllFaces()
    {
        if (_allFaces is not null)
            return _allFaces;
        _allFaces = [];
        if (!System.IO.Directory.Exists(_cgs.Directory))
            return _allFaces;
        var header = new byte[24];
        foreach (string path in System.IO.Directory.EnumerateFiles(_cgs.Directory, "CG*.png").Order(StringComparer.Ordinal))
        {
            if (!int.TryParse(System.IO.Path.GetFileNameWithoutExtension(path)[2..], out int cg))
                continue;
            using (var file = System.IO.File.OpenRead(path))
                file.ReadExactly(header);
            int width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16));
            int height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20));
            if (width == UnitFaces.Width && height == UnitFaces.Height)
                _allFaces.Add(cg);
        }
        return _allFaces;
    }
}
