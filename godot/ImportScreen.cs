using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Decompiler;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game;

/// <summary>
/// The import screen (docs/import.md, step 8; the owner's mock-up B, 2026-10-20): what the game shows at its start
/// when it has no data yet — a paper sheet in a lacquer frame on our wave backdrop, the steps written on it as a
/// numbered list (一 choose the game folder, 二 the files checked and the scenario read, 三 play), the current one
/// sealed in red. The folder field opens the system's folder dialog; Import runs <see cref="GameImport"/> on a
/// worker thread under a vermilion bar; a refusal (<see cref="GameFolderException"/>'s message) is written in
/// vermilion ink with a stamp beside it and "Choose another folder". Once imported, the folder is kept
/// (<see cref="GameData.GameDirectory"/>) and <see cref="_next"/> opens. Opened from Option → Game it has a
/// way back (<see cref="_back"/>: "Back to the title", Esc or Android's Back) that leaves the open import as it is. Laid
/// out in the 1280×800 screen's pixels, letterboxed into the window; on the phone held landscape as LI (ImportScreen.Phone.cs).
/// </summary>
public partial class ImportScreen : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Settings);

    private enum Phase { Choose, Importing, Done, Refused }

    private static readonly Rect2 Frame = new(250, 110, 780, 580);
    private static readonly Rect2 Paper = new(276, 136, 728, 528);
    private static readonly Rect2 WideButton = new(500, 588, 280, 48), NarrowButton = new(540, 588, 200, 48);
    // the way back right of the red button: a way back stands right of the action (owner, 2026-10-30)
    private static readonly Rect2 BackLink = new(TextRight - 170, 600, 170, 24);
    private const float StepX = 316, TextX = 364, TextRight = 964;

    private static Color FieldFill => Ui.Field;
    private static Color Good => Ui.BarGood;

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontVariation _cinzelBold = Lacquer.Cinzel(800);
    private readonly FontFile _gothic;
    private readonly Func<Node> _next;
    private readonly Func<Node>? _back;
    private readonly string? _unattended;
    private readonly string? _shots;
    private readonly bool _quit;

    private Phase _phase = Phase.Choose;
    private string _folder;
    private string _refusal = "", _hashes = "";
    private readonly object _lock = new();
    private ImportProgress _progress = new("Checking the game files", 0);
    private ImportedGame? _imported;
    private Exception? _failure;
    private bool _finished;
    private double _doneFor;
    private Rect2 _field;                          // the folder field, in screen pixels (laid out by the drawing)
    private bool _hoverField, _hoverButton, _pressedButton, _hoverBack;
    private FileDialog? _dialog;
    private bool _dialogOpen;                      // the native dialog leaves the node's Visible false and takes no input away
    private float _k = 1;
    private Vector2 _origin;
    private int _shot;
    private bool _progressShot;
    private bool _held;                            // the screen gallery's: one phase drawn, nothing run (Held)

    /// <summary>
    /// The screen for the saved game folder <paramref name="folder"/> (empty: none yet); with <paramref name="start"/>
    /// it imports that folder at once. <paramref name="next"/> makes the screen that follows the import.
    /// <paramref name="unattended"/> (the development switch <c>--import-screen DIR</c>) imports DIR without the dialog,
    /// saving the screen at each phase into <paramref name="shots"/>, and quits at the end with <paramref name="quit"/>.
    /// <paramref name="back"/>, when given, makes the screen that a way back without an import opens. Its text is in
    /// <paramref name="font"/>, the player's choice unless given.
    /// </summary>
    public ImportScreen(string folder, bool start, Func<Node> next, string? unattended = null, string? shots = null, bool quit = false,
        Func<Node>? back = null, GameFont? font = null)
    {
        _gothic = DeploymentTableView.LoadFont(font ?? GodotGlyphRenderer.Chosen);
        _folder = unattended ?? folder;
        _next = next;
        _back = back;
        _unattended = unattended;
        _shots = shots;
        _quit = quit;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (start && _folder.Length > 0 && unattended is null)
            Callable.From(StartImport).CallDeferred();
    }

    /// <summary>
    /// The screen held in one phase for the screen gallery (docs/ui/gallery.md): <paramref name="folder"/> chosen (empty:
    /// none yet), importing at <paramref name="importing"/>, imported (<paramref name="done"/>) or refused with
    /// <paramref name="refusal"/> (a <see cref="GameFolderException"/>'s message, its hashes on the small line), with the
    /// way back of Option → Game (<paramref name="back"/>) — drawn as an import draws that phase, in <paramref name="font"/>.
    /// No import runs, it takes no input and opens nothing; the gallery places it (anchored at the top left, not the full rect).
    /// </summary>
    internal static ImportScreen Held(GameFont font, string folder, ImportProgress? importing = null, bool done = false,
        string? refusal = null, bool back = false)
    {
        static Node Never() => throw new InvalidOperationException("The gallery's import screen opens nothing.");
        var screen = new ImportScreen(folder, start: false, next: Never, back: back ? Never : null, font: font)
        {
            _held = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        screen.SetAnchorsPreset(LayoutPreset.TopLeft);
        if (refusal is not null)
        {
            screen._phase = Phase.Refused;
            (screen._refusal, screen._hashes) = SplitHashes(refusal);
        }
        else if (done)
        {
            screen._phase = Phase.Done;
        }
        else if (importing is { } progress)
        {
            screen._phase = Phase.Importing;
            screen._progress = progress;
        }
        return screen;
    }

    public override void _Ready()
    {
        Resized += Layout;
        Layout();
        if (_unattended is not null)
            _ = UnattendedAsync();
    }

    private void Layout()
    {
        if (PlacePhone())
            return;
        var frame = ScreenFrame.In(new Rect2(Vector2.Zero, Size));
        _k = frame.Scale <= 0 ? 1 : frame.Scale;
        _origin = frame.Scale <= 0 ? (Size - new Vector2(1280, 800)) / 2 : frame.Origin;
        QueueRedraw();
    }

    // ---------------------------------------------------------------- the import

    private void StartImport()
    {
        if (_folder.Length == 0 || _phase == Phase.Importing)
            return;
        _phase = Phase.Importing;
        lock (_lock)
        {
            _progress = new ImportProgress("Checking the game files", 0);
            _finished = false;
            _imported = null;
            _failure = null;
        }
        string folder = _folder, cache = _unattended is not null ? LookCache() : GameData.ImportCache;
        Task.Run(() =>
        {
            ImportedGame? imported = null;
            Exception? failure = null;
            try
            {
                imported = GameImport.Import(folder, cache, new Progress(p => { lock (_lock) _progress = p; }));
            }
            catch (Exception e)
            {
                failure = e;
            }
            lock (_lock)
            {
                _imported = imported;
                _failure = failure;
                _finished = true;
            }
        });
        QueueRedraw();
    }

    /// <summary>A fresh cache for the unattended look, so its decompile runs and its bar is seen (our own folder, emptied first).</summary>
    private static string LookCache()
    {
        string path = ProjectSettings.GlobalizePath("user://import-look");
        if (System.IO.Directory.Exists(path))
            System.IO.Directory.Delete(path, recursive: true);
        return path;
    }

    public override void _Process(double delta)
    {
        if (_held)
            return;
        if (_phase == Phase.Done)
        {
            _doneFor += delta;
            if (_doneFor >= 0.5 && _unattended is null)
                Screens.Open(this, _next());
            return;
        }
        if (_phase != Phase.Importing)
            return;
        ImportedGame? imported;
        Exception? failure;
        bool finished;
        lock (_lock)
        {
            imported = _imported;
            failure = _failure;
            finished = _finished;
        }
        QueueRedraw();
        if (!finished)
            return;
        if (imported is not null)
        {
            GameData.Imported = imported;
            if (_unattended is null)
                GameData.GameDirectory = imported.Folder.Directory;
            _phase = Phase.Done;
            _doneFor = 0;
            return;
        }
        _phase = Phase.Refused;
        (_refusal, _hashes) = failure switch
        {
            GameFolderException e => SplitHashes(e.Message),
            DecompileException e => ($"The scenario in {_folder} could not be read: {e.Message}", ""),
            _ => ($"The folder {_folder} could not be imported: {failure?.Message}", ""),
        };
        GD.PrintErr($"Import refused: {_refusal} {_hashes}");
    }

    /// <summary>The check's message without its hashes, and the hashes for the small line under it.</summary>
    private static (string Message, string Hashes) SplitHashes(string message)
    {
        int at = message.IndexOf(" (its SHA-256 is ", StringComparison.Ordinal);
        return at < 0 ? (message, "") : (message[..at] + ".", message[(at + 2)..].TrimEnd('.', ')'));
    }

    private sealed class Progress(Action<ImportProgress> report) : IProgress<ImportProgress>
    {
        public void Report(ImportProgress value) => report(value);
    }

    // ---------------------------------------------------------------- the folder dialog

    private void ChooseFolder()
    {
        if (_dialogOpen)
            return;
        if (_dialog is null)
        {
            _dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.OpenDir,
                Access = FileDialog.AccessEnum.Filesystem,
                UseNativeDialog = true,
                Title = "Choose the folder of " + GameFiles.EnglishTranslation10.Name,
            };
            _dialog.DirSelected += dir =>
            {
                _dialogOpen = false;
                _folder = dir;
                _phase = Phase.Choose;
                QueueRedraw();
            };
            _dialog.Canceled += () =>
            {
                _dialogOpen = false;
                QueueRedraw();
            };
            AddChild(_dialog);
        }
        if (_folder.Length > 0 && System.IO.Directory.Exists(_folder))
            _dialog.CurrentDir = _folder;
        _dialogOpen = true;
        _hoverField = _hoverButton = _hoverBack = false;
        QueueRedraw();
        _dialog.PopupCentered(new Vector2I(900, 600));
    }

    // ---------------------------------------------------------------- input

    /// <summary>
    /// The red button: Import (narrow) or Choose another folder (wide); on the phone left of the way back (upright in the
    /// bar), alone at the right thumb without one.
    /// </summary>
    private Rect2 Button => _phone is { } f
        ? Phone.Rect(f.IsUpright ? PhoneImportUpright.Button(f, HasBack) : PhoneImport.Button(f, _phase == Phase.Refused, HasBack))
        : _phase == Phase.Refused ? WideButton : NarrowButton;

    /// <summary>The way back's link, right of the red button; on the phone level with it (upright in the bar right of it).</summary>
    private Rect2 BackBox => _phone is { } f
        ? Phone.Rect(f.IsUpright ? PhoneImportUpright.Back(f) : PhoneImport.Back(f, _phase == Phase.Refused))
        : BackLink;

    private bool ButtonLive => !_dialogOpen && _phase switch
    {
        Phase.Choose => _folder.Length > 0,
        Phase.Refused => true,
        _ => false,
    };

    private bool FieldLive => !_dialogOpen && _phase is Phase.Choose or Phase.Refused;

    /// <summary>Whether this screen has a way back (none at a first launch).</summary>
    private bool HasBack => _back is not null && _unattended is null;

    private bool BackLive => !_dialogOpen && HasBack && _phase is Phase.Choose or Phase.Refused;

    private void GoBack()
    {
        if (BackLive)
            Screens.Open(this, _back!());
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (_held)
            return;
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape } && BackLive)
        {
            GetViewport().SetInputAsHandled();
            GoBack();
        }
    }

    /// <summary>Android's Back is the way back, as Esc is.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationWMGoBackRequest && !_held)
            GoBack();
    }

    private void Press()
    {
        if (!ButtonLive)
            return;
        if (_phase == Phase.Refused)
            ChooseFolder();
        else
            StartImport();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_held)
            return;
        switch (e)
        {
            case InputEventMouseMotion motion:
            {
                var at = (motion.Position - _origin) / _k;
                bool field = FieldLive && _field.HasPoint(at), button = ButtonLive && Button.HasPoint(at),
                    back = BackLive && BackBox.HasPoint(at);
                if (field != _hoverField || button != _hoverButton || back != _hoverBack)
                {
                    _hoverField = field;
                    _hoverButton = button;
                    _hoverBack = back;
                    QueueRedraw();
                }
                break;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
            {
                var at = (click.Position - _origin) / _k;
                if (click.Pressed)
                {
                    _pressedButton = ButtonLive && Button.HasPoint(at);
                    QueueRedraw();
                }
                else
                {
                    bool wasPressed = _pressedButton;
                    _pressedButton = false;
                    if (FieldLive && _field.HasPoint(at))
                        ChooseFolder();
                    else if (wasPressed && Button.HasPoint(at))
                        Press();
                    else if (BackLive && BackBox.HasPoint(at))
                        GoBack();
                    QueueRedraw();
                }
                AcceptEvent();
                break;
            }
            case InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter }:
                Press();
                AcceptEvent();
                break;
        }
    }

    // ---------------------------------------------------------------- drawing (mock-up B)

    private string Heading => _phase switch
    {
        Phase.Refused => "This folder cannot be read",
        Phase.Importing or Phase.Done => "Importing your game",
        _ => "Import your game",
    };

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black);
        if (_phone is { } phone)
        {
            DrawPhone(new PhoneUnits(phone));
            return;
        }
        DrawSetTransform(_origin, 0, new Vector2(_k, _k));
        Ui.DrawWaveBackdrop(this, _k);
        Ui.DrawPanel(this, Frame, _k);
        DrawRect(Paper, Ui.Paper);
        DrawRect(Paper, Ui.PaperHead, false, Lacquer.Line(1, _k));

        Centred(_brush, Heading, 34, 182, Ui.HeadInk);
        Centred(_cinzel, GameFiles.EnglishTranslation10.Name, 15, 222, Ui.InkQuiet);
        DrawLine(new Vector2(Paper.Position.X + 24, 240), new Vector2(Paper.End.X - 24, 240), Ui.PaperRule, Lacquer.Line(2, _k));

        switch (_phase)
        {
            case Phase.Choose:
                DrawChoose();
                break;
            case Phase.Importing:
            case Phase.Done:
                DrawImporting();
                break;
            case Phase.Refused:
                DrawRefused();
                break;
        }
        if (BackLive)
            DrawString(_cinzel, new Vector2(BackLink.Position.X, Lacquer.Baseline(BackLink, _cinzel, 15)), "← Back to the title",
                HorizontalAlignment.Right, BackLink.Size.X, 15, _hoverBack ? Ui.Vermilion : Ui.InkQuiet);
    }

    private void DrawChoose()
    {
        float y = Step(262, 0, "Choose the game folder",
            "The folder of your copy, with system39.exe, KICHIKUSA.ALD and System39.ain in it.", StepState.Now);
        DrawField(y + 4);
        y = Step(y + 70, 1, "The files are checked and the scenario read", "Once for this folder, a second or so.", StepState.Next);
        Step(y + 14, 2, "Play", null, StepState.Next);
        DrawButton("Import", 22);
    }

    private void DrawImporting()
    {
        ImportProgress progress;
        lock (_lock)
            progress = _progress;
        bool done = _phase == Phase.Done;
        float y = Step(262, 0, "Game folder", _folder, StepState.Done);
        y = Step(y + 14, 1, done ? "The scenario is read" : progress.Stage, null, done ? StepState.Done : StepState.Now);
        float barY = y + 6;
        var bar = new Rect2(TextX, barY, TextRight - TextX, 22);
        DrawRect(bar, FieldFill);
        DrawRect(bar, Ui.InkGrey, false, Lacquer.Line(1, _k));
        float fraction = done ? 1 : (float)Math.Clamp(progress.Fraction, 0, 1);
        DrawRect(new Rect2(bar.Position + new Vector2(3, 3), new Vector2((bar.Size.X - 6) * fraction, bar.Size.Y - 6)), Ui.VermilionInk);
        DrawString(_gothic, new Vector2(TextX, barY + 46), $"{Math.Round(fraction * 100)}%", HorizontalAlignment.Right,
            TextRight - TextX, 16, Ui.Ink);
        Step(barY + 64, 2, "Play", null, done ? StepState.Now : StepState.Next);
        ProgressShot(progress, done);
    }

    /// <summary>The unattended look's dump of the bar, once, when the decompile is a third through.</summary>
    private void ProgressShot(ImportProgress progress, bool done)
    {
        if (_unattended is not null && !_progressShot && !done && progress.Fraction >= 0.3)
        {
            _progressShot = true;
            Shot("import-progress");
        }
    }

    private void DrawRefused()
    {
        float y = Step(262, 0, "Game folder", _refusal, StepState.Failed);
        if (_hashes.Length > 0)
            DrawString(_gothic, new Vector2(TextX, y + 12), _hashes, fontSize: 12, modulate: Ui.InkGrey);
        // the stamp
        var centre = new Vector2(880, 520);
        DrawArc(centre, 62, 0, Mathf.Tau, 72, Ui.Vermilion, Lacquer.Line(4, _k), true);
        DrawString(_cinzelBold, new Vector2(centre.X - 80, centre.Y), "NOT", HorizontalAlignment.Center, 160, 22, Ui.Vermilion);
        DrawString(_cinzelBold, new Vector2(centre.X - 80, centre.Y + 28), "v1.0", HorizontalAlignment.Center, 160, 18, Ui.Vermilion);
        DrawField(y + 30, compact: true);
        DrawButton("Choose another folder", 18);
    }

    private enum StepState { Now, Next, Done, Failed }

    private static Color StepColour(StepState state) => state switch
    {
        StepState.Done => Ui.InkQuiet,
        StepState.Now => Ui.Ink,
        StepState.Next => Ui.InkGrey,
        _ => Ui.VermilionInk,
    };

    /// <summary>A numbered step (一 二 三) with its title and its text wrapped under it; returns the y below it.</summary>
    private float Step(float y, int index, string title, string? body, StepState state)
    {
        var colour = StepColour(state);
        var seal = new Vector2(StepX + 15, y + 13);
        bool sealed_ = state is StepState.Now or StepState.Failed;
        if (sealed_)
            DrawCircle(seal, 15, Ui.Vermilion);
        DrawArc(seal, 14, 0, Mathf.Tau, 48, state == StepState.Next ? Ui.InkGrey : Ui.VermilionInk, Lacquer.Line(2, _k), true);
        DrawString(_gothic, new Vector2(seal.X - 15, seal.Y + 6), "一二三"[index].ToString(), HorizontalAlignment.Center, 30, 16,
            sealed_ ? Ui.SealText : colour);
        DrawString(_gothic, new Vector2(TextX, y + 20), title, fontSize: 19, modulate: colour);
        if (state == StepState.Done)
        {
            var tick = new Vector2(960, y + 12);
            DrawPolyline([tick + new Vector2(-6, 0), tick + new Vector2(-2, 5), tick + new Vector2(7, -6)], Good, Lacquer.Line(3, _k), true);
        }
        return body is null ? y + 40 : Wrap(body, TextX, y + 50, TextRight - TextX, 15,
            state == StepState.Failed ? Ui.VermilionInk : Ui.InkQuiet);
    }

    /// <summary>The folder field: its icon, the path (or "not chosen yet") and "Choose…"; it opens the folder dialog.</summary>
    private void DrawField(float y, bool compact = false)
    {
        _field = new Rect2(TextX, y, TextRight - TextX, compact ? 34 : 40);
        bool live = FieldLive;
        DrawRect(_field, FieldFill);
        DrawRect(_field, _hoverField ? Ui.Vermilion : Ui.InkGrey, false, Lacquer.Line(_hoverField ? 2 : 1, _k));
        float mid = _field.Position.Y + _field.Size.Y / 2;
        var icon = new Vector2(TextX + 12, mid - 8);
        DrawPolyline([icon + new Vector2(0, 2), icon + new Vector2(7, 2), icon + new Vector2(9, 5), icon + new Vector2(20, 5),
            icon + new Vector2(20, 16), icon + new Vector2(0, 16), icon + new Vector2(0, 2)], Ui.InkQuiet, Lacquer.Line(2, _k));
        string path = _folder.Length > 0 ? _folder : "not chosen yet";
        float maxWidth = TextRight - TextX - 150;
        DrawString(_gothic, new Vector2(TextX + 44, mid + 6), Ellipsis(path, maxWidth, 17), fontSize: 17,
            modulate: _folder.Length > 0 ? Ui.Ink : Ui.InkGrey);
        if (live)
            DrawString(_gothic, new Vector2(TextX, mid + 5), "Choose…", HorizontalAlignment.Right, TextRight - TextX - 12, 15,
                _hoverField ? Ui.Vermilion : Ui.VermilionInk);
    }

    private void DrawButton(string label, int size)
    {
        if (ButtonLive)
        {
            Ui.DrawRedButton(this, Button, _hoverButton, _pressedButton && _hoverButton, _k);
            DrawString(_cinzelBold, new Vector2(Button.Position.X, Lacquer.Baseline(Button, _cinzelBold, size)), label,
                HorizontalAlignment.Center, Button.Size.X, size, Ui.SealText);
        }
        else
        {
            DrawRect(Button, Ui.OffButton);
            DrawRect(Button, Ui.OffLine, false, Lacquer.Line(2, _k));
            DrawString(_cinzelBold, new Vector2(Button.Position.X, Lacquer.Baseline(Button, _cinzelBold, size)), label,
                HorizontalAlignment.Center, Button.Size.X, size, Ui.OffText);
        }
    }

    private void Centred(Font font, string text, int size, float middle, Color colour) =>
        DrawString(font, new Vector2(Paper.Position.X, Lacquer.Baseline(new Rect2(0, middle - 20, 1, 40), font, size)), text,
            HorizontalAlignment.Center, Paper.Size.X, size, colour);

    /// <summary>Text wrapped at spaces into <paramref name="width"/>; returns the y below its last line.</summary>
    private float Wrap(string text, float x, float baseline, float width, int size, Color colour)
    {
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string tried = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && _gothic.GetStringSize(tried, fontSize: size).X > width)
            {
                DrawString(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
                baseline += size * 1.45f;
                line = word;
            }
            else
            {
                line = tried;
            }
        }
        if (line.Length > 0)
        {
            DrawString(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
            baseline += size * 1.45f;
        }
        return baseline - size;
    }

    /// <summary>A path cut at its start to fit, as "…\Kichikuou Rance".</summary>
    private string Ellipsis(string text, float width, int size)
    {
        if (_gothic.GetStringSize(text, fontSize: size).X <= width)
            return text;
        for (int cut = 1; cut < text.Length; cut++)
        {
            string tried = "…" + text[cut..];
            if (_gothic.GetStringSize(tried, fontSize: size).X <= width)
                return tried;
        }
        return text;
    }

    // ---------------------------------------------------------------- the unattended look (--import-screen DIR)

    private async Task UnattendedAsync()
    {
        await Frames(10);
        Shot("import-choose");
        await Frames(10);
        StartImport();
        while (_phase == Phase.Importing)
            await Frames(1);
        await Frames(5);
        Shot(_phase == Phase.Done ? "import-done" : "import-refused");
        await Frames(5);
        if (_quit)
            GetTree().Quit(_phase == Phase.Done || _phase == Phase.Refused ? 0 : 1);
        else if (_phase == Phase.Done)
            Screens.Open(this, _next());
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>The window as the next frame draws it, into <see cref="_shots"/> (a window is needed: headless draws none).</summary>
    private async void Shot(string what)
    {
        if (_shots is null)
            return;
        System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath(_shots));
        string path = System.IO.Path.Combine(ProjectSettings.GlobalizePath(_shots), $"{_shot++:D5}-{what}.png");
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"Import screen: saved {path}");
    }
}
