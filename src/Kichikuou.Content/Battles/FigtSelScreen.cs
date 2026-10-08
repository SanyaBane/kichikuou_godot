using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Battles;

/// <summary>Mouse buttons as the engine reports them (<c>IM</c>): 16 left, 32 right.</summary>
public enum MouseButton
{
    /// <summary>Left button (<c>SYS35KEY_RET</c>).</summary>
    Left = 16,

    /// <summary>Right button (<c>SYS35KEY_SPC</c>).</summary>
    Right = 32,
}

/// <summary>
/// <c>FIGT_SEL.ADV</c> on screen: the prompt box, hover frames on the panels, clicks on targets,
/// the slot popup and the command popup (docs/battle/screen-layout.md §4). The script polls the
/// mouse; here the presentation engine feeds mouse events in script coordinates and reads
/// <see cref="Result"/>. Which answers are possible comes from the core's
/// <see cref="FigtSelMenu"/>; an answer the core does not allow is ignored.
/// </summary>
public sealed class FigtSelScreen
{
    private const string Sel = "FIGT_SEL.ADV";

    private readonly BattlePresenter _p;
    private readonly FigtSelMenu _menu;
    private int _mode;              // Tmp0019
    private int _hover = 98;        // Tmp0002: the framed button (98 = fresh, nothing to erase)
    private int _mouseX, _mouseY;
    private bool _leftHeld, _rightHeld;
    private State _state = State.Targeting;
    private SelectionBox? _box;
    private List<(string Text, Func<FigtSelChoice?> Choose)> _items = [];
    private int _popupButton;       // Tmp0001 when the popup was asked for
    private int _popupWidth;

    private enum State
    {
        Targeting,
        SlotPopupPending, SlotPopup, SlotPopupCancelled, SlotPopupCancelledRelease,
        CommandPopupPending, CommandPopup, CommandPopupCancelled, CommandPopupCancelledRelease,
        Done,
    }

    /// <summary>Saves the screen and draws the prompt (<c>FIGT_SEL.ADV:1-64</c>).</summary>
    public FigtSelScreen(BattlePresenter presenter, FigtSelMenu menu)
    {
        _p = presenter;
        _menu = menu;
        _mode = menu.InitialMode.Value;
        var s = _p.Surface;
        s.Copy(0, 0, 640, 400, 0, 400);
        _p.Writer.Open(216, 193, 204, 86);
        DrawPrompt();
    }

    /// <summary>The answer once the player has chosen; the screen is restored by then.</summary>
    public FigtSelChoice? Result { get; private set; }

    /// <summary>The open popup, if any.</summary>
    public SelectionBox? Popup => _state is State.SlotPopup or State.CommandPopup ? _box : null;

    private int FrameColor => _mode >= 10 ? 249 : 255; // Tmp0020

    /// <summary><c>L_001bb</c> (<c>:64-159</c>): the prompt box and its text.</summary>
    private void DrawPrompt()
    {
        var w = _p.Writer;
        _p.G(861, keyed: true);
        w.Home();
        w.Color = 22;
        w.Size = 24;
        if (_menu.AttackType == 0 && _menu.ActingSlot > 2)
        {
            w.Print(_p.Read(new ScriptRef(Sel, 69), []));
            return;
        }
        w.Print(_p.Read(new ScriptRef(Sel, 75), []));
        switch (_mode)
        {
            case 1: w.Print(_p.Read(new ScriptRef(Sel, 79), [])); break;
            case 2: w.Print(_p.Read(new ScriptRef(Sel, 83), [])); break;
            case 3: w.Print(_p.Read(new ScriptRef(Sel, 87), [])); break;
            case > 9:
                w.Print(_p.Read(new ScriptRef(Sel, 91), []));
                w.Color = 249;
                int line = _mode switch { >= 10 and <= 21 => 95 + (_mode - 10) * 4, 30 => 143, 31 => 147, 32 => 151, _ => 0 };
                if (line != 0)
                    w.Print(_p.Read(new ScriptRef(Sel, line), []));
                w.Color = 22;
                w.Print(_p.Read(new ScriptRef(Sel, 155), []));
                break;
        }
        w.Print(_p.Read(new ScriptRef(Sel, 158), []));
    }

    // ------------------------------------------------------------------ input

    /// <summary>The mouse moved (script coordinates).</summary>
    public void MouseMove(int x, int y)
    {
        (_mouseX, _mouseY) = (x, y);
        if (_state == State.Targeting)
            UpdateHover();
        else if (_state is State.SlotPopup or State.CommandPopup)
            _box?.Hover(x, y);
    }

    /// <summary>A button went down.</summary>
    public void MouseDown(MouseButton button, int x, int y)
    {
        MouseMove(x, y);
        if (button == MouseButton.Left)
            _leftHeld = true;
        else
            _rightHeld = true;

        switch (_state)
        {
            case State.Targeting:
                Press(button);
                break;
            case State.SlotPopup or State.CommandPopup:
                if (button == MouseButton.Right)
                    CancelPopup();
                break;
            case State.SlotPopupCancelled:
                _state = button == MouseButton.Right ? State.SlotPopupCancelledRelease : State.SlotPopupPending;
                break;
            case State.CommandPopupCancelled:
                if (button == MouseButton.Right)
                {
                    _state = State.CommandPopupCancelledRelease;
                }
                else
                {
                    // :286-287 — any other button reopens a popup with only the fixed items.
                    BuildFixedItems(withModes: false);
                    _state = State.CommandPopupPending;
                }
                break;
        }
    }

    /// <summary>A button went up.</summary>
    public void MouseUp(MouseButton button, int x, int y)
    {
        MouseMove(x, y);
        bool wasLeft = button == MouseButton.Left;
        if (wasLeft)
            _leftHeld = false;
        else
            _rightHeld = false;
        bool released = !_leftHeld && !_rightHeld;

        switch (_state)
        {
            case State.SlotPopupPending when released:
                OpenSlotPopup();
                break;
            case State.CommandPopupPending when released:
                OpenPopup(State.CommandPopup);
                break;
            case State.SlotPopup or State.CommandPopup when wasLeft:
                int item = _box!.HitTest(x, y);
                if (item >= 0)
                    ChooseItem(item);
                break;
            case State.SlotPopupCancelledRelease when released:
                // :347-353 — restore the panel's frame area, back to targeting.
                RestoreButton(_popupButton);
                BackToTargeting();
                break;
            case State.CommandPopupCancelledRelease when released:
                BackToTargeting();
                break;
        }
    }

    // ------------------------------------------------------------------ targeting (:160-203)

    /// <summary><c>L_00b16</c>: the button under the mouse (strict inequalities), or 99.</summary>
    private int HitTest()
    {
        for (int b = 0; b < 8 && b < _menu.EnemySlotsUsed.Value + 4; b++)
        {
            if (b is >= 1 and <= 3 && _menu.PlayerSlotsUsed.Value < b + 1)
                continue;
            var (x, y, w, h) = BattlePresenter.Panels[b];
            if (_mouseX > x && _mouseY > y && _mouseX < x + w && _mouseY < y + h)
                return b;
        }
        return 99;
    }

    /// <summary>Whether an attack on enemy button <paramref name="b"/> in the current mode is possible (frame not grey).</summary>
    private bool Valid(int b) => _menu.Find((Sys3Int)_mode) is { } option && option.Arguments.Contains((Sys3Int)b);

    /// <summary><c>:163-191</c>: when the hovered button changes, the old frame is erased and a new one drawn.</summary>
    private void UpdateHover()
    {
        int b = HitTest();
        if (b == _hover)
            return;
        if (_hover != 98)
            RestoreButton(_hover);
        if (b != 99)
        {
            int color = b < 4 ? 18 : Valid(b) ? FrameColor : 15;
            var (x, y, w, h) = BattlePresenter.Panels[b];
            var s = _p.Surface;
            s.Fill(x, y, w, 4, color);
            s.Fill(x, y + h - 4, w, 4, color);
            s.Fill(x, y, 4, h, color);
            s.Fill(x + w - 4, y, 4, h, color);
        }
        _hover = b;
    }

    /// <summary><c>CC x1, y1 + 400, w, h, x1, y1</c>; button 99 has an empty rectangle.</summary>
    private void RestoreButton(int b)
    {
        if (b is < 0 or > 7)
            return;
        var (x, y, w, h) = BattlePresenter.Panels[b];
        _p.Surface.Copy(x, y + 400, w, h, x, y);
    }

    private void Press(MouseButton button)
    {
        int b = _hover;
        if (button == MouseButton.Left)
        {
            if (b < 4)
                Finish(new FigtSelChoice(FigtSelCommand.UnitInfo, (Sys3Int)(b + 1)));
            else if (b != 99 && Valid(b))
                Finish(new FigtSelChoice((Sys3Int)_mode, (Sys3Int)b));
            return;
        }

        _popupButton = b;
        if (_mouseX < 199 && b < 4)
        {
            _state = State.SlotPopupPending;
            return;
        }
        BuildFixedItems(withModes: true);
        _state = State.CommandPopupPending;
    }

    // ------------------------------------------------------------------ popups (:204-493)

    private void OpenSlotPopup()
    {
        int b = _popupButton;
        _items =
        [
            (Label(323), () => { RestoreButton(b); return new FigtSelChoice(FigtSelCommand.OrderManual, (Sys3Int)(b + 1)); }),
            (Label(327), () => { RestoreButton(b); return new FigtSelChoice(FigtSelCommand.OrderAttack, (Sys3Int)(b + 1)); }),
            (Label(331), () => { RestoreButton(b); return new FigtSelChoice(FigtSelCommand.OrderDefend, (Sys3Int)(b + 1)); }),
            (Label(335), () => new FigtSelChoice(FigtSelCommand.UnitInfo, (Sys3Int)(b + 1))),
        ];
        _popupWidth = 68;
        OpenPopup(State.SlotPopup, yLimit: 310);
    }

    /// <summary>The command popup's items: the mode entries (<c>L_00bc0</c>) on the first opening, then the fixed ones.</summary>
    private void BuildFixedItems(bool withModes)
    {
        _items = [];
        int hovered = _popupButton;
        if (withModes)
        {
            _popupWidth = 68;
            _modeEntries = 0;
            var listed = _menu.ListedModes ?? _menu.Options.Select(o => o.Command).Where(IsModeEntry).ToList();
            foreach (var entry in listed)
            {
                int mode = entry.Value;
                if (!ShowsModeEntry(mode))
                    continue;
                var (line, cells) = ModeEntry(mode);
                _popupWidth = 16 * cells;
                _modeEntries++;
                if (mode is >= 30 and <= 32)
                    _items.Add((Label(line), () => new FigtSelChoice((Sys3Int)mode, (Sys3Int)hovered)));
                else
                    _items.Add((Label(line), () => SwitchMode(mode)));
            }
        }
        if (_menu.Find(FigtSelCommand.CheaterBeam) is not null)
            _items.Add((Label(244), () => SwitchMode(FigtSelCommand.CheaterBeam)));
        AddReturn(249, FigtSelCommand.Defend);
        AddReturn(253, FigtSelCommand.Wait);
        AddReturn(257, FigtSelCommand.AutoAttack);
        AddReturn(261, FigtSelCommand.AutoDefend);
        if (_hover != 99)
            _items.Add((Label(266), () => new FigtSelChoice(FigtSelCommand.EnemyInfo, (Sys3Int)(_hover + 1) - 4)));
        AddReturn(271, FigtSelCommand.Retreat);
        if (_menu.Find(FigtSelCommand.Repel) is not null)
            AddReturn(276, FigtSelCommand.Repel);

        void AddReturn(int line, ushort command) =>
            _items.Add((Label(line), () => new FigtSelChoice(command, (Sys3Int)_popupButton)));
    }

    private static bool IsModeEntry(Sys3Int command) =>
        command.Value is (>= 1 and <= 3) or (>= 11 and <= 21) or (>= 30 and <= 32);

    /// <summary><c>L_00bc0</c>: a mode is listed unless it is current; the specials only while the mode is 1-3.</summary>
    private bool ShowsModeEntry(int mode) => mode switch
    {
        >= 1 and <= 3 => mode != _mode,
        >= 11 and <= 21 => _mode <= 3,
        _ => true,
    };

    /// <summary>The label line of each popup entry and its width in 16-pixel cells (<c>Tmp0014</c>).</summary>
    private static (int Line, int Cells) ModeEntry(int mode) => mode switch
    {
        1 => (525, 4), 2 => (534, 4), 3 => (543, 4),
        30 => (552, 6), 31 => (561, 6), 32 => (570, 4),
        11 => (583, 7), 12 => (592, 7), 14 => (602, 6), 13 => (611, 6), 17 => (629, 6),
        15 => (647, 7), 18 => (656, 4), 16 => (665, 6), 19 => (674, 5), 21 => (683, 5), 20 => (692, 5),
        _ => (0, 4),
    };

    private string Label(int line) => _p.Read(new ScriptRef(Sel, line), []);

    /// <summary>
    /// Opens the popup at the right press: (mouse − (16, 10)) clamped to x ∈ [10, 632 − width] and
    /// y ∈ [10, <paramref name="yLimit"/>] (<c>:208-233</c> / <c>:297-313</c>).
    /// </summary>
    private void OpenPopup(State state, int? yLimit = null)
    {
        // Tmp0015: 20 per mode entry, 40 in debug mode, 20 for Info.
        int extra = 20 * _modeEntries + (_menu.Find(FigtSelCommand.Repel) is not null ? 40 : 0) + (_hover != 99 ? 20 : 0);
        int x = Math.Clamp(_mouseX - 16, 10, Math.Max(10, 632 - _popupWidth));
        int y = Math.Clamp(_mouseY - 10, 10, Math.Max(10, yLimit ?? 290 - extra));
        _box = new SelectionBox(_p.Surface, _p.Glyphs, x, y, _popupWidth, _items.Select(i => i.Text).ToList());
        _box.Open();
        _box.Hover(_mouseX, _mouseY);
        _state = state;
    }

    private int _modeEntries;

    private void ChooseItem(int item)
    {
        var choose = _items[item].Choose;
        _box!.Close();
        _box = null;
        if (choose() is not { } choice)
            return; // a mode switch, already back to targeting
        if (_menu.Allows(choice))
            Finish(choice);
        else
            BackToTargeting(); // e.g. the slot popup reopened after a cancel answers slot 100
    }

    /// <summary><c>L_0099e</c>-<c>L_00aa8</c>: a new mode and frame colour, the 98 marker, the prompt redrawn.</summary>
    private FigtSelChoice? SwitchMode(int mode)
    {
        _mode = mode;
        _hover = 98;
        DrawPrompt();
        BackToTargeting();
        return null;
    }

    private void CancelPopup()
    {
        _box?.Close();
        _box = null;
        _popupButton = 99; // :280 / :338 — Tmp0001 := 99
        _state = _state == State.SlotPopup ? State.SlotPopupCancelled : State.CommandPopupCancelled;
    }

    private void BackToTargeting()
    {
        _state = State.Targeting;
        UpdateHover();
    }

    private void Finish(FigtSelChoice choice)
    {
        if (!_menu.Allows(choice))
            return;
        _p.Surface.Copy(0, 400, 640, 400, 0, 0); // L_00af4
        Result = choice;
        _state = State.Done;
    }
}
