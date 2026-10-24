using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our command panel (docs/ui/command-screen.md, "The command panel"; the owner's mock-up A, 2026-10-16):
/// <c>COMMAND.ADV</c>'s command menu — the right click on the map — as a lacquer column in the screen's
/// centre, where the original's menu opens: the commands as rows with a tag, the other items, and Turn END;
/// the status window and the week's uses are on our status strip (<see cref="StatusStripView"/>). A row's click answers the script's menu with its choice; a right click or Esc anywhere cancels
/// (0, back to the map), as the original's menu. Load/Save is two rows, Load and Save, the list each opens answered
/// with the choice (<see cref="LoadSaveShortcut"/>). Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// In the touch mode (owner's choice C2, docs/ui/touch.md, "The command panel and the province menu") the rows are a
/// finger's height and the two lists stand side by side, the commands left, the other items right with Turn END under them;
/// a tap outside the panel cancels, as the map's button does while it reads Close (owner, 2026-10-24: a touch screen has
/// no right button).
/// </summary>
internal sealed partial class CommandMenuView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Map);

    private const float Width = 320, Pad = 18, RowPitch = 37, RowH = 30, LinkPitch = 28, LinkH = 26, EndH = 48;
    // The touch mode's layout: two columns of rows 46 high (9.3 mm on the tablet), Turn END under the right one.
    private const float TouchColumn = 300, TouchMid = 24, TouchRowH = 46, TouchPitch = 54, TouchEndGap = 14, TouchEndH = 64;

    private readonly bool _touch = TouchMode.On;

    private readonly FontFile _brush = Lacquer.Brush();
    private FontFile _gothic;
    private CommandMenu? _menu;
    private float _k = 1;                           // window pixels to a screen pixel
    private int _hovered = -1, _pressed = -1;       // the row: 0-based, the commands, the links, then Turn END
    private bool _outside;                          // the touch mode: the press came down outside the panel
    private TaskCompletionSource<(int Choice, SaveList? Opens)>? _answer;

    public CommandMenuView(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>Shows <paramref name="menu"/>; <see cref="ChooseAsync"/> waits for its answer.</summary>
    public void Open(CommandMenu menu)
    {
        _menu = menu;
        _hovered = _pressed = -1;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>The script's answer: a row's choice, or 0 for a cancel; with the list a Load or Save row opens.</summary>
    public Task<(int Choice, SaveList? Opens)> ChooseAsync()
    {
        _answer = new TaskCompletionSource<(int, SaveList?)>();
        return _answer.Task;
    }

    /// <summary>Cancels the open panel (0, back to the map), as a right click does: the touch mode's Close.</summary>
    public void Cancel()
    {
        if (_answer is not null)
            Answer((0, null));
    }

    /// <summary>A point of the picture outside the panel, global: where unattended play taps to close it.</summary>
    public Vector2 OutsidePoint => GetGlobalRect().Position + new Vector2(40, 700) * _k;

    public void Close()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _menu = null;
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
        QueueRedraw();
    }

    private int Commands => _menu?.Commands.Count ?? 0;

    private int Links => _menu?.Links.Count ?? 0;

    private int RowCount => _menu is null ? 0 : Commands + Links + 1;

    private const float CommandsTop = Pad;

    private float LinksTop => _touch ? Pad : CommandsTop + Commands * RowPitch + 14;

    private float EndTop => _touch ? Pad + TouchColumnHeight(Links) + TouchEndGap : LinksTop + Links * LinkPitch + 10;

    /// <summary>The height of <paramref name="rows"/> touch rows one under another.</summary>
    private static float TouchColumnHeight(int rows) => rows == 0 ? 0 : (rows - 1) * TouchPitch + TouchRowH;

    private float PanelWidth => _touch ? Pad + TouchColumn + TouchMid + TouchColumn + Pad : Width;

    private float Height => _touch
        ? Math.Max(Pad + TouchColumnHeight(Commands), EndTop + TouchEndH) + Pad
        : EndTop + EndH + 16;

    /// <summary>The panel's corner: centred on the screen, as the original's menu (owner, 2026-10-16).</summary>
    private float PanelX => MathF.Round((1280 - PanelWidth) / 2);

    private float PanelY => MathF.Round((800 - Height) / 2);

    private float Line(float width) => Lacquer.Line(width, _k);

    private Rect2 RowRect(int row)
    {
        float x = PanelX, y = PanelY;
        if (_touch)
        {
            float right = x + Pad + TouchColumn + TouchMid;
            if (row < Commands)
                return new Rect2(x + Pad, y + Pad + row * TouchPitch, TouchColumn, TouchRowH);
            if (row < Commands + Links)
                return new Rect2(right, y + Pad + (row - Commands) * TouchPitch, TouchColumn, TouchRowH);
            return new Rect2(right, y + EndTop, TouchColumn, TouchEndH);
        }
        if (row < Commands)
            return new Rect2(x + 14, y + CommandsTop + row * RowPitch, Width - 28, RowH);
        if (row < Commands + Links)
            return new Rect2(x + 14, y + LinksTop + (row - Commands) * LinkPitch, Width - 28, LinkH);
        return new Rect2(x + 14, y + EndTop, Width - 28, EndH);
    }

    private bool Enabled(int row) => _menu is { } m && (row >= Commands || m.Commands[row].Enabled);

    private (int, SaveList?) Choice(int row) => _menu is not { } m ? (0, null)
        : row < Commands ? (m.Commands[row].Choice, null)
        : row < Commands + Links ? (m.Links[row - Commands].Choice, m.Links[row - Commands].Opens)
        : (m.TurnEnd, null);

    private int RowAt(Vector2 window)
    {
        var at = window / _k;
        for (int r = 0; r < RowCount; r++)
            if (RowRect(r).HasPoint(at))
                return r;
        return -1;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
            return;
        switch (e)
        {
            case InputEventMouseMotion motion:
                int row = RowAt(motion.Position);
                if (row != _hovered)
                {
                    _hovered = row;
                    MouseDefaultCursorShape = row >= 0 && Enabled(row) ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Answer((0, null));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = RowAt(button.Position);
                bool outside = _touch && !new Rect2(PanelX, PanelY, PanelWidth, Height).HasPoint(button.Position / _k);
                if (button.Pressed)
                    (_pressed, _outside) = (at, outside);
                else if (at >= 0 && at == _pressed && Enabled(at))
                    Answer(Choice(at));
                else if (outside && _outside)
                    Answer((0, null));                      // a tap outside the panel: back to the map
                else
                    _pressed = -1;
                break;
        }
        AcceptEvent();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (_answer is not null && e is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            Answer((0, null));
            GetViewport().SetInputAsHandled();
        }
    }

    private void Answer((int, SaveList?) choice)
    {
        var answer = _answer;
        _answer = null;
        answer?.TrySetResult(choice);
    }

    public override void _Draw()
    {
        if (_menu is null)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float x = PanelX, y = PanelY;
        Ui.DrawPanel(this, new Rect2(x, y, PanelWidth, Height), _k);
        for (int r = 0; r < Commands; r++)
            DrawCommand(r);
        if (_touch)
            Ui.DrawVerticalDivider(this, x + Pad + TouchColumn + TouchMid / 2, y + Pad + 4, y + Height - Pad - 4, _k);
        else
            Ui.DrawDivider(this, x + 16, x + Width - 16, y + LinksTop - 7, _k);
        for (int r = Commands; r < Commands + Links; r++)
            DrawLink(r);
        DrawTurnEnd();
    }

    private void DrawCommand(int r)
    {
        var item = _menu!.Commands[r];
        var rect = RowRect(r);
        bool on = item.Enabled, hover = r == _hovered && on;
        Ui.DrawRowBox(this, rect, hover, _k);
        var (tag, colour) = TagOf(item.Action);
        int size = _touch ? 19 : 16;
        var tagRect = _touch
            ? new Rect2(rect.Position.X + 7, rect.Position.Y + 8, 72, rect.Size.Y - 16)
            : new Rect2(rect.Position.X + 5, rect.Position.Y + 5, 60, rect.Size.Y - 10);
        Ui.DrawTag(this, _gothic, tagRect, tag, colour, on, _k);
        DrawString(_gothic, new Vector2(rect.Position.X + (_touch ? 94 : 78), Lacquer.Baseline(rect, _gothic, size)), item.Text, fontSize: size,
            modulate: on ? Ui.Text : Ui.OffText);
        if (!on)
        {
            const string spent = "used this week";
            float sw = _gothic.GetStringSize(spent, fontSize: 12).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, Lacquer.Baseline(rect, _gothic, 12)), spent, fontSize: 12, modulate: Ui.OffText);
        }
    }

    private void DrawLink(int r)
    {
        var rect = RowRect(r);
        bool hover = r == _hovered;
        // On touch every row has its box: no hover shows that it can be tapped.
        if (hover || _touch)
            Ui.DrawRowBox(this, rect, hover, _k);
        int size = _touch ? 18 : 15;
        DrawString(_gothic, new Vector2(rect.Position.X + (_touch ? 14 : 10), Lacquer.Baseline(rect, _gothic, size)), _menu!.Links[r - Commands].Text,
            fontSize: size, modulate: hover ? Ui.Text : Ui.Quiet);
    }

    private void DrawTurnEnd()
    {
        int r = RowCount - 1;
        var rect = RowRect(r);
        bool hover = r == _hovered;
        DrawRect(rect, hover ? Color.Color8(146, 40, 30) : Color.Color8(118, 30, 24));
        DrawRect(rect, hover ? Ui.HoverLine : Ui.Gold, false, Line(2));
        DrawRect(rect.Grow(-4), Color.Color8(214, 150, 90), false, Line(1));
        string text = _menu!.TurnEndText;
        int size = _touch ? 24 : 21;
        float tw = _brush.GetStringSize(text, fontSize: size).X;
        DrawString(_brush, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, _brush, size)), text, fontSize: size,
            modulate: Color.Color8(250, 226, 160));
    }

    /// <summary>A command's tag and its lacquer colour (ours: the original's menus have none).</summary>
    private static (string Tag, Color Colour) TagOf(CommandAction action) => action switch
    {
        CommandAction.Build => ("Build", Color.Color8(46, 62, 104)),
        CommandAction.Harem => ("Harem", Color.Color8(128, 44, 70)),
        CommandAction.Items => ("Items", Color.Color8(138, 104, 34)),
        CommandAction.Trade => ("Trade", Color.Color8(36, 92, 96)),
        _ => ("Units", Color.Color8(70, 96, 52)),
    };
}
