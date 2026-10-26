using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our Save/Load screen over <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s lists on the save library (owner's decision 29; docs/ui/save-load.md, §9,
/// the owner's layout G, 2026-10-21): one lacquer panel; on the left a switch between the two lists — the manual saves,
/// newest first (no order to choose, the owner's 2026-10-21; the number stays on the seal), scrolling (the touch mode's by a finger's drag too), a row's second
/// line its comment, "+ New save" heading them in Save; and the auto-saves, newest first, ten weeks' Turn Starts and Turn
/// Ends (owner's decision 31), each tagged at its row's bottom right under the time and scrolling as the manual saves do
/// (docs/ui/save-load.md, §12, the owner's K1) — and the chosen save's page on the right: its map large (All Data's
/// miniature), its figures, the save's comment (Edit writes it in place: Enter keeps it, Esc leaves the old one), an
/// auto-save's kind after the title and what its load does under the comment,
/// Delete at the save's title (a danger button asking in a callout under it, the owner's B5 of 2026-10-25,
/// docs/ui/save-load.md, §11), Back at the foot's left and a red button that answers the script's menu with the save's item
/// ("Load", "Save over No. n", "Save") — the second click, so no question. An auto-save takes no Save; Delete works in
/// either list and either tab. After Sengoku Rance's notes (docs/ui/save-load.md, §10, the owner's N1, 2026-10-22): a
/// save with a comment has a gold star on its seal's top left corner, and the manual saves' heading has a button
/// deleting every one without a comment, asking first in a callout beside it while the rows to go are struck through. From <c>COMMAND</c> the Load | Save tabs switch lists (<see cref="LoadSaveShortcut.Switch"/>).
/// Back, a right click or Esc answers 0. Laid out in the 1280×800 screen's pixels, scaled with the picture. After its answer the
/// screen stays drawn, taking no clicks, until the script shows the next list or the save screen closes.
/// </summary>
internal sealed partial class SaveLoadView : Control, IUiPaletteUser
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    // hits: the button, the tabs, the lists' switch, Delete and its answers, Edit, New save, the sweep and its answers,
    // Back, and from RowHit the rows shown, top to bottom
    private const int ButtonHit = 200, TabHit = 100, SideHit = 400, DeleteHit = 500, DeleteYesHit = 501,
        DeleteNoHit = 502, EditHit = 600, NewHit = 700, SweepHit = 800, SweepYesHit = 801, SweepNoHit = 802, BackHit = 900,
        RowHit = 1000;

    private static readonly Rect2 PanelRect = new(30, 24, 1220, 752);
    private static readonly Rect2 MapRect = new(426, 118, 800, 333);
    private static readonly Rect2 ButtonRect = new(896, 694, 330, 58);
    private static readonly Rect2 DeleteRect = new(1090, 84, 136, 30), BackRect = new(426, 694, 140, 58);
    private static readonly Rect2 CommentRect = new(426, 585, 696, 34);
    private static readonly Rect2 EditRect = new(1132, 585, 94, 34);
    private static readonly Rect2 CalloutRect = new(806, 126, 420, 110);
    private static readonly Rect2 YesRect = new(1004, 196, 110, 30), NoRect = new(1124, 196, 80, 30);
    private static readonly Rect2 SweepRect = new(206, 128, 176, 26);
    private static readonly Rect2 SweepCalloutRect = new(400, 75, 440, 132);
    private static readonly Rect2 SweepYesRect = new(608, 153, 120, 30), SweepNoRect = new(738, 153, 80, 30);
    private const float ListX = 52, ListW = 330, SideY = 84, SideH = 30, HeadY = 141, ListTop = 162, ListBottom = 756;
    private const float RowW = 318, RowH = 52, RowPitch = 57, NewH = 44, PageX = 426, PageY = 84;
    private const int CommentLength = 60;

    private static Color ChosenFill => Ui.ChosenFill;
    private static readonly Color AutoGold = Color.Color8(138, 104, 34);
    private static readonly Color TagText = Color.Color8(246, 232, 206);
    private static readonly Color TurnStartFill = Color.Color8(70, 110, 50), TurnEndFill = Color.Color8(118, 30, 24);
    private static Color StarRim => Ui.Rim;
    private static Color GoneVeil => Ui.Backdrop with { A = 150 / 255f };

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700), _cinzelLight = Lacquer.Cinzel(600);
    private readonly Dictionary<string, ImageTexture?> _maps = [];
    private readonly LineEdit _edit;
    private FontFile _gothic;
    private SaveList? _list;
    private IReadOnlyList<SaveId?> _items = [];
    private ISaveLibrary? _library;
    private SaveShelf _shelf = new([], []);
    private IReadOnlyList<SaveSlot> _manuals = [];
    private SaveSlot? _now;
    private bool _tabs, _autoSide, _chosenNew, _confirm, _sweep, _editing;
    private SaveId? _chosen;
    private int _scroll;
    private float _k = 1;
    private int _hovered, _pressed;
    private TaskCompletionSource<(int Choice, SaveList? Switch)>? _answer;

    public SaveLoadView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        _edit = new LineEdit { Visible = false, MaxLength = CommentLength, Flat = true, ContextMenuEnabled = false };
        ApplyPalette();
        _edit.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        _edit.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        _edit.TextSubmitted += _ => EndEdit(keep: true);
        _edit.GuiInput += e =>
        {
            if (e is InputEventKey { Keycode: Key.Escape, Pressed: true })
            {
                EndEdit(keep: false);
                _edit.AcceptEvent();
            }
        };
        _edit.FocusExited += () => EndEdit(keep: true);
        AddChild(_edit);
    }

    /// <summary>Option → Display: the comment's field in the palette's ink.</summary>
    public void ApplyPalette()
    {
        _edit.AddThemeColorOverride("font_color", Ui.Ink);
        _edit.AddThemeColorOverride("caret_color", Ui.Ink);
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>The list shown, or null.</summary>
    public SaveList? List => _list;

    /// <summary>The save whose page is shown, or null (none, or the new save).</summary>
    public SaveId? Chosen => _chosen;

    /// <summary>Whether the new manual save is chosen (the save list).</summary>
    public bool NewChosen => _chosenNew;

    /// <summary>Whether the auto-saves are shown, not the manual saves.</summary>
    public bool AutoSide => _autoSide;

    /// <summary>The manual saves top to bottom, newest first.</summary>
    public IReadOnlyList<SaveId> ManualSaves => [.. _manuals.Select(s => s.Id!.Value)];

    /// <summary>The auto-saves, newest first.</summary>
    public IReadOnlyList<SaveId> AutoSaves => [.. _shelf.Autos.Select(s => s.Id!.Value)];

    /// <summary>
    /// Shows <paramref name="list"/>: the script's menu stands for <paramref name="items"/> (<c>SaveLibraryListEvent</c>), the
    /// saves are read from <paramref name="library"/>, <paramref name="now"/> is the game a save stores. The save chosen
    /// before is kept when the same list opens again, the one just written chosen after a save (<paramref name="last"/>);
    /// a list opened anew chooses the save written last to load, the new save to save. <see cref="ChooseAsync"/> waits.
    /// </summary>
    public void Open(SaveList list, IReadOnlyList<SaveId?> items, ISaveLibrary library, SaveSlot? now, SaveId? last, bool tabs)
    {
        bool again = Visible && _list == list;
        _list = list;
        _items = items;
        _library = library;
        _now = now;
        _tabs = tabs;
        Reload();
        if (list == SaveList.Save && last is { Kind: SaveKind.Manual } written && again)
            Choose(written);
        else if (!again || (_chosen is { } kept && Find(kept) is null) || (_chosenNew && list != SaveList.Save))
            ChooseFirst(list);
        _confirm = _sweep = false;
        _hovered = _pressed = 0;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>The script's answer: the chosen save's item, or 0 to leave the list — with the other list for a tab.</summary>
    public Task<(int Choice, SaveList? Switch)> ChooseAsync()
    {
        _answer = new TaskCompletionSource<(int, SaveList?)>();
        return _answer.Task;
    }

    /// <summary>The menu was answered: the screen stays drawn and takes no clicks until the next list or <see cref="Close"/>.</summary>
    public void Release()
    {
        EndEdit(keep: true);
        _answer?.TrySetCanceled();
        _answer = null;
        _confirm = _sweep = false;
        _hovered = _pressed = 0;
        _finger.Cancel();
        MouseDefaultCursorShape = CursorShape.Arrow;
        QueueRedraw();
    }

    /// <summary>The save screen closed: hidden, the choice forgotten.</summary>
    public void Close()
    {
        Release();
        _list = null;
        _chosen = null;
        _chosenNew = false;
        _library = null;
        Visible = false;
    }

    /// <summary>Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        PlaceEdit();
        QueueRedraw();
    }

    // ---------------------------------------------------------------- unattended play's points

    /// <summary>A save's row centre in global coordinates, or null when it is not in view (unattended play's clicks).</summary>
    public Vector2? RowCentre(SaveId id) =>
        Rows().FirstOrDefault(r => r.Id == id) is { Id: not null } row ? Global(row.Rect.GetCenter()) : null;

    /// <summary>The New save row's centre in global coordinates.</summary>
    public Vector2 NewCentre => Global(NewRect.GetCenter());

    /// <summary>A tab's centre in global coordinates.</summary>
    public Vector2 TabCentre(SaveList list) => Global(TabRect(list).GetCenter());

    /// <summary>The lists' switch: the manual saves' half or the auto-saves'.</summary>
    public Vector2 SideCentre(bool auto) => Global(SideRect(auto).GetCenter());

    /// <summary>The red button's centre in global coordinates.</summary>
    public Vector2 ButtonCentre => Global(ButtonRect.GetCenter());

    /// <summary>Delete's centre, and its question's answers.</summary>
    public Vector2 DeleteCentre => Global(DeleteRect.GetCenter());

    public Vector2 DeleteAnswerCentre(bool yes) => Global((yes ? YesRect : NoRect).GetCenter());

    /// <summary>Back's centre in global coordinates.</summary>
    public Vector2 BackCentre => Global(BackRect.GetCenter());

    /// <summary>The sweep's centre, and its question's answers.</summary>
    public Vector2 SweepCentre => Global(SweepRect.GetCenter());

    public Vector2 SweepAnswerCentre(bool yes) => Global((yes ? SweepYesRect : SweepNoRect).GetCenter());

    /// <summary>Whether the sweep's question is open.</summary>
    public bool SweepAsked => _sweep;

    /// <summary>The manual saves the sweep would delete (those without a comment).</summary>
    public int UncommentedCount => SaveSlots.Uncommented(_shelf.Manuals).Count;

    /// <summary>Edit's centre in global coordinates.</summary>
    public Vector2 EditCentre => Global(EditRect.GetCenter());

    /// <summary>The comment's field while it is written (unattended play types into it).</summary>
    public LineEdit CommentField => _edit;

    private Vector2 Global(Vector2 layout) => GetGlobalTransformWithCanvas() * (layout * _k);

    // ---------------------------------------------------------------- the lists

    private void Reload()
    {
        _shelf = _library is { } library ? SaveSlots.ReadLibrary(library) : new SaveShelf([], []);
        _manuals = SaveSlots.NewestFirst(_shelf.Manuals);
    }

    private SaveSlot? Find(SaveId id) =>
        (id.Kind == SaveKind.Auto ? _shelf.Autos : _shelf.Manuals).FirstOrDefault(s => s.Id == id);

    /// <summary>To load, the save written last; to save, the new save.</summary>
    private void ChooseFirst(SaveList list)
    {
        if (list == SaveList.Save)
        {
            _chosen = null;
            _chosenNew = true;
            _autoSide = false;
            _scroll = 0;
            return;
        }
        var latest = _shelf.Autos.Concat(_shelf.Manuals).MaxBy(s => s.Written ?? DateTime.MinValue);
        _chosenNew = false;
        _chosen = null;
        if (latest?.Id is { } id)
            Choose(id);
        else
            ShowSide(false);
    }

    private void Choose(SaveId id)
    {
        _chosen = id;
        _chosenNew = false;
        ShowSide(id.Kind == SaveKind.Auto);
        ScrollTo(id);
    }

    /// <summary>The auto-saves or the manual saves shown, from their top when the side changes.</summary>
    private void ShowSide(bool auto)
    {
        if (auto != _autoSide)
            _scroll = 0;
        _autoSide = auto;
    }

    /// <summary>The list shown, top to bottom: the auto-saves or the manual saves, newest first.</summary>
    private IReadOnlyList<SaveSlot> Shown => _autoSide ? _shelf.Autos : _manuals;

    private int RowsShown => (int)((ListBottom - RowsTop) / RowPitch);

    /// <summary>The first row's top: under New save in the save list's manual saves.</summary>
    private float RowsTop => ListTop + (!_autoSide && _list == SaveList.Save ? NewH + 6 : 0);

    private void ScrollTo(SaveId id)
    {
        int place = Shown.ToList().FindIndex(s => s.Id == id);
        if (place < 0)
            return;
        if (place < _scroll)
            _scroll = place;
        else if (place >= _scroll + RowsShown)
            _scroll = place - RowsShown + 1;
    }

    private void ScrollBy(int rows) => SetScroll(_scroll + rows);

    /// <summary>The list shown from the <paramref name="row"/>th save on in view, as far as it goes.</summary>
    private void SetScroll(int row)
    {
        row = Math.Clamp(row, 0, MaxScroll);
        if (row == _scroll)
            return;
        _scroll = row;
        QueueRedraw();
    }

    /// <summary>The farthest the list shown scrolls: the first row in view when the last one is at the foot.</summary>
    private int MaxScroll => Math.Max(0, Shown.Count - RowsShown);

    /// <summary>The rows in view, top to bottom, with their places.</summary>
    private IEnumerable<(SaveId? Id, Rect2 Rect)> Rows()
    {
        var shown = Shown;
        for (int i = _scroll, place = 0; i < shown.Count && place < RowsShown; i++, place++)
            yield return (shown[i].Id, new Rect2(ListX, RowsTop + place * RowPitch, RowW, RowH));
    }

    private static Rect2 NewRect => new(ListX, ListTop, RowW, NewH);

    private static Rect2 TabRect(SaveList list) => new(PanelRect.End.X - 220 + (list == SaveList.Save ? 96 : 0), PanelRect.Position.Y + 11, 90, 30);

    private static Rect2 SideRect(bool auto) => new(ListX + (auto ? ListW / 2 + 2 : 0), SideY, ListW / 2 - 2, SideH);

    /// <summary>The chosen save's item in the script's menu, or 0 when the menu has none for it (an auto-save in Save).</summary>
    private int ItemOfChosen
    {
        get
        {
            if (!_chosenNew && _chosen is null)
                return 0;
            int index = _items.ToList().IndexOf(_chosenNew ? null : _chosen);
            return index + 1;
        }
    }

    private bool ButtonShown => ItemOfChosen > 0;

    private SaveSlot? ChosenSave => _chosen is { } id ? Find(id) : null;

    // ---------------------------------------------------------------- input

    private int HitAt(Vector2 window)
    {
        var at = window / _k;
        if (_confirm)
            return YesRect.HasPoint(at) ? DeleteYesHit : NoRect.HasPoint(at) ? DeleteNoHit : 0;
        if (_sweep)
            return SweepYesRect.HasPoint(at) ? SweepYesHit : SweepNoRect.HasPoint(at) ? SweepNoHit : 0;
        if (!_autoSide && UncommentedCount > 0 && SweepRect.HasPoint(at))
            return SweepHit;
        if (ButtonShown && ButtonRect.HasPoint(at))
            return ButtonHit;
        if (BackRect.HasPoint(at))
            return BackHit;
        if (ChosenSave is not null && DeleteRect.HasPoint(at))
            return DeleteHit;
        if (ChosenSave is { Id: not null } && !_editing && EditRect.HasPoint(at))
            return EditHit;
        if (_tabs)
            foreach (var list in (SaveList[])[SaveList.Load, SaveList.Save])
                if (list != _list && TabRect(list).HasPoint(at))
                    return TabHit + (int)list;
        foreach (bool auto in (bool[])[false, true])
            if (auto != _autoSide && SideRect(auto).HasPoint(at))
                return SideHit + (auto ? 1 : 0);
        if (!_autoSide && _list == SaveList.Save && NewRect.HasPoint(at))
            return NewHit;
        int place = 0;
        foreach (var (_, rect) in Rows())
        {
            if (rect.HasPoint(at))
                return RowHit + place;
            place++;
        }
        return 0;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
        {
            AcceptEvent();                                              // drawn between two lists: nothing passes through
            return;
        }
        if (TouchMode.On && ListTouch(e))
        {
            AcceptEvent();
            return;
        }
        switch (e)
        {
            case InputEventMouseMotion motion:
                int hit = HitAt(motion.Position);
                if (hit != _hovered)
                {
                    _hovered = hit;
                    MouseDefaultCursorShape = hit != 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel:
                if (!_confirm && !_sweep)
                {
                    ScrollBy(wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1);
                    _hovered = HitAt(wheel.Position);
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                if (_confirm || _sweep)
                {
                    _confirm = _sweep = false;
                    QueueRedraw();
                }
                else
                    Answer((0, null));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = HitAt(button.Position);
                if (button.Pressed)
                {
                    _pressed = at;
                    if (_editing)
                        EndEdit(keep: true);                            // a click off the field keeps what is written
                }
                else if (at != 0 && at == _pressed)
                    Act(at);
                else
                    _pressed = 0;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (_answer is null || e is not InputEventKey { Keycode: Key.Escape, Pressed: true })
            return;
        if (_confirm || _sweep)
        {
            _confirm = _sweep = false;
            QueueRedraw();
        }
        else
            Answer((0, null));
        GetViewport().SetInputAsHandled();
    }

    private void Act(int hit)
    {
        _pressed = 0;
        switch (hit)
        {
            case ButtonHit:
                Answer((ItemOfChosen, null));
                break;
            case BackHit:
                Answer((0, null));
                break;
            case DeleteHit:
                _confirm = true;
                break;
            case DeleteYesHit:
                Delete();
                break;
            case DeleteNoHit:
                _confirm = false;
                break;
            case SweepHit:
                _sweep = true;
                break;
            case SweepYesHit:
                Sweep();
                break;
            case SweepNoHit:
                _sweep = false;
                break;
            case EditHit:
                StartEdit();
                break;
            case NewHit:
                _chosen = null;
                _chosenNew = true;
                break;
            case >= RowHit:
                if (Rows().ElementAtOrDefault(hit - RowHit).Id is { } id)
                    Choose(id);
                break;
            case >= NewHit:
                break;
            case >= SideHit:
                ShowSide(hit == SideHit + 1);
                if (_chosen is { } chosen && (chosen.Kind == SaveKind.Auto) == _autoSide)
                    ScrollTo(chosen);
                break;
            case > TabHit:
                Answer((0, (SaveList)(hit - TabHit)));
                break;
        }
    }

    /// <summary>The chosen save deleted (the answer Yes): the next one in its list chosen, else the one before it.</summary>
    private void Delete()
    {
        _confirm = false;
        if (_chosen is not { } id || _library is not { } library)
            return;
        var rows = id.Kind == SaveKind.Auto ? _shelf.Autos : _manuals;
        int place = rows.ToList().FindIndex(s => s.Id == id);
        if (!library.Delete(id))
            return;
        Reload();
        rows = id.Kind == SaveKind.Auto ? _shelf.Autos : _manuals;
        _chosen = null;
        if (rows.Count > 0 && rows[Math.Clamp(place, 0, rows.Count - 1)].Id is { } next)
            Choose(next);
        else if (_list == SaveList.Save && id.Kind == SaveKind.Manual)
            _chosenNew = true;                                          // the last manual save gone: New save
        ScrollBy(0);
    }

    /// <summary>
    /// The sweep answered Delete: every manual save without a comment deleted (<see cref="SaveSlots.DeleteUncommented"/>);
    /// a chosen save that went gives its place to the newest manual save left, else New save (Save) or the save written
    /// last (Load).
    /// </summary>
    private void Sweep()
    {
        _sweep = false;
        if (_library is not { } library)
            return;
        SaveSlots.DeleteUncommented(library);
        Reload();
        _scroll = 0;
        if (_chosen is { } id && Find(id) is null)
        {
            if (_manuals.Count > 0 && _manuals[0].Id is { } newest)
                Choose(newest);
            else if (_list == SaveList.Save)
            {
                _chosen = null;
                _chosenNew = true;
            }
            else
                ChooseFirst(SaveList.Load);
        }
        else if (_chosen is { Kind: SaveKind.Manual } kept)
            ScrollTo(kept);
    }

    private void Answer((int, SaveList?) answer)
    {
        EndEdit(keep: true);
        var task = _answer;
        _answer = null;
        task?.TrySetResult(answer);
    }

    // ---------------------------------------------------------------- the comment

    private void StartEdit()
    {
        if (ChosenSave is not { Id: not null } save)
            return;
        _editing = true;
        _edit.Text = save.Comment;
        _edit.Visible = true;
        PlaceEdit();
        _edit.GrabFocus();
        _edit.CaretColumn = _edit.Text.Length;
    }

    /// <summary>The comment written kept (Enter, a click elsewhere) or left (Esc).</summary>
    private void EndEdit(bool keep)
    {
        if (!_editing)
            return;
        _editing = false;
        if (keep && _chosen is { } id && _library is { } library && ChosenSave?.Comment != _edit.Text.Trim())
        {
            library.SetComment(id, _edit.Text.Trim());
            Reload();
        }
        _edit.Visible = false;
        if (_edit.HasFocus())
            _edit.ReleaseFocus();
        QueueRedraw();
    }

    private void PlaceEdit()
    {
        var box = CommentRect.Grow(-4);
        _edit.Position = new Vector2(box.Position.X + 6, box.Position.Y) * _k;
        _edit.Size = new Vector2(box.Size.X - 12, box.Size.Y) * _k;
        _edit.AddThemeFontOverride("font", _gothic);
        _edit.AddThemeFontSizeOverride("font_size", Math.Max(8, (int)Math.Round(16 * _k)));
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    /// <summary>The baseline that puts text of <paramref name="size"/> with its middle at <paramref name="y"/>.</summary>
    private static float Middle(Font font, int size, float y) => y + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    private void Text(Font font, int size, float x, float middle, string text, Color colour) =>
        DrawString(font, new Vector2(x, Middle(font, size, middle)), text, fontSize: size, modulate: colour);

    private void TextRight(Font font, int size, float right, float middle, string text, Color colour) =>
        Text(font, size, right - font.GetStringSize(text, fontSize: size).X, middle, text, colour);

    private void TextCentred(Font font, int size, float centre, float middle, string text, Color colour) =>
        Text(font, size, centre - font.GetStringSize(text, fontSize: size).X / 2, middle, text, colour);

    /// <summary><paramref name="text"/> cut to <paramref name="width"/> with an ellipsis.</summary>
    private string Fit(string text, int size, float width)
    {
        if (_gothic.GetStringSize(text, fontSize: size).X <= width)
            return text;
        while (text.Length > 1 && _gothic.GetStringSize(text + "…", fontSize: size).X > width)
            text = text[..^1];
        return text.TrimEnd() + "…";
    }

    public override void _Draw()
    {
        if (_list is not { } list)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, 120 / 255f));
        Ui.DrawPanel(this, PanelRect, _k);
        bool save = list == SaveList.Save;
        Text(_brush, 24, PanelRect.Position.X + 22, PanelRect.Position.Y + 25, save ? "Save" : "Load", Ui.Name);
        Ui.DrawDivider(this, PanelRect.Position.X + 16, PanelRect.End.X - 16, PanelRect.Position.Y + 44, _k);
        if (_tabs)
        {
            DrawTab(SaveList.Load);
            DrawTab(SaveList.Save);
        }
        DrawSide(false, $"Manual saves · {_shelf.Manuals.Count}");
        DrawSide(true, $"Auto-saves · {_shelf.Autos.Count}");
        if (_autoSide)
            DrawAutos();
        else
            DrawManuals(save);
        DrawPage(save);
        if (_confirm)
            DrawCallout();
        if (_sweep)
            DrawSweepCallout();
    }

    private void DrawSide(bool auto, string text)
    {
        var rect = SideRect(auto);
        bool on = auto == _autoSide, hover = _hovered == SideHit + (auto ? 1 : 0);
        if (on)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
            Ui.DrawRowBox(this, rect, hover, _k);
        TextCentred(_cinzel, 14, rect.GetCenter().X, rect.GetCenter().Y, text, on ? Ui.Name : hover ? Ui.Text : Ui.Quiet);
    }

    private void DrawAutos()
    {
        Text(_cinzel, 11, ListX, HeadY, "NEWEST FIRST", Ui.Subtitle);
        TextRight(_gothic, 11, ListX + ListW, HeadY, "the game keeps the last ten weeks", Ui.Label);
        foreach (var (id, rect) in Rows())
            if (id is { } save && Find(save) is { } held)
                DrawRow(rect, held, auto: true);
        if (_shelf.Autos.Count == 0)
            TextCentred(_cinzelLight, 15, ListX + ListW / 2, ListTop + 40, "No auto-saves yet.", Ui.OffText);
        DrawGroove();
    }

    private void DrawManuals(bool save)
    {
        Text(_cinzel, 11, ListX, HeadY, "NEWEST FIRST", Ui.Subtitle);
        DrawSweepButton();
        if (save)
            DrawNewRow();
        foreach (var (id, rect) in Rows())
            if (id is { } held && Find(held) is { } slot)
                DrawRow(rect, slot, auto: false);
        if (_manuals.Count == 0)
            TextCentred(_cinzelLight, 15, ListX + ListW / 2, RowsTop + 40, "No manual saves yet.", Ui.OffText);
        DrawGroove();
    }

    /// <summary>The scroll bar right of the rows. The groove stays when every save fits, so the rows keep their width.</summary>
    private void DrawGroove()
    {
        int count = Shown.Count;
        float top = RowsTop, span = ListBottom - top, x = ListX + ListW - 6;
        DrawRect(new Rect2(x, top, 5, span), Ui.Groove);
        if (count > RowsShown)
        {
            float a = top + span * _scroll / count, b = top + span * Math.Min(count, _scroll + RowsShown) / count;
            DrawRect(new Rect2(x, a, 5, b - a), Ui.Gold);
        }
    }

    private void DrawNewRow()
    {
        var rect = NewRect;
        if (_chosenNew)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
        {
            bool hover = _hovered == NewHit;
            DrawRect(rect, hover ? Ui.RowHover : Ui.RowShade);
            DrawDashed(rect, hover ? Ui.HoverLine : Ui.DimGold);
        }
        float cy = rect.GetCenter().Y;
        Text(_cinzel, 22, rect.Position.X + 18, cy, "+", Ui.Name);
        Text(_cinzel, 15, rect.Position.X + 44, cy, "New save", _chosenNew || _hovered == NewHit ? Ui.Name : Ui.Quiet);
    }

    private void DrawDashed(Rect2 rect, Color colour)
    {
        float w = Line(1);
        for (float x = rect.Position.X; x < rect.End.X; x += 12)
        {
            float x1 = Math.Min(x + 6, rect.End.X);
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x1, rect.Position.Y), colour, w);
            DrawLine(new Vector2(x, rect.End.Y), new Vector2(x1, rect.End.Y), colour, w);
        }
        for (float y = rect.Position.Y; y < rect.End.Y; y += 12)
        {
            float y1 = Math.Min(y + 6, rect.End.Y);
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.Position.X, y1), colour, w);
            DrawLine(new Vector2(rect.End.X, y), new Vector2(rect.End.X, y1), colour, w);
        }
    }

    private void DrawTab(SaveList tab)
    {
        var rect = TabRect(tab);
        bool active = tab == _list, hover = _hovered == TabHit + (int)tab;
        if (active)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
            Ui.DrawRowBox(this, rect, hover, _k);
        TextCentred(_cinzel, 15, rect.GetCenter().X, rect.GetCenter().Y, tab.ToString(), active ? Ui.Name : hover ? Ui.Text : Ui.Quiet);
    }

    /// <summary>
    /// A save's row: the seal, the short date, the time written right; under them the comment at the left, a star on the
    /// seal when there is one, and an auto-save's kind at the right (docs/ui/save-load.md, §12, the owner's K1).
    /// </summary>
    private void DrawRow(Rect2 rect, SaveSlot held, bool auto)
    {
        int place = Rows().ToList().FindIndex(r => r.Id == held.Id);
        bool chosen = held.Id == _chosen && !_chosenNew;
        if (chosen)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
        {
            Ui.DrawRowBox(this, rect, _hovered == RowHit + place, _k);
            if (auto)
                DrawRect(rect, AutoGold, false, Line(1));
        }
        float x0 = rect.Position.X, y0 = rect.Position.Y;
        DrawSeal(new Rect2(x0 + 8, rect.GetCenter().Y - 12, 24, 24), held.Seal, auto ? AutoGold : Ui.Vermilion);
        float tx = x0 + 42;
        Text(_cinzel, 15, tx, y0 + 16, held.ShortDate, Ui.Name);
        TextRight(_gothic, 14, rect.End.X - 10, y0 + 16, held.WrittenShort, Ui.Value);   // the lists' order: the bright line
        float end = auto ? DrawRingTag(rect.End.X - 10, y0 + 37, held, 12) - 14 : rect.End.X - 22;
        if (held.Commented)
        {
            Text(_gothic, 12, tx, y0 + 37, "“" + Fit(held.Comment, 12, end - tx) + "”", Ui.Quiet);
            var corner = new Vector2(x0 + 9, rect.GetCenter().Y - 11);         // the seal's top left (the right is near the date)
            DrawStar(corner, 9, StarRim);
            DrawStar(corner, 7, Ui.Gold);
        }
        else if (_sweep && !auto)
        {
            DrawRect(rect, GoneVeil);                                   // the sweep asking: this row would go
            DrawLine(new Vector2(x0 + 6, rect.GetCenter().Y), new Vector2(rect.End.X - 6, rect.GetCenter().Y), Ui.Vermilion, Line(1.5f));
        }
    }

    /// <summary>A five-pointed star centred at <paramref name="centre"/>, its points at <paramref name="radius"/>.</summary>
    private void DrawStar(Vector2 centre, float radius, Color colour)
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.PI / 5, r = i % 2 == 0 ? radius : radius * 0.45f;
            points[i] = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
        }
        DrawColoredPolygon(points, colour);
    }

    /// <summary>
    /// The sweep in the manual saves' heading: "Delete all without ★ (n)", counting the saves it would delete; off while
    /// every manual save has a comment.
    /// </summary>
    private void DrawSweepButton()
    {
        int count = UncommentedCount;
        bool off = count == 0, hover = _hovered == SweepHit || _sweep;
        if (off)
        {
            DrawRect(SweepRect, Ui.OffButton);
            DrawRect(SweepRect, Ui.OffLine, false, Line(1));
        }
        else
            Ui.DrawRowBox(this, SweepRect, hover, _k);
        const string label = "Delete all without";
        string tail = $"({count})";
        float lw = _gothic.GetStringSize(label, fontSize: 12).X, tw = _gothic.GetStringSize(tail, fontSize: 12).X;
        float x = SweepRect.GetCenter().X - (lw + 22 + tw) / 2, y = SweepRect.GetCenter().Y;
        var colour = off ? Ui.OffText : hover ? Ui.Name : Ui.Text;
        Text(_gothic, 12, x, y, label, colour);
        DrawStar(new Vector2(x + lw + 11, y), 6, off ? Ui.OffText : Ui.Gold);
        Text(_gothic, 12, x + lw + 22, y, tail, colour);
    }

    /// <summary>The sweep's question beside its button, the tip pointing left at it so the list stays in view: Delete n | Keep.</summary>
    private void DrawSweepCallout()
    {
        var shelf = _shelf.Manuals;
        int gone = SaveSlots.Uncommented(shelf).Count;
        Ui.DrawPanel(this, SweepCalloutRect, _k);
        float left = SweepCalloutRect.Position.X, tip = SweepRect.GetCenter().Y;
        DrawColoredPolygon([new(left + 2, tip - 10), new(left + 2, tip + 10), new(left - 12, tip)], Ui.Gold);
        Text(_gothic, 16, left + 22, SweepCalloutRect.Position.Y + 32,
            gone == 1 ? "Delete the manual save without a comment?" : $"Delete the {gone} manual saves without a comment?", Ui.Text);
        int kept = shelf.Count - gone;
        string stay = kept == 0 ? "That is every manual save." : kept == 1 ? "The one with a comment (★) stays." : $"The {kept} with one (★) stay.";
        Text(_gothic, 16, left + 22, SweepCalloutRect.Position.Y + 56, stay + " It cannot be undone.", Ui.Text);
        bool yes = _hovered == SweepYesHit;
        Ui.DrawRedButton(this, SweepYesRect, yes, yes && _pressed == SweepYesHit, _k);
        TextCentred(_brush, 16, SweepYesRect.GetCenter().X, SweepYesRect.GetCenter().Y, $"Delete {gone}", Ui.Name);
        DrawSmallButton(SweepNoRect, "Keep", SweepNoHit);
    }

    private void DrawSeal(Rect2 rect, string text, Color fill)
    {
        DrawRect(rect, fill);
        DrawRect(rect.Grow(-2), Ui.SealText with { A = 0.45f }, false, Line(1));
        TextCentred(_cinzel, text.Length > 2 ? 11 : 14, rect.GetCenter().X, rect.GetCenter().Y + 1, text, Ui.SealText);
    }

    /// <summary>
    /// An auto-save's kind right-aligned at <paramref name="right"/>: "Turn Start" green, "Turn End" in <c>COMMAND</c>'s
    /// Turn END red, each framed in gold as the command panel's tags (the owner's K1); its left edge.
    /// </summary>
    private float DrawRingTag(float right, float middle, SaveSlot held, int size)
    {
        string text = held.RingText;
        float w = _gothic.GetStringSize(text, fontSize: size).X + 14, h = size + 7;
        var rect = new Rect2(right - w, middle - h / 2, w, h);
        DrawRect(rect, held.Kind == SlotKind.TurnEnd ? TurnEndFill : TurnStartFill);
        DrawRect(rect, Ui.Gold, false, Line(1));
        TextCentred(_gothic, size, rect.GetCenter().X, middle, text, TagText);
        return rect.Position.X;
    }

    private void DrawPage(bool save)
    {
        var held = ChosenSave;
        var shown = _chosenNew ? _now : held;
        DrawBack();
        if (shown is null)
        {
            string none = _autoSide ? "No auto-save chosen." : save ? "Choose New save or a save to save over." : "No saved games.";
            TextCentred(_cinzelLight, 18, MapRect.GetCenter().X, MapRect.GetCenter().Y, none, Ui.Quiet);
            return;
        }
        string title = _chosenNew ? $"New save · {shown.DateText}" : held!.Id?.Kind == SaveKind.Auto ? shown.DateText : $"{held.Label} · {shown.DateText}";
        Text(_cinzel, 22, PageX, PageY + 14, title, Ui.Name);
        if (held?.Id?.Kind == SaveKind.Auto)
        {
            float w = _cinzel.GetStringSize(title, fontSize: 22).X;
            DrawRect(new Rect2(PageX + w + 16, PageY + 4, 52, 20), AutoGold);
            TextCentred(_gothic, 12, PageX + w + 42, PageY + 14, "Auto", TagText);
            float x = PageX + w + 76;
            DrawRingTag(x + _gothic.GetStringSize(held.RingText, fontSize: 13).X + 14, PageY + 14, held, 13);
        }
        if (MapOf(shown) is { } map)
            DrawTextureRect(map, MapRect, false);
        else
            DrawRect(MapRect, Colors.Black);
        DrawRect(MapRect, Ui.DimGold, false, Line(1));
        float gy = MapRect.End.Y + 14;
        (string, string)[] stats =
        [
            ("Stage", shown.StageText), ("Budget", shown.BudgetText), ("Realm", shown.RealmText),
            ("Units", shown.Units.ToString()), ("Troops", Figures.Number(shown.Troops)), ("Written", _chosenNew ? "now" : shown.WrittenText),
        ];
        for (int i = 0; i < stats.Length; i++)
        {
            float sx = PageX + i % 3 * 280, sy = gy + i / 3 * 46;
            DrawString(_gothic, new Vector2(sx, sy + _gothic.GetAscent(13)), stats[i].Item1, fontSize: 13, modulate: Ui.Label);
            DrawString(_gothic, new Vector2(sx, sy + 18 + _gothic.GetAscent(17)), stats[i].Item2, fontSize: 17, modulate: Ui.Value);
        }
        float cy = MapRect.End.Y + 114;
        if (_chosenNew)
            Text(_gothic, 15, PageX, cy + 10, "A new manual save. Its comment can be written once it is saved.", Ui.Text);
        else
        {
            DrawComment(held!, cy);
            if (held!.IsAuto && !_editing)                              // what its load does, under the comment
            {
                (string, string) lines = held.Kind == SlotKind.TurnEnd
                    ? ("Written when Turn END was chosen. Loading it brings back the map with the week's commands",
                        "as they were then; Turn END goes on to the nations' turns.")
                    : ("Written at the start of the week. Loading it plays the week's start again:",
                        "the budget, then the commands.");
                Text(_gothic, 14, PageX, cy + 77, lines.Item1, Ui.Quiet);
                Text(_gothic, 14, PageX, cy + 97, lines.Item2, Ui.Quiet);
            }
        }
        float by = ButtonRect.Position.Y, lx = BackRect.End.X + 20, lw = ButtonRect.Position.X - 16 - lx;
        if (save && _now is { } now)
        {
            Text(_gothic, 15, lx, by + 16, Fit($"Saving: {now.DateText}", 15, lw), Ui.Text);
            Text(_gothic, 13, lx, by + 40, Fit($"{now.BudgetText} · {now.RealmText} · {Figures.Number(now.Troops)} troops", 13, lw), Ui.Quiet);
        }
        if (ButtonShown)
            DrawButton(_chosenNew || !save ? (save ? "Save" : "Load") : $"Save over {held!.Label}");
        else if (save)
            DrawOffButton("Save");
        if (held is not null)
            DrawDelete();
    }

    /// <summary>Back at the foot's left, as a right click: a row box with a gold arrow.</summary>
    private void DrawBack()
    {
        bool hover = _hovered == BackHit;
        Ui.DrawRowBox(this, BackRect, hover, _k);
        const string label = "Back";
        float w = _cinzel.GetStringSize(label, fontSize: 16).X + 18, x = BackRect.GetCenter().X - w / 2, y = BackRect.GetCenter().Y;
        DrawColoredPolygon([new(x + 8, y - 6), new(x + 8, y + 6), new(x, y)], Ui.Gold);
        Text(_cinzel, 16, x + 18, y, label, hover ? Ui.Name : Ui.Text);
    }

    /// <summary>Delete as a danger button: dark red lacquer in a vermilion frame, a bin before its label.</summary>
    private void DrawDelete()
    {
        bool hover = _hovered == DeleteHit || _confirm;
        Ui.DrawDangerButton(this, DeleteRect, hover, _k);
        var colour = hover ? Ui.Name : Ui.DangerText;
        const string label = "Delete";
        float w = _cinzel.GetStringSize(label, fontSize: 15).X + 22, x = DeleteRect.GetCenter().X - w / 2, y = DeleteRect.GetCenter().Y;
        DrawBin(x, y - 8, colour);
        Text(_cinzel, 15, x + 22, y + 1, label, colour);
    }

    /// <summary>A small waste bin, 14 wide and 16 high, its top left at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private void DrawBin(float x, float y, Color colour)
    {
        float w = Line(1.5f), thin = Line(1);
        DrawLine(new(x, y + 3), new(x + 14, y + 3), colour, w);
        DrawLine(new(x + 5, y), new(x + 9, y), colour, w);
        DrawPolyline([new(x + 2, y + 5), new(x + 3, y + 16), new(x + 11, y + 16), new(x + 12, y + 5)], colour, w);
        DrawLine(new(x + 5.5f, y + 7.5f), new(x + 5.5f, y + 13.5f), colour, thin);
        DrawLine(new(x + 8.5f, y + 7.5f), new(x + 8.5f, y + 13.5f), colour, thin);
    }

    /// <summary>The manual save's comment on its slip, with Edit; while it is written, the field over the slip (paper).</summary>
    private void DrawComment(SaveSlot held, float y)
    {
        DrawString(_gothic, new Vector2(PageX, y + _gothic.GetAscent(13)), "Comment", fontSize: 13, modulate: Ui.Label);
        if (_editing)
        {
            var box = new Rect2(CommentRect.Position, new Vector2(EditRect.End.X - CommentRect.Position.X, CommentRect.Size.Y));
            DrawRect(box, Ui.Paper);
            DrawRect(box, Ui.Vermilion, false, Line(2));
            TextRight(_gothic, 12, box.End.X, box.End.Y + 14, $"Enter: keep it  ·  Esc: leave it as it was  ·  {CommentLength} letters at most", Ui.Quiet);
            return;
        }
        DrawRect(CommentRect, Ui.Row);
        DrawRect(CommentRect, Ui.DimGold, false, Line(1));
        bool any = held.Comment.Length > 0;
        Text(_gothic, 16, CommentRect.Position.X + 10, CommentRect.GetCenter().Y,
            any ? Fit(held.Comment, 16, CommentRect.Size.X - 20) : "none — Edit writes one", any ? Ui.Value : Ui.Label);
        DrawSmallButton(EditRect, "Edit", EditHit);
    }

    private void DrawSmallButton(Rect2 rect, string text, int hit)
    {
        bool hover = _hovered == hit || (hit == DeleteHit && _confirm);
        Ui.DrawRowBox(this, rect, hover, _k);
        TextCentred(_cinzel, 14, rect.GetCenter().X, rect.GetCenter().Y, text, hover ? Ui.Name : Ui.Text);
    }

    private void DrawButton(string title)
    {
        bool hover = _hovered == ButtonHit;
        Ui.DrawRedButton(this, ButtonRect, hover, hover && _pressed == ButtonHit, _k);
        TextCentred(_brush, 22, ButtonRect.GetCenter().X, ButtonRect.GetCenter().Y, title, Ui.Name);
    }

    /// <summary>The red button off: an auto-save is the game's own, not saved over.</summary>
    private void DrawOffButton(string title)
    {
        DrawRect(ButtonRect, Ui.OffButton);
        DrawRect(ButtonRect, Ui.OffLine, false, Line(2));
        TextCentred(_brush, 22, ButtonRect.GetCenter().X, ButtonRect.GetCenter().Y - 6, title, Ui.OffText);
        TextCentred(_gothic, 12, ButtonRect.GetCenter().X, ButtonRect.GetCenter().Y + 16, "auto-saves are the game's own", Ui.OffText);
    }

    /// <summary>Delete's question in a callout hanging under the button (as the Treasure screen's): Delete | Keep.</summary>
    private void DrawCallout()
    {
        if (ChosenSave is not { } held)
            return;
        Ui.DrawPanel(this, CalloutRect, _k);
        float tip = DeleteRect.GetCenter().X, top = CalloutRect.Position.Y;
        DrawColoredPolygon([new(tip - 10, top + 2), new(tip + 10, top + 2), new(tip, top - 12)], Ui.Gold);
        string what = held.IsAuto ? $"this {held.RingText} auto-save" : $"{held.Label}, {held.DateText}";   // its date is the title's
        Text(_gothic, 16, CalloutRect.Position.X + 22, CalloutRect.Position.Y + 32, Fit($"Delete {what}?", 16, CalloutRect.Size.X - 44), Ui.Text);
        Text(_gothic, 16, CalloutRect.Position.X + 22, CalloutRect.Position.Y + 56, "It cannot be undone.", Ui.Text);
        bool yes = _hovered == DeleteYesHit;
        Ui.DrawRedButton(this, YesRect, yes, yes && _pressed == DeleteYesHit, _k);
        TextCentred(_brush, 16, YesRect.GetCenter().X, YesRect.GetCenter().Y, "Delete", Ui.Name);
        DrawSmallButton(NoRect, "Keep", DeleteNoHit);
    }

    /// <summary>
    /// The save's map as All Data draws it (<c>各種DATA.ADV:1-11</c>): its six tiles at a third, the markers over them at
    /// their own size — here at twice that (1280×533) for the page's large map; cached by tiles and markers.
    /// </summary>
    private ImageTexture? MapOf(SaveSlot slot)
    {
        string key = string.Join(",", slot.Tiles) + "|" + string.Join(";", slot.Marks.Select(m => $"{m.Cg},{m.X},{m.Y}"));
        if (_maps.TryGetValue(key, out var cached))
            return cached;
        ImageTexture? texture = null;
        var full = Image.CreateEmpty(1920, 800, false, Image.Format.Rgba8);
        bool any = false;
        for (int i = 0; i < slot.Tiles.Count; i++)
        {
            if (_pictures.Picture(slot.Tiles[i]) is not { } tile)
                continue;
            any = true;
            full.BlitRect(tile, new Rect2I(0, 0, Math.Min(640, tile.GetWidth()), Math.Min(400, tile.GetHeight())), new Vector2I(i % 3 * 640, i / 3 * 400));
        }
        if (any)
        {
            full.Resize(1280, 533, Image.Interpolation.Lanczos);
            foreach (var mark in slot.Marks)
            {
                if (_pictures.Picture(mark.Cg) is not { } picture)
                    continue;
                var big = (Image)picture.Duplicate();
                big.Resize(picture.GetWidth() * 2, picture.GetHeight() * 2, Image.Interpolation.Nearest);
                full.BlendRect(big, new Rect2I(Vector2I.Zero, big.GetSize()), new Vector2I(mark.X * 2, mark.Y * 2));
            }
            texture = ImageTexture.CreateFromImage(full);
        }
        _maps[key] = texture;
        return texture;
    }
}
