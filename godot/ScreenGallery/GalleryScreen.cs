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
/// at the right fitted at 1280×800 (on a phone in its frame, <see cref="ScreenFrame.In"/>), a bar switching the palette
/// and the game's font for the gallery alone (the player's choices stay). With <see cref="ScreenshotDirectory"/> it dumps
/// every entry (or <see cref="Only"/>) as <c>&lt;id&gt;.png</c> at 1280×800 with <c>manifest.json</c>, the list hidden, each
/// picture compared with <see cref="PreviousDirectory"/>'s; on a phone (<c>--phone landscape</c>) the whole window at
/// <see cref="WindowSize"/>, its black sides with it. A window is needed, nothing moves the system pointer.
/// <c>tools/ui/gallery.ps1</c> runs it and writes the index.
/// </summary>
public partial class GalleryScreen : Control
{
    /// <summary>The entries to dump, by id, written into the former dump in the directory (<see cref="Merged"/>); null: all.</summary>
    public IReadOnlyList<string>? Only { get; init; }

    /// <summary>Where to dump the entries; null: the gallery is looked at.</summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>Quit after the dump.</summary>
    public bool QuitWhenDone { get; init; }

    /// <summary>A former dump each picture is compared with (<c>--previous DIR</c>): the manifest says which changed.</summary>
    public string? PreviousDirectory { get; init; }

    /// <summary>The window's size while dumping (<c>--window WxH</c>; docs/ui/phone-build.md, §3).</summary>
    public Vector2I WindowSize { get; init; } = new(1280, 800);

    /// <summary>
    /// A picture counts as changed when more pixels than this differ from the former one by more than
    /// <see cref="Tolerance"/> in a channel: the click's triangle bobs, so a few hundred pixels move between two dumps.
    /// </summary>
    private const int ChangedPixels = 600;

    private const int Tolerance = 8;

    /// <summary>Where the views' clock stands as each entry is dumped (<see cref="UiClock.Frames"/>): an hour in.</summary>
    private const ulong ClockStart = 3_600_000;

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
        _stage = new GalleryStage(_host, _font, _faces, _presenter, () => new GamePresenter(
            new ScenarioText(GameData.ScenarioDirectory), GameData.Cgs(), _glyphs, new ScenarioScripts(GameData.ScenarioDirectory)));
        string note = entry.Note;
        try
        {
            _place = entry.Build(_stage);
            if (_place is null)
                note = $"The kept save user://{string.Join(", ", _stage.Missing)} is missing (docs/ui/gallery.md, §2, makes it).";
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

    /// <summary>The picture's area: the 1280×800 screen fitted into the stage and centred, or the phone's frame.</summary>
    private Rect2 PictureArea() => ScreenFrame.In(new Rect2(Vector2.Zero, _area.Size)).Area;

    private void Place()
    {
        if (_place is not null && _area.Size.X > 0)
            _place(PictureArea());
    }

    // ---------------------------------------------------------------- the dump (docs/ui/gallery.md, §5)

    private sealed record DumpedEntry(string Id, string Section, string Title, string? Save, string Note, string? File, string? Sha256,
        string? Error, string? Since = null, int Differing = 0, int[]? Box = null);

    private sealed record Manifest(List<DumpedEntry> Entries);

    private static readonly JsonSerializerOptions ManifestJson =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// A dump of <see cref="Only"/> some entries written into the pass's former dump (<paramref name="manifestPath"/>, its
    /// pictures left in place): the entries in the gallery's order, the dumped ones new, the others as the former dump
    /// has them, a change found by an earlier partial dump still marked (its former picture stays in the previous
    /// folder, <c>gallery.ps1</c>), so several partial dumps show every change until a full one; an entry the gallery no
    /// longer has is left out.
    /// </summary>
    private List<DumpedEntry> Merged(string manifestPath, List<DumpedEntry> dumped)
    {
        if (!System.IO.File.Exists(manifestPath))
            return dumped;
        var former = JsonSerializer.Deserialize<Manifest>(System.IO.File.ReadAllText(manifestPath), ManifestJson)?.Entries ?? [];
        var merged = new List<DumpedEntry>();
        foreach (var entry in _entries)
        {
            if (dumped.Find(d => d.Id == entry.Id) is { } now)
                merged.Add(now);
            else if (former.Find(d => d.Id == entry.Id) is { } kept)
                merged.Add(kept);
        }
        return merged;
    }

    /// <summary>
    /// The picture against the former dump's of the same name: "new" without one, else "changed" or "same" by
    /// <see cref="ChangedPixels"/>, how many pixels differ and the box round them (left, top, right, bottom; null: none).
    /// </summary>
    private (string? Since, int Differing, int[]? Box) Compare(Image picture, string file)
    {
        if (PreviousDirectory is null)
            return (null, 0, null);
        string path = System.IO.Path.Combine(PreviousDirectory, file);
        if (!System.IO.File.Exists(path) || Image.LoadFromFile(path) is not { } before)
            return ("new", 0, null);
        if (before.GetSize() != picture.GetSize())
            return ("changed", picture.GetWidth() * picture.GetHeight(), null);
        before.Convert(Image.Format.Rgba8);
        var now = (Image)picture.Duplicate();
        now.Convert(Image.Format.Rgba8);
        byte[] a = before.GetData(), b = now.GetData();
        int differing = 0, width = picture.GetWidth();
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        for (int i = 0; i < a.Length; i += 4)
        {
            if (Math.Abs(a[i] - b[i]) <= Tolerance && Math.Abs(a[i + 1] - b[i + 1]) <= Tolerance && Math.Abs(a[i + 2] - b[i + 2]) <= Tolerance)
                continue;
            differing++;
            int x = i / 4 % width, y = i / 4 / width;
            (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        }
        return (differing > ChangedPixels ? "changed" : "same", differing, right < 0 ? null : [left, top, right, bottom]);
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
        // The real pointer over the window would move or drop a hover an entry set (2026-10-25: treasure-hover lost
        // its cell); the entries' clicks go to the views' _GuiInput directly, so the window takes no input while dumping.
        GetViewport().GuiDisableInput = true;
        DisplayServer.WindowSetSize(WindowSize);
        System.IO.Directory.CreateDirectory(directory);
        for (int i = 0; i < 4; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var dumped = new List<DumpedEntry>();
        var tree = GetTree();
        Action step = UiClock.Step;
        tree.ProcessFrame += step;                              // the views' clock goes a frame at a time while dumping
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (Only is not null && !Only.Contains(entry.Id))
                continue;
            UiClock.Frames(ClockStart);
            Show(i);
            if (_place is null)
            {
                dumped.Add(new(entry.Id, entry.Section, entry.Title, entry.Save, entry.Note, null, null, _note.Text));
                continue;
            }
            for (int frame = 0; frame < entry.Settle * 60; frame++)
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            for (int frame = 0; frame < 4; frame++)
                await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
            var picture = GetViewport().GetTexture().GetImage();
            if (!Device.Phone)
            {
                float k = picture.GetWidth() / GetViewportRect().Size.X;        // the window's pixels a layout pixel
                var area = PictureArea();
                var at = _area.GlobalPosition + area.Position;
                picture = picture.GetRegion(new Rect2I((int)(at.X * k), (int)(at.Y * k), (int)(area.Size.X * k), (int)(area.Size.Y * k)));
                if (picture.GetWidth() != 1280 || picture.GetHeight() != 800)
                    picture.Resize(1280, 800, Image.Interpolation.Lanczos);
            }
            string file = $"{entry.Id}.png";
            string path = System.IO.Path.Combine(directory, file);
            picture.SavePng(path);
            string hash = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path))).ToLowerInvariant();
            var (since, differing, box) = Compare(picture, file);
            dumped.Add(new(entry.Id, entry.Section, entry.Title, entry.Save, entry.Note, file, hash, null, since, differing, box));
        }
        tree.ProcessFrame -= step;
        UiClock.Real();
        string manifestPath = System.IO.Path.Combine(directory, "manifest.json");
        var entries = Only is null ? dumped : Merged(manifestPath, dumped);
        var manifest = new
        {
            Written = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Palette = UiPalettes.Chosen.Key,
            Font = _font.ToString(),
            Touch = TouchMode.On,
            Device = Device.Layout.ToString(),
            Entries = entries,
        };
        System.IO.File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestJson));
        GD.Print($"Kichikuou: the screen gallery dumped {dumped.Count(d => d.File is not null)} of {dumped.Count} entries into {directory}"
            + (entries == dumped ? "." : $", {entries.Count} in its manifest."));
        if (QuitWhenDone)
            GetTree().Quit(dumped.Any(d => d.Error is not null) ? 1 : 0);
    }
}
