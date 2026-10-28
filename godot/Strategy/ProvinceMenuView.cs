using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our province menu (docs/ui/command-screen.md, "The province menu"; the owner's mock-up 4a): a city's
/// first menu as a lacquer panel with a double gold frame in the screen's centre — the city's picture, its
/// name and army, the card's figures, the commands as rows with a tag each, Return. A row's click answers
/// the script's menu with its choice; a right click or Esc anywhere cancels (0), as the original's menu —
/// but not in the dungeon's menu (its place as a caption over the rows), where a cancel only opens it again.
/// Laid out in the 1280×800 screen's pixels, scaled with the picture. In the touch mode (the owner's B, 2026-10-25,
/// docs/ui/touch.md, "The province menu within the thumbs' reach") the province's card — the picture, the name, the army,
/// the figures — stands beside the tapped province, a triangle pointing at it and the province ringed in gold, and the
/// commands stand on the right rail as the command panel's, from the foot up, Return at the foot; a tap off the card and
/// the rail cancels as a right click does, so not in the dungeon's menu (owner, 2026-10-24), which stands over the
/// city's picture and points at nothing. On the phone held landscape the card stands beside the province at the phone's
/// type and the rail is the right side (docs/ui/phone.md, §12, LM1c; <c>ProvinceMenuView.Phone.cs</c>).
/// </summary>
internal sealed partial class ProvinceMenuView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Map);

    // The PC's layout: the owner's A1 (2026-10-25, docs/ui/command-screen.md, "Taller rows"); the rows 38 high unless
    // Debug → Menu Row Height says otherwise (MenuRowsChoice), the header as it is.
    private const float Pad = 18, PictureW = 234, PictureH = 156, Caption = 32;
    // The touch mode's layout: the card 300 wide, 60 beside the province (left of it when there is room); the rail as the
    // command panel's right one (CommandMenuView): 244 wide at x 1032 down to the foot, rows 220×46 54 apart, the lowest
    // item's top at 664, Return 220×60 at 726, where the map's button stands.
    private const float CardW = 300, CardPad = 14, CardGap = 60, TouchPictureW = 272, TouchPictureH = 180, TouchFigure = 36;
    private const float RailW = 244, RightRail = 1032, RailFoot = 796, RailInset = 12;
    private const float TouchRowW = 220, TouchRowH = 46, TouchPitch = 54, ItemFoot = 664, TouchReturnTop = 726, TouchReturnH = 60;
    private const float RingRadius = 30;

    private static bool Touch => Device.TouchLayout;                // the tablet's layout, read live (docs/ui/phone-build.md, §3)

    private static MenuRows Rows => MenuRowsChoice.Rows;
    private static float Width => MathF.Max(500, MathF.Round(500 * Rows.Scale * 0.9f));
    private static float RowPitch => Rows.Pitch;
    private static float RowH => Rows.Height;
    private static float ReturnW => MathF.Round(120 * Rows.Scale);
    private static float ReturnH => Rows.Height - 2;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private FontFile _gothic;
    private ProvinceMenu? _menu;
    private ImageTexture? _picture;
    private Vector2? _at;                           // the touch mode: the tapped province's point, where the card points
    private float _k = 1;                           // window pixels to a screen pixel
    private int _hovered = -1, _pressed = -1;       // the row: 0-based, the commands then Return
    private bool _outside;                          // the touch mode: the press came down outside the panel
    private TaskCompletionSource<int>? _answer;

    public ProvinceMenuView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    // An open menu follows Debug → Menu Row Height at once.
    public override void _EnterTree() => MenuRowsChoice.Changed += QueueRedraw;

    public override void _ExitTree() => MenuRowsChoice.Changed -= QueueRedraw;

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>
    /// Shows <paramref name="menu"/>; <see cref="ChooseAsync"/> waits for its answer. <paramref name="at"/> is the province's
    /// point on the screen (1280×800), where the touch mode's card points; null for none (out of view, the dungeon's menu).
    /// </summary>
    public void Open(ProvinceMenu menu, Vector2? at = null)
    {
        _menu = menu;
        _at = menu.Place is null ? at : null;
        _picture = _pictures.Picture(menu.Picture) is { } image ? ImageTexture.CreateFromImage(image) : null;
        _hovered = _pressed = -1;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>
    /// <paramref name="province"/>'s point on the screen (1280×800) for the touch mode's card: its map point on the view the
    /// screen shows (<paramref name="shown"/>, <see cref="GamePresenter.MapViewShown"/>; else the camera as the map loop's
    /// corner); null out of view.
    /// </summary>
    public static Vector2? PointOf(GameState state, int province, (int X, int Y)? shown) =>
        MapMarkers.PointOnView(state, province, shown ?? GesturePoints.ViewCorner(state, centred: false)) is { } p
            ? new Vector2(p.X, p.Y) * 2 : null;

    /// <summary>The script's answer: a row's choice, or 0 for a cancel.</summary>
    public Task<int> ChooseAsync()
    {
        _answer = new TaskCompletionSource<int>();
        return _answer.Task;
    }

    /// <summary>A point of the picture outside the panel (the touch mode's card and rail), global: where unattended play taps to close it.</summary>
    public Vector2 OutsidePoint
    {
        get
        {
            if (_phone is { } f)
                return PhoneOutsidePoint(f);
            var point = new Vector2(40, 700);
            if (Touch)
                point = ((Vector2[])[new(40, 700), new(40, 100), new(980, 100), new(640, 780)])
                    .First(p => !CardRect.HasPoint(p) && !RailRect.HasPoint(p) && (_at is not { } at || p.DistanceTo(at) > 2 * RingRadius));
            return GetGlobalRect().Position + point * _k;
        }
    }

    public void Close()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _menu = null;
        _picture = null;
        _at = null;
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

    private int RowCount => _menu is null ? 0 : _menu.Items.Count + 1;

    private float Height => Top + (RowCount - 1) * RowPitch + 1 + ReturnH + 14;

    /// <summary>Where the PC's rows start, under the header and the place's caption, if any.</summary>
    private float Top => Pad + PictureH + Pad + (_menu?.Place is null ? 0 : Caption);

    private float PanelWidth => Width;

    /// <summary>The panel's corner: centred on the screen, as the original's menu (owner, 2026-10-16).</summary>
    private float PanelX => MathF.Round((1280 - PanelWidth) / 2);

    private float PanelY => MathF.Round((800 - Height) / 2);

    private float Line(float width) => Lacquer.Line(width, _k);

    /// <summary>
    /// The touch mode's card: beside the tapped province, left of it when there is room, else right of it short of the
    /// rail, centred on it and kept on the screen; without a point, in the middle of the map left of the rail.
    /// </summary>
    private Rect2 CardRect
    {
        get
        {
            float h = CardPad + TouchPictureH + 14 + 62 + (_menu?.Card.Count ?? 0) * TouchFigure + CardPad - 8;
            if (_at is not { } at)
                return new Rect2(MathF.Round((RightRail - CardW) / 2), MathF.Round((800 - h) / 2), CardW, h);
            float x = at.X - CardGap - CardW;
            if (x < 8)
                x = Math.Min(at.X + CardGap, RightRail - 8 - CardW);
            return new Rect2(x, MathF.Round(Math.Clamp(at.Y - h / 2, 8, 792 - h)), CardW, h);
        }
    }

    private const float TouchTagW = 62, TouchTextLeft = 78;

    /// <summary>
    /// The touch mode's rows' text size: 19, or as much smaller as the longest name needs to fit the rail's row
    /// ("Military Academy"), the same for every row so they read as one list.
    /// </summary>
    private int TouchTextSize
    {
        get
        {
            int size = 19;
            float room = TouchRowW - TouchTextLeft - 8;
            while (size > 14 && _menu is { } m && m.Items.Any(i => _gothic.GetStringSize(i.Text, fontSize: size).X > room))
                size--;
            return size;
        }
    }

    /// <summary>The touch mode's rail: the place's caption (the dungeon's menu), the commands, Return at the foot.</summary>
    private Rect2 RailRect
    {
        get
        {
            int items = Math.Max(0, RowCount - 1);
            float top = (items == 0 ? TouchReturnTop : ItemFoot - (items - 1) * TouchPitch) - RailInset
                - (_menu?.Place is null ? 0 : Caption);
            return new Rect2(RightRail, top, RailW, RailFoot - top);
        }
    }

    private Rect2 RowRect(int row) => Touch
        ? row < RowCount - 1
            ? new Rect2(RightRail + RailInset, ItemFoot - row * TouchPitch, TouchRowW, TouchRowH)  // from the foot up
            : new Rect2(RightRail + RailInset, TouchReturnTop, TouchRowW, TouchReturnH)
        : row < RowCount - 1
            ? new Rect2(PanelX + 16, PanelY + Top + row * RowPitch, Width - 32, RowH)
            : new Rect2(PanelX + Width - 16 - ReturnW, PanelY + Top + (RowCount - 1) * RowPitch + 1, ReturnW, ReturnH);

    private bool Enabled(int row) => _menu is { } m && (row == RowCount - 1 || m.Items[row].Enabled);

    private int Choice(int row) => _menu is not { } m ? 0 : row == RowCount - 1 ? m.Return : m.Items[row].Choice;

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
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } when _menu is { CancelReopens: false }:
                Answer(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = RowAt(button.Position);
                var point = button.Position / _k;
                bool outside = _menu is { CancelReopens: false } && (_phone is { } phone ? PhoneOutside(phone, button.Position)
                    : Touch && !CardRect.HasPoint(point) && !RailRect.HasPoint(point));
                if (button.Pressed)
                    (_pressed, _outside) = (at, outside);
                else if (at >= 0 && at == _pressed && Enabled(at))
                    Answer(Choice(at));
                else if (outside && _outside)
                    Answer(0);                              // a tap outside the panel: back to the map
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
        if (_phone is { } phone)
        {
            DrawPhone(phone, m);
            return;
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float x = PanelX, y = PanelY;
        var card = CardRect;
        if (Touch)
            DrawTouchFrame(card);
        else
            Ui.DrawPanel(this, new Rect2(x, y, PanelWidth, Height), _k);
        // The city's picture.
        var picture = Touch ? new Rect2(card.Position.X + CardPad, card.Position.Y + CardPad, TouchPictureW, TouchPictureH)
            : new Rect2(x + Pad, y + Pad, PictureW, PictureH);
        if (_picture is not null)
            DrawTextureRect(_picture, picture, false);
        DrawRect(picture.Grow(1), Ui.DimGold, false, Line(1));
        // The name (made smaller to fit), the army, the figures: right of the picture, in the touch mode under it.
        float tx = Touch ? picture.Position.X : picture.End.X + Pad, ty = Touch ? picture.End.Y + 14 : y + 16;
        float room = Touch ? TouchPictureW : x + Width - 16 - tx;
        int nameSize = Touch ? 22 : 21, armySize = 14, labelSize = 14, valueSize = Touch ? 16 : 17;
        while (nameSize > 11 && _brush.GetStringSize(m.Name, fontSize: nameSize).X > room)
            nameSize--;
        DrawString(_brush, new Vector2(tx, ty + _brush.GetAscent(nameSize)), m.Name, fontSize: nameSize, modulate: Ui.Name);
        DrawString(_cinzel, new Vector2(tx + 1, ty + 32 + _cinzel.GetAscent(armySize)), m.Army, fontSize: armySize,
            modulate: Ui.Subtitle);
        float sy2 = ty + (Touch ? 62 : 58);
        foreach (string line in m.Card)
        {
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            string label = colon < 0 ? "" : line[..colon], value = colon < 0 ? line : line[(colon + 2)..];
            DrawString(_gothic, new Vector2(tx, sy2 + _gothic.GetAscent(labelSize)), label, fontSize: labelSize, modulate: Ui.Label);
            DrawString(_gothic, new Vector2(tx, sy2 + labelSize + 2 + _gothic.GetAscent(valueSize)), value, fontSize: valueSize,
                modulate: value == "??" ? Ui.Label : Ui.Value);
            sy2 += Touch ? TouchFigure : 36;
        }
        // The place a later menu belongs to (the dungeon), between gold rules over the rows (in the touch mode at the rail's top).
        if (m.Place is { } place)
        {
            var rail = RailRect;
            float cx0 = Touch ? rail.Position.X + 16 : x + 16, cx1 = Touch ? rail.End.X - 16 : x + Width - 16, mid = (cx0 + cx1) / 2;
            float cy = Touch ? rail.Position.Y + RailInset + Caption / 2 : y + Top - Caption / 2 - 4;
            float pw = _cinzel.GetStringSize(place, fontSize: 16).X;
            DrawString(_cinzel, new Vector2(mid - pw / 2, cy + (_cinzel.GetAscent(16) - _cinzel.GetDescent(16)) / 2), place,
                fontSize: 16, modulate: Ui.Name);
            DrawLine(new Vector2(cx0, cy), new Vector2(mid - pw / 2 - 12, cy), Ui.DimGold, Line(1));
            DrawLine(new Vector2(mid + pw / 2 + 12, cy), new Vector2(cx1, cy), Ui.DimGold, Line(1));
        }
        // The commands, then Return.
        for (int r = 0; r < RowCount; r++)
            DrawRow(r);
    }

    /// <summary>
    /// The touch mode's frame (the owner's B): the tapped province ringed in gold, the card's panel with its triangle at the
    /// province, the rail's panel.
    /// </summary>
    private void DrawTouchFrame(Rect2 card)
    {
        if (_at is { } at)
        {
            DrawArc(at, RingRadius + 5, 0, MathF.Tau, 48, Ui.DimGold, Line(1));
            DrawArc(at, RingRadius, 0, MathF.Tau, 48, Ui.Gold, Line(3));
        }
        Ui.DrawPanel(this, card, _k);
        if (_at is { } p)
        {
            bool left = card.End.X <= p.X;
            float edge = left ? card.End.X - 1 : card.Position.X + 1;
            float tip = left ? p.X - RingRadius - 6 : p.X + RingRadius + 6;
            float cy = Math.Clamp(p.Y, card.Position.Y + 24, card.End.Y - 24);
            Vector2[] triangle = [new(edge, cy - 16), new(tip, p.Y), new(edge, cy + 16)];
            DrawColoredPolygon(triangle, Ui.Panel with { A = 1 });
            DrawPolyline(triangle, Ui.Gold, Line(2));
        }
        Ui.DrawPanel(this, RailRect, _k);
    }

    private void DrawRow(int r)
    {
        var m = _menu!;
        var rect = RowRect(r);
        bool on = Enabled(r), hover = r == _hovered && on;
        Ui.DrawRowBox(this, rect, hover, _k);
        float baseline(int size, Font font) => rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        if (r == RowCount - 1)
        {
            int returnSize = Touch ? 18 : Rows.Text - 2;
            float w = _gothic.GetStringSize("Return", fontSize: returnSize).X;
            DrawString(_gothic, new Vector2(rect.GetCenter().X - w / 2, baseline(returnSize, _gothic)), "Return", fontSize: returnSize,
                modulate: hover ? Ui.Text : Ui.Quiet);
            return;
        }
        var item = m.Items[r];
        var (tag, colour) = TagOf(item.Action);
        var tagRect = Touch
            ? new Rect2(rect.Position.X + 7, rect.Position.Y + 8, TouchTagW, rect.Size.Y - 16)
            : new Rect2(rect.Position.X + Rows.TagInset, rect.Position.Y + Rows.TagInset, Rows.TagWidth, rect.Size.Y - 2 * Rows.TagInset);
        Ui.DrawTag(this, _gothic, tagRect, tag, colour, on, _k, Touch ? 12 : Rows.TagText);
        int size = Touch ? TouchTextSize : Rows.Text;
        DrawString(_gothic, new Vector2(rect.Position.X + (Touch ? TouchTextLeft : Rows.TextLeft), baseline(size, _gothic)), item.Text, fontSize: size,
            modulate: on ? Ui.Text : Ui.OffText);
        if (!on && !Touch)                                             // the rail has no room for the note: only dimmed
        {
            const string spent = "used this week";
            int spentSize = Rows.Small;
            float sw = _gothic.GetStringSize(spent, fontSize: spentSize).X;
            DrawString(_gothic, new Vector2(rect.End.X - 10 - sw, baseline(spentSize, _gothic)), spent, fontSize: spentSize,
                modulate: Ui.OffText);
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
