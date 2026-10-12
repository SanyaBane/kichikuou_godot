using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Portraits (ours, development): the faces our screens show, cut as <see cref="UnitFaces"/>
/// cuts them, for the owner to judge before they are used (docs/ui/deployment-table.md, third part).
/// Two lists: the units of the running game (their names and <c>Unit_FaceCg</c> as the state has
/// them now, so unit 48 once a scene has filled it) and every face CG in the player's files (each
/// 192×256 CG). A tile is the square at the size the party panel shows it in a 1280×800 window;
/// a click shows the whole face twice as large with the square framed.
/// </summary>
internal sealed partial class PortraitViewer : Window
{
    /// <summary>A square's size on the screen in a 1280×800 window (the party panel's).</summary>
    private const int TileSize = 90;

    private readonly CgLibrary _cgs;
    private readonly IReadOnlyList<UnitFace> _units;
    private readonly Dictionary<int, Image> _images = [];
    private List<int>? _allFaces;
    private OptionButton _list = null!;
    private GridContainer _grid = null!;
    private Label _count = null!;
    private TextureRect _whole = null!;
    private ReferenceRect _frame = null!;
    private TextureRect _square = null!;
    private Label _about = null!;

    /// <param name="cgs">The player's CGs.</param>
    /// <param name="state">The running game's state, or null before the boot (no units then).</param>
    public PortraitViewer(CgLibrary cgs, GameState? state)
    {
        _cgs = cgs;
        _units = state is null ? [] : UnitFaces.Units(state);
        Title = "Portraits (debug)";
        Size = new Vector2I(1240, 760);
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
        root.AddChild(left);

        var right = new VBoxContainer { CustomMinimumSize = new Vector2(UnitFaces.Width * 2, 0) };
        _about = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(UnitFaces.Width * 2, 0) };
        right.AddChild(_about);
        _whole = new TextureRect
        {
            CustomMinimumSize = new Vector2(UnitFaces.Width, UnitFaces.Height) * 2,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        _frame = new ReferenceRect { EditorOnly = false, BorderColor = Colors.Yellow, BorderWidth = 2, Visible = false };
        _whole.AddChild(_frame);
        right.AddChild(_whole);
        _square = new TextureRect { CustomMinimumSize = new Vector2(TileSize, TileSize), StretchMode = TextureRect.StretchModeEnum.Keep };
        right.AddChild(_square);
        root.AddChild(right);

        CloseRequested += QueueFree;
        Fill();
    }

    /// <summary>The tiles of the list chosen: (title, face CG) each.</summary>
    private void Fill()
    {
        foreach (var child in _grid.GetChildren())
            child.QueueFree();
        var tiles = _list.Selected == 0
            ? _units.Select(u =>($"{u.Unit} {(u.Name.Length > 0 ? u.Name : "(empty)")}", u.FaceCg)).ToList()
            : AllFaces().Select(cg => ($"CG {cg}{UsedBy(cg)}", cg)).ToList();
        _count.Text = _list.Selected == 0 && _units.Count == 0
            ? "  no game booted yet"
            : $"  {tiles.Count} faces; the usual square {Describe(UnitFaces.Default)}, {UnitFaces.Exceptions.Count} exception(s)";
        foreach (var (title, cg) in tiles)
            _grid.AddChild(Tile(title, cg));
    }

    private Button Tile(string title, int cg)
    {
        var button = new Button
        {
            Text = title,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            CustomMinimumSize = new Vector2(TileSize + 24, TileSize + 34),
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Top,
            Flat = true,
        };
        if (Square(cg) is { } square)
            button.Icon = ImageTexture.CreateFromImage(square);
        button.Pressed += () => ShowFace(title, cg);
        return button;
    }

    /// <summary>The whole face twice as large with its square framed, and the square at the panel's size.</summary>
    private void ShowFace(string title, int cg)
    {
        var crop = UnitFaces.CropOf(cg);
        bool exception = UnitFaces.Exceptions.ContainsKey(cg);
        _about.Text = $"{title}\nFace CG {cg}: square {Describe(crop)}{(exception ? " (an exception)" : "")}";
        if (Face(cg) is not { } face)
        {
            _whole.Texture = null;
            _square.Texture = null;
            _frame.Visible = false;
            _about.Text += "\nNo such CG in the player's files.";
            return;
        }
        _whole.Texture = ImageTexture.CreateFromImage(face);
        _frame.Position = new Vector2(crop.X, crop.Y) * 2;
        _frame.Size = new Vector2(crop.Size, crop.Size) * 2;
        _frame.Visible = true;
        _square.Texture = Square(cg) is { } square ? ImageTexture.CreateFromImage(square) : null;
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
            ShowFace($"11 {_units[10].Name}", _units[10].FaceCg);
        for (int i = 0; i < 3; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTexture().GetImage().SavePng(System.IO.Path.Combine(dir, "portraits-units.png"));
    }

    private static string Describe(FaceCrop c) => $"({c.X}, {c.Y}) {c.Size}×{c.Size}";

    /// <summary>The units of the running game whose face is <paramref name="cg"/>, for the second list.</summary>
    private string UsedBy(int cg)
    {
        var names = _units.Where(u => u.FaceCg == cg).Select(u => u.Name.Length > 0 ? u.Name : $"unit {u.Unit}").ToList();
        return names.Count == 0 ? "" : " " + string.Join(", ", names);
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

    /// <summary>The face's square at <see cref="TileSize"/>, or null.</summary>
    private Image? Square(int cg)
    {
        if (Face(cg) is not { } face)
            return null;
        var crop = UnitFaces.CropOf(cg);
        return Shrink(face.GetRegion(new Rect2I(crop.X, crop.Y, crop.Size, crop.Size)), TileSize);
    }

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
