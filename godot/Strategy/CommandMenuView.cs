using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our command panel (docs/ui/command-screen.md, "The command panel"; the owner's mock-up A, 2026-10-16):
/// <c>COMMAND.ADV</c>'s command menu — the right click on the map — as a lacquer column in the screen's
/// centre, where the original's menu opens: the nation with its flag, the date, the status window's two
/// figures, the commands as rows with a tag and their uses left this week (diamonds), the other items, and
/// Turn END. A row's click answers the script's menu with its choice; a right click or Esc anywhere cancels
/// (0, back to the map), as the original's menu. Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class CommandMenuView : Control
{
    private const float Width = 320, Pad = 18, RowPitch = 37, RowH = 30, LinkPitch = 28, LinkH = 26, EndH = 48;

    /// <summary>The flags' CGs: <c>Prov.Owner + 819</c>, Leazas 820 (the map's markers).</summary>
    private const int LeazasFlag = 820;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private FontFile _gothic;
    private CommandMenu? _menu;
    private ImageTexture? _flag;
    private float _k = 1;                           // window pixels to a screen pixel
    private int _hovered = -1, _pressed = -1;       // the row: 0-based, the commands, the links, then Turn END
    private TaskCompletionSource<int>? _answer;

    public CommandMenuView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
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
        _flag = !menu.Thieves && _pictures.Picture(LeazasFlag) is { } image ? ImageTexture.CreateFromImage(image) : null;
        _hovered = _pressed = -1;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>The script's answer: a row's choice, or 0 for a cancel.</summary>
    public Task<int> ChooseAsync()
    {
        _answer = new TaskCompletionSource<int>();
        return _answer.Task;
    }

    public void Close()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _menu = null;
        _flag = null;
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

    private const float StatusTop = Pad + 76, CommandsTop = StatusTop + 2 * 24 + 18;

    private float LinksTop => CommandsTop + Commands * RowPitch + 14;

    private float EndTop => LinksTop + Links * LinkPitch + 10;

    private float Height => EndTop + EndH + 16;

    /// <summary>The panel's corner: centred on the screen, as the original's menu (owner, 2026-10-16).</summary>
    private float PanelX => MathF.Round((1280 - Width) / 2);

    private float PanelY => MathF.Round((800 - Height) / 2);

    private float Line(float width) => Lacquer.Line(width, _k);

    private Rect2 RowRect(int row)
    {
        float x = PanelX, y = PanelY;
        if (row < Commands)
            return new Rect2(x + 14, y + CommandsTop + row * RowPitch, Width - 28, RowH);
        if (row < Commands + Links)
            return new Rect2(x + 14, y + LinksTop + (row - Commands) * LinkPitch, Width - 28, LinkH);
        return new Rect2(x + 14, y + EndTop, Width - 28, EndH);
    }

    private bool Enabled(int row) => _menu is { } m && (row >= Commands || m.Commands[row].Enabled);

    private int Choice(int row) => _menu is not { } m ? 0
        : row < Commands ? m.Commands[row].Choice
        : row < Commands + Links ? m.Links[row - Commands].Choice
        : m.TurnEnd;

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
                Answer(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = RowAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at >= 0 && at == _pressed && Enabled(at))
                    Answer(Choice(at));
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
            Answer(0);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Answer(int choice)
    {
        var answer = _answer;
        _answer = null;
        answer?.TrySetResult(choice);
    }

    public override void _Draw()
    {
        if (_menu is not { } m)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float x = PanelX, y = PanelY;
        Lacquer.DrawPanel(this, new Rect2(x, y, Width, Height), _k);
        // The nation with its flag, the date.
        float nx = x + Pad + 2;
        if (_flag is not null)
        {
            DrawTextureRect(_flag, new Rect2(nx, y + Pad + 4, 30, 24), false);
            nx += 40;
        }
        int nameSize = 22;
        while (nameSize > 12 && _brush.GetStringSize(m.Nation, fontSize: nameSize).X > x + Width - Pad - nx)
            nameSize--;
        DrawString(_brush, new Vector2(nx, y + Pad + _brush.GetAscent(nameSize)), m.Nation, fontSize: nameSize, modulate: Lacquer.Name);
        DrawString(_cinzel, new Vector2(x + Pad + 1, y + Pad + 38 + _cinzel.GetAscent(13)), m.Date, fontSize: 13, modulate: Lacquer.Subtitle);
        Lacquer.DrawDivider(this, x + 16, x + Width - 16, y + StatusTop - 8, _k);
        // The status window's figures.
        float sy = y + StatusTop;
        foreach (var (label, value) in m.Status)
        {
            DrawString(_gothic, new Vector2(x + 20, sy + _gothic.GetAscent(14)), label, fontSize: 14, modulate: Lacquer.Label);
            float vw = _gothic.GetStringSize(value, fontSize: 16).X;
            DrawString(_gothic, new Vector2(x + Width - 20 - vw, sy + _gothic.GetAscent(16) - 1), value, fontSize: 16, modulate: Lacquer.Value);
            sy += 24;
        }
        Lacquer.DrawDivider(this, x + 16, x + Width - 16, y + CommandsTop - 9, _k);
        for (int r = 0; r < Commands; r++)
            DrawCommand(r);
        Lacquer.DrawDivider(this, x + 16, x + Width - 16, y + LinksTop - 7, _k);
        for (int r = Commands; r < Commands + Links; r++)
            DrawLink(r);
        DrawTurnEnd();
    }

    private void DrawCommand(int r)
    {
        var item = _menu!.Commands[r];
        var rect = RowRect(r);
        bool on = item.Enabled, hover = r == _hovered && on;
        Lacquer.DrawRowBox(this, rect, hover, _k);
        var (tag, colour) = TagOf(item.Action);
        Lacquer.DrawTag(this, _gothic, new Rect2(rect.Position.X + 5, rect.Position.Y + 5, 60, rect.Size.Y - 10), tag, colour, on, _k);
        DrawString(_gothic, new Vector2(rect.Position.X + 78, Lacquer.Baseline(rect, _gothic, 16)), item.Text, fontSize: 16,
            modulate: on ? Lacquer.Text : Lacquer.OffText);
        if (!on)
        {
            const string spent = "used this week";
            float sw = _gothic.GetStringSize(spent, fontSize: 12).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, Lacquer.Baseline(rect, _gothic, 12)), spent, fontSize: 12, modulate: Lacquer.OffText);
            return;
        }
        // The uses left this week: gold diamonds, the spent ones dim.
        float cy = rect.GetCenter().Y;
        for (int i = 0; i < item.Total; i++)
        {
            float cx = rect.End.X - 14 - (item.Total - 1 - i) * 14;
            DrawColoredPolygon([new(cx, cy - 5), new(cx + 5, cy), new(cx, cy + 5), new(cx - 5, cy)], i < item.Left ? Lacquer.Gold : Lacquer.SpentPip);
        }
    }

    private void DrawLink(int r)
    {
        var rect = RowRect(r);
        bool hover = r == _hovered;
        if (hover)
            Lacquer.DrawRowBox(this, rect, true, _k);
        DrawString(_gothic, new Vector2(rect.Position.X + 10, Lacquer.Baseline(rect, _gothic, 15)), _menu!.Links[r - Commands].Text, fontSize: 15,
            modulate: hover ? Lacquer.Text : Lacquer.Quiet);
    }

    private void DrawTurnEnd()
    {
        int r = RowCount - 1;
        var rect = RowRect(r);
        bool hover = r == _hovered;
        DrawRect(rect, hover ? Color.Color8(146, 40, 30) : Color.Color8(118, 30, 24));
        DrawRect(rect, hover ? Lacquer.HoverLine : Lacquer.Gold, false, Line(2));
        DrawRect(rect.Grow(-4), Color.Color8(214, 150, 90), false, Line(1));
        string text = _menu!.TurnEndText;
        float tw = _brush.GetStringSize(text, fontSize: 21).X;
        DrawString(_brush, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, _brush, 21)), text, fontSize: 21,
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
