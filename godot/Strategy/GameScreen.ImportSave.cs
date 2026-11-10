using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Saves;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// File → Import Original Save… (owner, 2026-11-10; docs/save-import.md, §7; ours, xsystem35 has no such item): Godot's
/// native file dialog in the game folder showing <c>*.asd</c>, the file read and checked (<see cref="OriginalSave"/>), a
/// refusal said in a message box, then a question as Restart's — the save, its date and file, and that the game under way
/// is left without saving — and on Load the game screen started again with the save, which the session loads at once after
/// the boot (<see cref="GameStart.Original"/>). Nothing is written: no save of ours, the file untouched.
/// On Android (§8: its file picker answers with a <c>content://</c> URI and hides <c>Android/data</c>, where the game folder
/// lives) the game offers the game folder's saves itself: the phone's Menu sheet has a page of them (the owner's S1,
/// <see cref="MenuSheetView"/>), the tablet's menu bar item a stock dialog listing them (T1), as in the touch layout on a PC.
/// </summary>
public partial class GameScreen
{
    private FileDialog? _saveDialog;
    private bool _autoImported;                        // --menu-import's exercise ran

    /// <summary>The menu item: the file dialog, or on Android (and in the touch layout) the list (not while a box or a screen of the menu bar is open).</summary>
    private void ImportOriginalSave()
    {
        if (_modal)
            return;
        if (OS.GetName() == "Android" || TouchMode.On)
        {
            ImportFromList();
            return;
        }
        if (_saveDialog is null)
        {
            _saveDialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.OpenFile,
                Access = FileDialog.AccessEnum.Filesystem,
                UseNativeDialog = true,
                Title = "Import a save of the original game",
                Filters = ["*.asd ; Saves of the original game"],
            };
            _saveDialog.FileSelected += path =>
            {
                _modal = false;
                ImportChosen(OriginalSaveFolder.ReadFile(path));
            };
            _saveDialog.Canceled += () => _modal = false;
            AddChild(_saveDialog);
        }
        string folder = GameData.GameDirectory;
        if (folder.Length > 0 && System.IO.Directory.Exists(folder))
            _saveDialog.CurrentDir = folder;
        _modal = true;
        _input.Clear();
        _saveDialog.PopupCentered(new Vector2I(900, 600));
    }

    /// <summary>The game folder's saves of the original (§8), the folder named as the player reaches it (Android's storage left out).</summary>
    private static PhoneMenu.SaveShelf OriginalSaveShelf()
    {
        const string Storage = "/storage/emulated/0/";
        string folder = GameData.GameDirectory;
        return new(OriginalSaveFolder.Read(folder), folder.StartsWith(Storage, StringComparison.Ordinal) ? folder[Storage.Length..] : folder);
    }

    /// <summary>A file chosen and read: the refusal or the question.</summary>
    private void ImportChosen(OriginalSaveFile file)
    {
        if (file.Refused)
        {
            GD.Print($"Kichikuou: {file.Path} not imported: {file.Refusal}");
            var notice = PhoneMenu.RefusalOf(file);
            Refuse(notice.Title, notice.Line);
            return;
        }
        var question = PhoneMenu.ImportQuestion(file) with
        {
            Line = string.Join("\n", file.QuestionLines) + "\n\n" + PhoneMenu.LoseProgress,     // the PC's box: a blank line before it
        };
        Ask(question, () => LoadOriginal(file));
    }

    /// <summary>
    /// The game screen started again with <paramref name="file"/>'s save, loaded at once after the boot. Unattended play
    /// (<see cref="GameOptions.Auto"/>) keeps its options and goes on from it.
    /// </summary>
    private void LoadOriginal(OriginalSaveFile file)
    {
        GD.Print($"Kichikuou: importing {file.Path} ({file.Label}, {file.Save!.Resume}).");
        var options = _options.Auto
            ? _options with
            {
                Start = GameStart.Original, OriginalSave = file.Save, OriginalLabel = file.Label, AutoImportOriginal = null, SlotsFrom = null,
            }
            : new GameOptions { Start = GameStart.Original, OriginalSave = file.Save, OriginalLabel = file.Label };
        Screens.Open(this, new GameScreen(options));
    }

    private ConfirmationDialog? _saveList;
    private Tree? _saveTree;

    /// <summary>
    /// The tablet's list (T1): a stock dialog with the game folder's saves — "Original save A", its date, its stage, or why it
    /// cannot be imported —, the first selected; Load (or a double tap) asks the PC's question, or says why it is refused.
    /// With none, a message box saying where the original keeps them and where they go.
    /// </summary>
    private void ImportFromList()
    {
        var shelf = OriginalSaveShelf();
        if (shelf.Saves.Count == 0)
        {
            Refuse("No saves of the original", PhoneMenu.NoSaves(shelf.Folder).Replace("\n", "\n\n"));
            return;
        }
        var dialog = new ConfirmationDialog { Title = "Import a save of the original game", OkButtonText = "Load" };
        var box = new VBoxContainer();
        box.AddChild(new Label { Text = "The saves in the game folder:" });
        var tree = new Tree
        {
            Columns = 3, HideRoot = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(640, 340),
        };
        var root = tree.CreateItem();
        for (int i = 0; i < shelf.Saves.Count; i++)
        {
            var save = shelf.Saves[i];
            var item = tree.CreateItem(root);
            item.SetText(0, save.Label);
            item.SetText(1, save.Refused ? "cannot be imported" : save.DateText);
            item.SetText(2, save.Refused ? save.RefusalWord : save.StageText);
            item.SetMetadata(0, i);
            if (save.Refused)
                for (int c = 0; c < 3; c++)
                    item.SetCustomColor(c, new Color(0.6f, 0.6f, 0.6f));
        }
        root.GetFirstChild()?.Select(0);
        box.AddChild(tree);
        dialog.AddChild(box);
        _modal = true;
        _input.Clear();
        dialog.Confirmed += Chosen;
        dialog.Canceled += () => Close(null);
        tree.ItemActivated += Chosen;
        AddChild(dialog);
        (_saveList, _saveTree) = (dialog, tree);
        dialog.PopupCentered();

        void Chosen()
        {
            int i = tree.GetSelected() is { } selected ? selected.GetMetadata(0).AsInt32() : -1;
            Close(i >= 0 ? shelf.Saves[i] : null);
        }

        void Close(OriginalSaveFile? chosen)
        {
            if (!IsInstanceValid(dialog) || dialog.IsQueuedForDeletion())
                return;
            _modal = false;
            (_saveList, _saveTree) = (null, null);
            dialog.QueueFree();
            if (chosen is not null)
                ImportChosen(chosen);
        }
    }

    /// <summary>
    /// <see cref="GameOptions.AutoImportOriginal"/> in unattended play, once at the first map wait: on the phone the Menu
    /// cell's tap, Import original save's, the save's (dumps <c>menu-sheet-saves</c>, <c>menu-sheet-save-question</c>) and
    /// Load's, through Godot's input; else the menu bar's item, the list's row chosen and Load, the question answered Load
    /// (dumps <c>import-list</c>, <c>import-list-question</c>). Then the screen is the save's: this one's game stops.
    /// </summary>
    private async Task AutoImportOriginalAsync(string letter)
    {
        _autoImported = true;
        GD.Print($"Kichikuou: --auto imports the original's save {letter} as Android offers it.");
        if (PhoneLandscape || PhonePortrait)
        {
            UpdatePhone();
            await FrameAsync();
            await TapAsync(_menuSheet.CellCentre);
            if (!_menuSheet.IsOpen)
                throw new InvalidOperationException("--menu-import's tap on the Menu cell opened no Menu sheet.");
            await TapAsync(_menuSheet.TileCentre(PhoneMenu.Command.ImportSave));
            if (_menuSheet.Tiles is not [{ Command: PhoneMenu.Command.Back }, ..])
                throw new InvalidOperationException("--menu-import's tap on Import original save opened no page of saves.");
            Shot("menu-sheet-saves");
            await TapAsync(_menuSheet.SaveCentre(letter));
            if (_menuSheet.Asking is not { Confirm: "Load" })
                throw new InvalidOperationException($"--menu-import's tap on save {letter} asked no question to load it ({_menuSheet.Asking?.Title}).");
            Shot("menu-sheet-save-question");
            await TapAsync(_menuSheet.ConfirmCentre);
        }
        else
        {
            ImportOriginalSave();
            if (_saveTree?.GetRoot() is not { } root)
                throw new InvalidOperationException("--menu-import found no list of the original's saves.");
            await FrameAsync();
            Shot("import-list");
            var row = root.GetChildren().FirstOrDefault(r => r.GetText(0).EndsWith(" " + letter, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"--menu-import found no save {letter} in the list.");
            row.Select(0);
            _saveList!.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
            await FrameAsync();
            if (_lastAsk is not { } ask || !IsInstanceValid(ask))
                throw new InvalidOperationException($"--menu-import's Load of save {letter} asked no question.");
            Shot("import-list-question");
            ask.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
        }
        await FrameAsync();
        if (!_closing)
            throw new InvalidOperationException($"--menu-import's Load of save {letter} did not start the game again.");
        throw new OperationCanceledException();                         // this screen's game ends; the save's goes on

        async Task FrameAsync() => await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);

        async Task TapAsync(Vector2 at)
        {
            SendPointerMotion(at);
            await FrameAsync();
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, MouseButton.Left, pressed);
                await FrameAsync();
            }
        }
    }

    /// <summary>A message box with <paramref name="title"/> and the reason in words; the game waits while it is open.</summary>
    private void Refuse(string title, string reason)
    {
        var box = new AcceptDialog { Title = title, DialogText = reason, DialogAutowrap = true };
        box.Size = new Vector2I(560, 0);
        _modal = true;
        _input.Clear();
        box.Confirmed += () => Close(box);
        box.Canceled += () => Close(box);
        AddChild(box);
        box.PopupCentered(new Vector2I(560, 0));

        void Close(AcceptDialog dialog)
        {
            _modal = false;
            dialog.QueueFree();
        }
    }
}
