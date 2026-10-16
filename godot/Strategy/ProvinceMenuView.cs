using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our province menu (docs/ui/command-screen.md, "The province menu"; the owner's mock-up 4a): a city's
/// first menu as a lacquer panel with a double gold frame at the map's top left — the city's picture, its
/// name and army, the card's figures, the commands as rows with a tag each, Return. A row's click answers
/// the script's menu with its choice; a right click or Esc anywhere cancels (0), as the original's menu.
/// Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class ProvinceMenuView : Control
{
    private static readonly Color Lacquer = new(22 / 255f, 16 / 255f, 12 / 255f, 0.96f);
    private static readonly Color Gold = Color.Color8(196, 160, 86);
    private static readonly Color DimGold = Color.Color8(110, 86, 46);
    private static readonly Color NameColour = Color.Color8(232, 199, 122);
    private static readonly Color ArmyColour = Color.Color8(160, 140, 104);
    private static readonly Color Label = Color.Color8(150, 134, 108);
    private static readonly Color Value = Color.Color8(236, 226, 204);
    private static readonly Color Row = Color.Color8(34, 26, 20);
    private static readonly Color RowHover = Color.Color8(64, 48, 28);
    private static readonly Color HoverLine = Color.Color8(222, 186, 104);
    private static readonly Color Text = Color.Color8(244, 232, 206);
    private static readonly Color OffText = Color.Color8(98, 88, 74);
    private static readonly Color OffTag = Color.Color8(52, 46, 40);

    private const float PanelX = 14, PanelY = 14, Width = 470, Pad = 18, PictureW = 225, PictureH = 150, RowPitch = 36, RowH = 29;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = LoadFont("res://fonts/Shojumaru-Regular.ttf");
    private readonly FontVariation _cinzel = new() { BaseFont = LoadFont("res://fonts/Cinzel-VariableFont_wght.ttf") };
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
        _cinzel.VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 500,
        };
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    private static FontFile LoadFont(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

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

    private float Height => Pad + PictureH + Pad + (RowCount - 1) * RowPitch + 50;

    private Rect2 RowRect(int row) => row < RowCount - 1
        ? new Rect2(PanelX + 16, PanelY + Pad + PictureH + Pad + row * RowPitch, Width - 32, RowH)
        : new Rect2(PanelX + Width - 118, PanelY + Pad + PictureH + Pad + (RowCount - 1) * RowPitch + 4, 102, 28);

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
        float x = PanelX, y = PanelY, h = Height;
        // The shadow, the lacquer, the double gold frame and its corners.
        for (int i = 6; i >= 1; i--)
            DrawRect(new Rect2(x - i + 2, y - i + 6, Width + 2 * i, h + 2 * i), new Color(0, 0, 0, 0.06f));
        DrawRect(new Rect2(x, y, Width, h), Lacquer);
        DrawRect(new Rect2(x + 1, y + 1, Width - 2, h - 2), Gold, false, 2);
        DrawRect(new Rect2(x + 5.5f, y + 5.5f, Width - 11, h - 11), DimGold, false, 1);
        foreach (var (cx, cy, sx, sy) in new[] { (x, y, 1, 1), (x + Width, y, -1, 1), (x, y + h, 1, -1), (x + Width, y + h, -1, -1) })
        {
            DrawPolyline([new(cx + sx * 9, cy + sy * 2), new(cx + sx * 9, cy + sy * 9), new(cx + sx * 2, cy + sy * 9)], Gold, 2);
            DrawColoredPolygon([new(cx + sx * 9, cy + sy * 14), new(cx + sx * 14, cy + sy * 9), new(cx + sx * 9, cy + sy * 4), new(cx + sx * 4, cy + sy * 9)], Gold);
        }
        // The city's picture.
        var picture = new Rect2(x + Pad, y + Pad, PictureW, PictureH);
        if (_picture is not null)
            DrawTextureRect(_picture, picture, false);
        DrawRect(picture.Grow(1), DimGold, false, 1);
        // The name (made smaller to fit), the army, the figures.
        float tx = picture.End.X + Pad, room = x + Width - 16 - tx;
        int nameSize = 19;
        while (nameSize > 11 && _brush.GetStringSize(m.Name, fontSize: nameSize).X > room)
            nameSize--;
        DrawString(_brush, new Vector2(tx, y + 16 + _brush.GetAscent(nameSize)), m.Name, fontSize: nameSize, modulate: NameColour);
        DrawString(_cinzel, new Vector2(tx + 1, y + 46 + _cinzel.GetAscent(13)), m.Army, fontSize: 13, modulate: ArmyColour);
        float sy2 = y + 70;
        foreach (string line in m.Card)
        {
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            string label = colon < 0 ? "" : line[..colon], value = colon < 0 ? line : line[(colon + 2)..];
            DrawString(_gothic, new Vector2(tx, sy2 + _gothic.GetAscent(13)), label, fontSize: 13, modulate: Label);
            DrawString(_gothic, new Vector2(tx, sy2 + 15 + _gothic.GetAscent(15)), value, fontSize: 15, modulate: value == "??" ? Label : Value);
            sy2 += 34;
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
        DrawRect(rect, hover ? RowHover : Row);
        DrawRect(rect, hover ? HoverLine : DimGold, false, hover ? 2 : 1);
        float baseline(int size, Font font) => rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        if (r == RowCount - 1)
        {
            float w = _gothic.GetStringSize("Return", fontSize: 15).X;
            DrawString(_gothic, new Vector2(rect.GetCenter().X - w / 2, baseline(15, _gothic)), "Return", fontSize: 15,
                modulate: hover ? Text : Color.Color8(176, 160, 130));
            return;
        }
        var item = m.Items[r];
        var (tag, colour) = TagOf(item.Action);
        var tagRect = new Rect2(rect.Position.X + 5, rect.Position.Y + 5, 62, rect.Size.Y - 10);
        DrawRect(tagRect, on ? colour : OffTag);
        if (on)
            DrawRect(tagRect, Gold, false, 1);
        float tw = _gothic.GetStringSize(tag, fontSize: 13).X;
        DrawString(_gothic, new Vector2(tagRect.GetCenter().X - tw / 2, baseline(13, _gothic)), tag, fontSize: 13, modulate: on ? Text : OffText);
        DrawString(_gothic, new Vector2(rect.Position.X + 80, baseline(16, _gothic)), item.Text, fontSize: 16, modulate: on ? Text : OffText);
        if (!on)
        {
            const string spent = "used this week";
            float sw = _gothic.GetStringSize(spent, fontSize: 12).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, baseline(12, _gothic)), spent, fontSize: 12, modulate: OffText);
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
        _ => ("Ninja", Color.Color8(74, 50, 96)),
    };
}
