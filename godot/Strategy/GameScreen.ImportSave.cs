using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Saves;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// File → Import Original Save… (owner, 2026-11-10; docs/save-import.md, §7; ours, xsystem35 has no such item): Godot's
/// native file dialog in the game folder showing <c>*.asd</c>, the file read and checked (<see cref="OriginalSave"/>), a
/// refusal said in a message box, then a question as Restart's — the save, its date and file, and that the game under way
/// is left without saving — and on Load the game screen started again with the save, which the session loads at once after
/// the boot (<see cref="GameStart.Original"/>). Nothing is written: no save of ours, the file untouched.
/// </summary>
public partial class GameScreen
{
    private FileDialog? _saveDialog;

    /// <summary>The menu item: the file dialog (not while a box or a screen of the menu bar is open).</summary>
    private void ImportOriginalSave()
    {
        if (_modal)
            return;
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
                ImportChosen(path);
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

    /// <summary>A file chosen: read and checked, then the refusal or the question.</summary>
    private void ImportChosen(string path)
    {
        string file = System.IO.Path.GetFileName(path);
        SaveData save;
        try
        {
            save = OriginalSave.Read(path);
        }
        catch (OriginalSaveException e)
        {
            GD.Print($"Kichikuou: {path} not imported: {e.Message}");
            Refuse($"{file} cannot be imported.", e.Message);
            return;
        }
        string label = OriginalSave.Label(path);
        string date = HaremRoster.DateText(save.Variables[517].Value) ?? "";              // Week (VAR0517)
        string written = save.Written is { } time ? $", saved {time:yyyy-MM-dd HH:mm}" : "";
        var question = new PhoneMenu.Question("Load the original's save?",
            $"{label}: {date}\n{file}{written}\n\n{PhoneMenu.LoseProgress}", "Load");
        Ask(question, () =>
        {
            GD.Print($"Kichikuou: importing {path} ({label}, {save.Resume}).");
            Screens.Open(this, new GameScreen(new GameOptions
            {
                Start = GameStart.Original, OriginalSave = save, OriginalLabel = label,
            }));
        });
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
