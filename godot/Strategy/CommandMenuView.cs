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
/// In the touch mode (the owner's A, 2026-10-25, docs/ui/touch.md, "The command panel within the thumbs' reach") the rows
/// are a finger's height on two rails in the sides' lower halves, as the battle's: the other items left, the week's commands
/// right under their caption (owner, 2026-10-30: at the right thumb), each from the foot up, Turn END on the right rail's top;
/// the map's button, reading Close, stands at the right rail's foot. A tap off the rails cancels, as the button does (owner,
/// 2026-10-24: a touch screen has no right button).
/// On the phone held landscape the rails are the black sides (docs/ui/phone.md, §12, LM1b; <c>CommandMenuView.Phone.cs</c>).
/// </summary>
internal sealed partial class CommandMenuView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Map);

    // The PC's layout: the owner's M1 (2026-10-25, docs/ui/command-screen.md, "Taller rows"), every row 38 high unless
    // Debug → Menu Row Height says otherwise (MenuRowsChoice).
    private const float Pad = 18;
    // The touch mode's layout: rails 244 wide at the picture's sides down to its foot, rows 220×46 (45 × 9.3 mm on the
    // tablet) 54 apart; the lowest item's top at 732, the lowest command's at 656 over the Close button (CommandsButton:
    // 220×72 at 1044, 712); Turn END 62 high, 46 over the commands: the caption 8 under it, a divider 34, the first row 46.
    private const float RailW = 244, LeftRail = 4, RightRail = 1032, RailFoot = 796, RailInset = 12;
    private const float TouchRowW = 220, TouchRowH = 46, TouchPitch = 54, TouchEndH = 62;
    private const float LinkFoot = 732, CommandFoot = 656, RailHead = 46;

    private static bool Touch => Device.TouchLayout;                // the tablet's layout, read live (docs/ui/phone-build.md, §3)

    private static MenuRows Rows => MenuRowsChoice.Rows;
    private static float Width => MathF.Max(360, MathF.Round(360 * Rows.Scale * 0.95f));
    private static float RowPitch => Rows.Pitch;
    private static float RowH => Rows.Height;
    private static float LinkPitch => Rows.Height + Math.Max(2, Rows.Gap - 3);
    private static float LinkH => Rows.Height;
    private static float EndH => Rows.Height + 18;

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private const int CaptionSize = 13;
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

    // An open panel follows Debug → Menu Row Height at once.
    public override void _EnterTree() => MenuRowsChoice.Changed += QueueRedraw;

    public override void _ExitTree() => MenuRowsChoice.Changed -= QueueRedraw;

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
    public Vector2 OutsidePoint => _phone is { } f ? PhoneOutsidePoint(f)
        : GetGlobalRect().Position + (Touch ? new Vector2(640, 600) : new Vector2(40, 700)) * _k;

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
        if (PlacePhone())
            return;
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

    private float LinksTop => CommandsTop + Commands * RowPitch + 14;

    private float EndTop => LinksTop + Links * LinkPitch + 10;

    private float PanelWidth => Width;

    private float Height => EndTop + EndH + 16;

    /// <summary>The panel's corner: centred on the screen, as the original's menu (owner, 2026-10-16).</summary>
    private float PanelX => MathF.Round((1280 - PanelWidth) / 2);

    private float PanelY => MathF.Round((800 - Height) / 2);

    private float Line(float width) => Lacquer.Line(width, _k);

    /// <summary>The touch mode's top item on the left rail (the highest of the items, from the foot up).</summary>
    private float TopLink => LinkFoot - Math.Max(0, Links - 1) * TouchPitch;

    /// <summary>The touch mode's Turn END on the right rail's top, over the commands' caption.</summary>
    private float TurnEndTop => CommandFoot - Math.Max(0, Commands - 1) * TouchPitch - RailHead - TouchEndH;

    /// <summary>The touch mode's left rail: the other items.</summary>
    private Rect2 LeftRailRect
    {
        get
        {
            float top = TopLink - RailInset;
            return new Rect2(LeftRail, top, RailW, RailFoot - top);
        }
    }

    /// <summary>The touch mode's right rail: Turn END, the week's commands under their caption, the map's button at its foot.</summary>
    private Rect2 RightRailRect
    {
        get
        {
            float top = TurnEndTop - RailInset;
            return new Rect2(RightRail, top, RailW, RailFoot - top);
        }
    }

    private Rect2 RowRect(int row)
    {
        float x = PanelX, y = PanelY;
        if (Touch)
        {
            // From the foot up, as the battle's rails: the first command lowest on the right, the first item lowest on the left.
            if (row < Commands)
                return new Rect2(RightRail + RailInset, CommandFoot - row * TouchPitch, TouchRowW, TouchRowH);
            if (row < Commands + Links)
                return new Rect2(LeftRail + RailInset, LinkFoot - (row - Commands) * TouchPitch, TouchRowW, TouchRowH);
            return new Rect2(RightRail + RailInset, TurnEndTop, TouchRowW, TouchEndH);
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
        if (_phone is { } f)
            return PhoneRowAt(f, window);
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
                var point = button.Position / _k;
                bool outside = _phone is not null ? at < 0 : Touch && !LeftRailRect.HasPoint(point) && !RightRailRect.HasPoint(point);
                if (button.Pressed)
                    (_pressed, _outside) = (at, outside);
                else if (at >= 0 && at == _pressed && Enabled(at))
                    Answer(Choice(at));
                else if (outside && _outside)
                    Answer((0, null));                      // a tap off the panel (the rails): back to the map
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
        if (_phone is { } phone)
        {
            DrawPhone(phone);
            return;
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float x = PanelX, y = PanelY;
        if (Touch)
            DrawRails();
        else
            Ui.DrawPanel(this, new Rect2(x, y, PanelWidth, Height), _k);
        for (int r = 0; r < Commands; r++)
            DrawCommand(r);
        if (!Touch)
            Ui.DrawDivider(this, x + 16, x + Width - 16, y + LinksTop - 7, _k);
        for (int r = Commands; r < Commands + Links; r++)
            DrawLink(r);
        DrawTurnEnd();
    }

    /// <summary>
    /// The touch mode's two rails (the owner's A): the left one with the other items; the right one's Turn END, then the
    /// caption over a divider and the week's commands; the map's button draws itself, as Close, at the right rail's foot.
    /// </summary>
    private void DrawRails()
    {
        var right = RightRailRect;
        Ui.DrawPanel(this, LeftRailRect, _k);
        Ui.DrawPanel(this, right, _k);
        const string caption = "THIS WEEK";
        float head = TurnEndTop + TouchEndH;
        float cw = _cinzel.GetStringSize(caption, fontSize: CaptionSize).X;
        DrawString(_cinzel, new Vector2(right.GetCenter().X - cw / 2, Lacquer.Baseline(new Rect2(right.Position.X, head + 8, RailW, 20), _cinzel, CaptionSize)),
            caption, fontSize: CaptionSize, modulate: Ui.Subtitle);
        Ui.DrawDivider(this, right.Position.X + 16, right.End.X - 16, head + 34, _k);
    }

    private void DrawCommand(int r)
    {
        var item = _menu!.Commands[r];
        var rect = RowRect(r);
        bool on = item.Enabled, hover = r == _hovered && on;
        Ui.DrawRowBox(this, rect, hover, _k);
        var (tag, colour) = TagOf(item.Action);
        int size = Touch ? 19 : Rows.Text;
        var tagRect = Touch
            ? new Rect2(rect.Position.X + 7, rect.Position.Y + 8, 72, rect.Size.Y - 16)
            : new Rect2(rect.Position.X + Rows.TagInset, rect.Position.Y + Rows.TagInset, Rows.TagWidth, rect.Size.Y - 2 * Rows.TagInset);
        // A building under way (the owner's A, 2026-10-25): the tag half-lit, the weeks left where a spent row says so; the
        // row answers as ever (○ opens Cﾃｲﾄ for Maris's refusal, × does nothing).
        // The touch mode's rail has no room for the note: the weeks go on the tag's chip, as the strip's, and a spent row is
        // only dimmed.
        bool busy = item.WeeksText is not null;
        if (busy)
            Ui.DrawBusyTag(this, _gothic, tagRect, tag, colour, _k, Touch ? 13 : Rows.TagText, Touch ? item.WeeksLeft : null);
        else
            Ui.DrawTag(this, _gothic, tagRect, tag, colour, on, _k, Touch ? 13 : Rows.TagText);
        float textX = rect.Position.X + (Touch ? 94 : Rows.TextLeft);
        while (Touch && size > 14 && textX + _gothic.GetStringSize(item.Text, fontSize: size).X > rect.End.X - 8)
            size--;                                                     // a long name shrinks to fit the rail's row
        DrawString(_gothic, new Vector2(textX, Lacquer.Baseline(rect, _gothic, size)), item.Text, fontSize: size,
            modulate: busy ? Ui.Quiet : on ? Ui.Text : Ui.OffText);
        if (!Touch && (busy || !on))
        {
            string note = item.WeeksText ?? "used this week";
            int noteSize = Rows.Small;
            float sw = _gothic.GetStringSize(note, fontSize: noteSize).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, Lacquer.Baseline(rect, _gothic, noteSize)), note, fontSize: noteSize,
                modulate: busy ? Ui.Quiet : Ui.OffText);
        }
    }

    private void DrawLink(int r)
    {
        var rect = RowRect(r);
        bool hover = r == _hovered;
        // On touch every row has its box: no hover shows that it can be tapped.
        if (hover || Touch)
            Ui.DrawRowBox(this, rect, hover, _k);
        int size = Touch ? 18 : Rows.Text - 1;
        DrawString(_gothic, new Vector2(rect.Position.X + (Touch ? 14 : 10), Lacquer.Baseline(rect, _gothic, size)), _menu!.Links[r - Commands].Text,
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
        int size = Touch ? 24 : Rows.Text + 5;
        float tw = _brush.GetStringSize(text, fontSize: size).X;
        DrawString(_brush, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, _brush, size)), text, fontSize: size,
            modulate: Color.Color8(250, 226, 160));
    }

    /// <summary>A command's tag and its colour, the week's use it spends (ours: the original's menus have none; <see cref="TagColours"/>).</summary>
    private static (string Tag, Color Colour) TagOf(CommandAction action) => (action switch
    {
        CommandAction.Build => "Build",
        CommandAction.Harem => "Harem",
        CommandAction.Items => "Items",
        CommandAction.Trade => "Trade",
        _ => "Units",
    }, TagColours.Of(action));
}
