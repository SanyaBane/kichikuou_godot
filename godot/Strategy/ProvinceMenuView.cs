using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our province menu (docs/ui/command-screen.md, "The province menu"; the owner's mock-up 4a): a city's
/// first menu as a lacquer panel with a double gold frame in the screen's centre — the city's picture, its
/// name and army, the card's figures, the commands as rows with a tag each, Return. A row's click answers
/// the script's menu with its choice; a right click or Esc anywhere cancels (0), as the original's menu —
/// but not in the dungeon's menu (its place as a caption over the rows), where a cancel only opens it again.
/// Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class ProvinceMenuView : Control
{
    private const float Width = 470, Pad = 18, PictureW = 225, PictureH = 150, RowPitch = 36, RowH = 29, Caption = 30;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private FontFile _gothic;
    private ProvinceMenu? _menu;
    private ImageTexture? _picture;
    private float _k = 1;                           // window pixels to a screen pixel
    private int _hovered = -1, _pressed = -1;       // the row: 0-based, the commands then Return
    private TaskCompletionSource<int>? _answer;

    public ProvinceMenuView(GameFont font, FacePictures pictures)
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
    public void Open(ProvinceMenu menu)
    {
        _menu = menu;
        _picture = _pictures.Picture(menu.Picture) is { } image ? ImageTexture.CreateFromImage(image) : null;
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
        _picture = null;
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

    private int RowCount => _menu is null ? 0 : _menu.Items.Count + 1;

    private float Height => Top + (RowCount - 1) * RowPitch + 50;

    /// <summary>Where the rows start, under the header and the place's caption, if any.</summary>
    private float Top => Pad + PictureH + Pad + (_menu?.Place is null ? 0 : Caption);

    /// <summary>The panel's corner: centred on the screen, as the original's menu (owner, 2026-10-16).</summary>
    private float PanelX => MathF.Round((1280 - Width) / 2);

    private float PanelY => MathF.Round((800 - Height) / 2);

    private float Line(float width) => Lacquer.Line(width, _k);

    private Rect2 RowRect(int row) => row < RowCount - 1
        ? new Rect2(PanelX + 16, PanelY + Top + row * RowPitch, Width - 32, RowH)
        : new Rect2(PanelX + Width - 118, PanelY + Top + (RowCount - 1) * RowPitch + 4, 102, 28);

    private bool Enabled(int row) => _menu is { } m && (row == RowCount - 1 || m.Items[row].Enabled);

    private int Choice(int row) => _menu is not { } m ? 0 : row == RowCount - 1 ? m.Return : m.Items[row].Choice;

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
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } when _menu is { CancelReopens: false }:
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
        if (_answer is not null && _menu is { CancelReopens: false } && e is InputEventKey { Keycode: Key.Escape, Pressed: true })
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
        float x = PanelX, y = PanelY, h = Height;
        Lacquer.DrawPanel(this, new Rect2(x, y, Width, h), _k);
        // The city's picture.
        var picture = new Rect2(x + Pad, y + Pad, PictureW, PictureH);
        if (_picture is not null)
            DrawTextureRect(_picture, picture, false);
        DrawRect(picture.Grow(1), Lacquer.DimGold, false, Line(1));
        // The name (made smaller to fit), the army, the figures.
        float tx = picture.End.X + Pad, room = x + Width - 16 - tx;
        int nameSize = 19;
        while (nameSize > 11 && _brush.GetStringSize(m.Name, fontSize: nameSize).X > room)
            nameSize--;
        DrawString(_brush, new Vector2(tx, y + 16 + _brush.GetAscent(nameSize)), m.Name, fontSize: nameSize, modulate: Lacquer.Name);
        DrawString(_cinzel, new Vector2(tx + 1, y + 46 + _cinzel.GetAscent(13)), m.Army, fontSize: 13, modulate: Lacquer.Subtitle);
        float sy2 = y + 70;
        foreach (string line in m.Card)
        {
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            string label = colon < 0 ? "" : line[..colon], value = colon < 0 ? line : line[(colon + 2)..];
            DrawString(_gothic, new Vector2(tx, sy2 + _gothic.GetAscent(13)), label, fontSize: 13, modulate: Lacquer.Label);
            DrawString(_gothic, new Vector2(tx, sy2 + 15 + _gothic.GetAscent(15)), value, fontSize: 15, modulate: value == "??" ? Lacquer.Label : Lacquer.Value);
            sy2 += 34;
        }
        // The place a later menu belongs to (the dungeon), between gold rules.
        if (m.Place is { } place)
        {
            float cy = y + Pad + PictureH + Pad + Caption / 2 - 4, pw = _cinzel.GetStringSize(place, fontSize: 15).X;
            DrawString(_cinzel, new Vector2(x + (Width - pw) / 2, cy + (_cinzel.GetAscent(15) - _cinzel.GetDescent(15)) / 2), place,
                fontSize: 15, modulate: Lacquer.Name);
            DrawLine(new Vector2(x + 16, cy), new Vector2(x + (Width - pw) / 2 - 12, cy), Lacquer.DimGold, Line(1));
            DrawLine(new Vector2(x + (Width + pw) / 2 + 12, cy), new Vector2(x + Width - 16, cy), Lacquer.DimGold, Line(1));
        }
        // The commands, then Return.
        for (int r = 0; r < RowCount; r++)
            DrawRow(r);
    }

    private void DrawRow(int r)
    {
        var m = _menu!;
        var rect = RowRect(r);
        bool on = Enabled(r), hover = r == _hovered && on;
        Lacquer.DrawRowBox(this, rect, hover, _k);
        float baseline(int size, Font font) => rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        if (r == RowCount - 1)
        {
            float w = _gothic.GetStringSize("Return", fontSize: 15).X;
            DrawString(_gothic, new Vector2(rect.GetCenter().X - w / 2, baseline(15, _gothic)), "Return", fontSize: 15,
                modulate: hover ? Lacquer.Text : Lacquer.Quiet);
            return;
        }
        var item = m.Items[r];
        var (tag, colour) = TagOf(item.Action);
        var tagRect = new Rect2(rect.Position.X + 5, rect.Position.Y + 5, 62, rect.Size.Y - 10);
        Lacquer.DrawTag(this, _gothic, tagRect, tag, colour, on, _k);
        DrawString(_gothic, new Vector2(rect.Position.X + 80, baseline(16, _gothic)), item.Text, fontSize: 16, modulate: on ? Lacquer.Text : Lacquer.OffText);
        if (!on)
        {
            const string spent = "used this week";
            float sw = _gothic.GetStringSize(spent, fontSize: 12).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, baseline(12, _gothic)), spent, fontSize: 12, modulate: Lacquer.OffText);
        }
    }

    /// <summary>A command's tag and its lacquer colour (ours: the original's menus have none).</summary>
    private static (string Tag, Color Colour) TagOf(ProvinceAction action) => action switch
    {
        ProvinceAction.Levy => ("Levy", Color.Color8(150, 52, 36)),
        ProvinceAction.Build => ("Build", Color.Color8(46, 62, 104)),
        ProvinceAction.Plot => ("Plot", Color.Color8(104, 70, 34)),
        ProvinceAction.Visit => ("Visit", Color.Color8(70, 96, 52)),
        ProvinceAction.Search => ("Search", Color.Color8(36, 92, 96)),
        ProvinceAction.Battle => ("Battle", Color.Color8(128, 30, 30)),
        ProvinceAction.Cell => ("Cell", Color.Color8(96, 40, 58)),
        ProvinceAction.Look => ("Look", Color.Color8(70, 96, 52)),
        _ => ("Ninja", Color.Color8(74, 50, 96)),
    };
}
