using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Scripts;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The screen gallery (docs/ui/gallery.md; Debug → Screen Gallery…, <c>--screen-gallery [id,…]</c>): our screens on their
/// own, each entry a screen in one state built from a kept save without playing to it. The list at the left, the screen
/// at the right fitted at 1280×800, a bar switching the palette and the game's font for the gallery alone (the player's
/// choices stay). With <see cref="ScreenshotDirectory"/> it dumps every entry (or <see cref="Only"/>) as <c>&lt;id&gt;.png</c>
/// at 1280×800 with <c>manifest.json</c>, the list hidden, each picture compared with <see cref="PreviousDirectory"/>'s; a
/// window is needed, nothing moves the system pointer. <c>tools/ui/gallery.ps1</c> runs it and writes the index.
/// </summary>
public partial class GalleryScreen : Control
{
    /// <summary>The entries to dump, by id; null: all.</summary>
    public IReadOnlyList<string>? Only { get; init; }

    /// <summary>Where to dump the entries; null: the gallery is looked at.</summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>Quit after the dump.</summary>
    public bool QuitWhenDone { get; init; }

    /// <summary>A former dump each picture is compared with (<c>--previous DIR</c>): the manifest says which changed.</summary>
    public string? PreviousDirectory { get; init; }

    /// <summary>
    /// A picture counts as changed when more pixels than this differ from the former one by more than
    /// <see cref="Tolerance"/> in a channel: the click's triangle bobs, so a few hundred pixels move between two dumps.
    /// </summary>
    private const int ChangedPixels = 600;

    private const int Tolerance = 8;

    private readonly IReadOnlyList<GalleryEntry> _entries = GalleryEntries.All();
    private Control _side = null!;
    private ItemList _list = null!;
    private Label _note = null!;
    private Control _area = null!;
    private Control? _host;
    private Action<Rect2>? _place;
    private GalleryStage? _stage;
    private int _shown = -1;
    private GameFont _font = GodotGlyphRenderer.Chosen;
    private FacePictures _faces = null!;
    private GamePresenter _presenter = null!;
    private GodotGlyphRenderer _glyphs = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var background = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
        var row = new HBoxContainer();
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.AddThemeConstantOverride("separation", 0);
        AddChild(row);

        var side = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        side.AddThemeConstantOverride("separation", 8);
        _side = new MarginContainer();
        foreach (var edge in new[] { "left", "top", "right", "bottom" })
            _side.AddThemeConstantOverride($"margin_{edge}", 10);
        _side.AddChild(side);
        row.AddChild(_side);
        var back = new Button { Text = "← Title" };
        back.Pressed += () => Screens.ToTitle(this);
        side.AddChild(back);
        side.AddChild(new Label { Text = "Screen gallery (debug)" });
        _list = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill };
        string? section = null;
        foreach (var entry in _entries)
        {
            if (entry.Section != section)
            {
                section = entry.Section;
                int header = _list.AddItem($"— {section} —", selectable: false);
                _list.SetItemDisabled(header, true);
            }
            int index = _list.AddItem(entry.Title);
            _list.SetItemTooltip(index, entry.Id);
            _list.SetItemMetadata(index, _entries.ToList().IndexOf(entry));
        }
        _list.ItemSelected += index => Show((int)_list.GetItemMetadata((int)index));
        side.AddChild(_list);
        side.AddChild(Switch("Palette", UiPalette.All.Select(p => p.Title), UiPalette.All.ToList().IndexOf(UiPalettes.Chosen), i =>
        {
            UiPalettes.Use(UiPalette.All[i]);
            if (_host is not null)
                UiPalettes.Repaint(_host);
        }));
        var fonts = GodotGlyphRenderer.MsGothicInstalled ? new[] { GameFont.BizUdGothic, GameFont.MsGothic } : [GameFont.BizUdGothic];
        side.AddChild(Switch("Font", fonts.Select(f => f.ToString()), Math.Max(0, Array.IndexOf(fonts, _font)), i =>
        {
            _font = fonts[i];
            Show(_shown);
        }));
        _note = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(280, 0) };
        side.AddChild(_note);

        _area = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
        _area.Resized += Place;
        row.AddChild(_area);

        _faces = new FacePictures(GameData.Cgs());
        _glyphs = new GodotGlyphRenderer();
        _presenter = new GamePresenter(new ScenarioText(GameData.ScenarioDirectory), GameData.Cgs(), _glyphs,
            new ScenarioScripts(GameData.ScenarioDirectory));
        if (ScreenshotDirectory is not null)
            _ = DumpAsync(ScreenshotDirectory);
        else
            Show(0);
    }

    public override void _ExitTree() => _glyphs?.Dispose();

    private static HBoxContainer Switch(string label, IEnumerable<string> items, int selected, Action<int> chosen)
    {
        var box = new HBoxContainer();
        box.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(70, 0) });
        var button = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var item in items)
            button.AddItem(item);
        button.Selected = Math.Max(0, selected);
        button.ItemSelected += i => chosen((int)i);
        box.AddChild(button);
        return box;
    }

    /// <summary>Entry <paramref name="index"/> on the stage, built afresh; an error or a missing save shown in the note.</summary>
    private void Show(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;
        _host?.QueueFree();
        _host = new Control { MouseFilter = MouseFilterEnum.Pass };
        _host.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _area.AddChild(_host);
        _shown = index;
        var entry = _entries[index];
        _stage = new GalleryStage(_host, _font, _faces, _presenter);
        string note = entry.Note;
        try
        {
            _place = entry.Build(_stage);
            if (_place is null)
                note = $"The kept save user://{string.Join(", ", _stage.Missing)} is missing.";
        }
        catch (Exception e)
        {
            _place = null;
            note = $"Not shown: {e.Message}";
            GD.PushError($"Kichikuou: the screen gallery's {entry.Id}: {e}");
        }
        _note.Text = $"{entry.Id}{(entry.Save is { } save ? $" · {save}" : "")}\n{note}";
        Place();
    }

    /// <summary>The picture's area: the 1280×800 screen fitted into the stage, centred.</summary>
    private Rect2 PictureArea()
    {
        var size = _area.Size;
        float scale = Math.Min(size.X / 1280f, size.Y / 800f);
        var drawn = new Vector2(1280, 800) * scale;
        return new Rect2((size - drawn) / 2, drawn);
    }

    private void Place()
    {
        if (_place is not null && _area.Size.X > 0)
            _place(PictureArea());
    }

    // ---------------------------------------------------------------- the dump (docs/ui/gallery.md, §5)

    private sealed record DumpedEntry(string Id, string Section, string Title, string? Save, string Note, string? File, string? Sha256,
        string? Error, string? Since = null, int Differing = 0);

    /// <summary>
    /// The picture against the former dump's of the same name: "new" without one, else "changed" or "same" by
    /// <see cref="ChangedPixels"/>, and how many pixels differ.
    /// </summary>
    private (string? Since, int Differing) Compare(Image picture, string file)
    {
        if (PreviousDirectory is null)
            return (null, 0);
        string path = System.IO.Path.Combine(PreviousDirectory, file);
        if (!System.IO.File.Exists(path) || Image.LoadFromFile(path) is not { } before)
            return ("new", 0);
        if (before.GetSize() != picture.GetSize())
            return ("changed", picture.GetWidth() * picture.GetHeight());
        before.Convert(Image.Format.Rgba8);
        var now = (Image)picture.Duplicate();
        now.Convert(Image.Format.Rgba8);
        byte[] a = before.GetData(), b = now.GetData();
        int differing = 0;
        for (int i = 0; i < a.Length; i += 4)
        {
            if (Math.Abs(a[i] - b[i]) > Tolerance || Math.Abs(a[i + 1] - b[i + 1]) > Tolerance || Math.Abs(a[i + 2] - b[i + 2]) > Tolerance)
                differing++;
        }
        return (differing > ChangedPixels ? "changed" : "same", differing);
    }

    private async Task DumpAsync(string directory)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.PushError("Kichikuou: the screen gallery's dump needs a window (our views draw nothing headless).");
            GetTree().Quit(1);
            return;
        }
        _side.Visible = false;
        DisplayServer.WindowSetSize(new Vector2I(1280, 800));
        System.IO.Directory.CreateDirectory(directory);
        for (int i = 0; i < 4; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var dumped = new List<DumpedEntry>();
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (Only is not null && !Only.Contains(entry.Id))
                continue;
            Show(i);
            if (_place is null)
            {
                dumped.Add(new(entry.Id, entry.Section, entry.Title, entry.Save, entry.Note, null, null, _note.Text));
                continue;
            }
            for (int frame = 0; frame < 4; frame++)
                await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
            var image = GetViewport().GetTexture().GetImage();
            float k = image.GetWidth() / GetViewportRect().Size.X;          // the window's pixels a layout pixel
            var area = PictureArea();
            var at = _area.GlobalPosition + area.Position;
            var picture = image.GetRegion(new Rect2I((int)(at.X * k), (int)(at.Y * k), (int)(area.Size.X * k), (int)(area.Size.Y * k)));
            if (picture.GetWidth() != 1280 || picture.GetHeight() != 800)
                picture.Resize(1280, 800, Image.Interpolation.Lanczos);
            string file = $"{entry.Id}.png";
            string path = System.IO.Path.Combine(directory, file);
            picture.SavePng(path);
            string hash = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path))).ToLowerInvariant();
            var (since, differing) = Compare(picture, file);
            dumped.Add(new(entry.Id, entry.Section, entry.Title, entry.Save, entry.Note, file, hash, null, since, differing));
        }
        var manifest = new
        {
            Written = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Palette = UiPalettes.Chosen.Key,
            Font = _font.ToString(),
            Touch = TouchMode.On,
            Entries = dumped,
        };
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        GD.Print($"Kichikuou: the screen gallery dumped {dumped.Count(d => d.File is not null)} of {dumped.Count} entries into {directory}.");
        if (QuitWhenDone)
            GetTree().Quit(dumped.Any(d => d.Error is not null) ? 1 : 0);
    }
}
