using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The phone's Menu held landscape (docs/ui/phone-build.md, step L1; docs/ui/phone.md, §26, the owner's LN3): the Menu cell
/// on top of the left side, and the sheet it opens — the menu bar's commands as tiles four a row in a panel over the frame's
/// middle, the screen dimmed, the cell reading Close (<see cref="PhoneMenu"/>). A tile's tap acts (<see cref="Chosen"/>)
/// and the sheet closes; a switch's tile changes and the sheet stays; Restart and Exit ask first in a callout at their
/// tile, the confirming red button | Cancel inside (a way back stands right of the action, owner, 2026-10-30); Debug opens its own tiles. A tap off the panel or on Close closes the
/// sheet, Back (the right click) too — an open question first. Closed it takes the cell's box alone; open the whole screen
/// (its parent's area), and the game waits from <see cref="Opened"/> to <see cref="Closed"/>. Drawn in the frame's layout
/// pixels (<see cref="PhoneUnits"/>); placed only on a phone. Held upright (step P1; §25, PN1, PN1q) the cell is the bar's
/// first and the sheet rises from the bar, its tiles rows across the screen from the thumb up, a question over its row.
/// </summary>
internal sealed partial class MenuSheetView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private static readonly Color Veil = new(0, 0, 0, 0.6f), Lit = Color.Color8(96, 72, 30), Held = new(1, 1, 1, 0.08f);

    // What a press came down on: a tile's index, or one of these.
    private const int None = -1, Cell = 1000, Cancel = 1001, Confirm = 1002, Outside = 1003, Inside = 1004;

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly Func<PhoneMenu.Settings> _settings;
    private PhoneFrame? _phone;
    private bool _debug;
    private IReadOnlyList<PhoneMenu.Tile> _tiles = [];
    private PhoneMenu.Tile? _asking;                    // the tile whose question is up
    private int _held = None;

    public MenuSheetView(Func<PhoneMenu.Settings> settings)
    {
        _settings = settings;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 1001;                                  // over everything, the message log (1000) included
        Visible = false;
        MouseExited += () => SetHeld(None);
    }

    /// <summary>The sheet opened: the game waits.</summary>
    public event Action? Opened;

    /// <summary>The sheet closed.</summary>
    public event Action? Closed;

    /// <summary>A tile acted (the sheet closed before an act), a switch changed, or a question confirmed.</summary>
    public event Action<PhoneMenu.Command>? Chosen;

    /// <summary>An event over the closed cell but a left button's: the screen's own (Back's right click among them).</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>Whether the sheet is up.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The tiles shown: the first page or Debug's.</summary>
    public IReadOnlyList<PhoneMenu.Tile> Tiles => _tiles;

    /// <summary>The question up, or null.</summary>
    public PhoneMenu.Question? Asking => _asking is { } t ? PhoneMenu.QuestionOf(t.Command) : null;

    /// <summary>The Menu (or Close) cell's centre, global coordinates.</summary>
    public Vector2 CellCentre => Centre(PhoneScenes.MenuCell(Frame));

    /// <summary>The centre of <paramref name="command"/>'s tile on the page shown, global coordinates.</summary>
    public Vector2 TileCentre(PhoneMenu.Command command)
    {
        int i = _tiles.ToList().FindIndex(t => t.Command == command);
        return i < 0 ? throw new InvalidOperationException($"No {command} tile on the Menu's page.")
            : Centre(PhoneMenu.Tiles(Frame, _tiles)[i]);
    }

    /// <summary>The question's Cancel's centre, global coordinates.</summary>
    public Vector2 CancelCentre => _asking is { } t ? Centre(Question(t).Cancel) : throw new InvalidOperationException("No question up.");

    /// <summary>Opens the sheet on its first page.</summary>
    public void Open()
    {
        if (IsOpen)
            return;
        IsOpen = true;
        _debug = false;
        _asking = null;
        Refresh();
        Opened?.Invoke();
        Place();
    }

    /// <summary>Closes the sheet (its question with it).</summary>
    public void Close()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        _asking = null;
        SetHeld(None);
        Closed?.Invoke();
        Place();
    }

    /// <summary>The tiles read again (a switch changed elsewhere: F1, the drawer's Skip).</summary>
    public void Refresh()
    {
        var settings = _settings();
        _tiles = _debug ? PhoneMenu.Debug(settings) : PhoneMenu.Main(settings);
        QueueRedraw();
    }

    /// <summary>Over its parent's area: the Menu cell alone while closed, the whole area while open.</summary>
    public void Place()
    {
        var phone = Device.Held(this);
        if (phone is null)
            return;
        var cell = PhoneScenes.MenuCell(phone);
        var (position, size) = IsOpen
            ? (Vector2.Zero, GetParentAreaSize())
            : (new Vector2(cell.X, cell.Y), new Vector2(cell.W, cell.H));
        if (phone == _phone && Position == position && Size == size)
            return;
        _phone = phone;
        Position = position;
        Size = size;
        QueueRedraw();
    }

    private PhoneFrame Frame => _phone ?? throw new InvalidOperationException("The Menu is placed on a phone only.");

    private PhoneMenu.Callout Question(PhoneMenu.Tile tile) =>
        PhoneMenu.QuestionAt(Frame, PhoneMenu.Tiles(Frame, _tiles)[_tiles.ToList().IndexOf(tile)]);

    private Vector2 Centre(PhoneFrame.Box box) => GlobalPosition - Position + new Vector2(box.X + box.W / 2, box.Y + box.H / 2);

    public override void _GuiInput(InputEvent e)
    {
        if (_phone is null)
            return;
        if (!IsOpen && e is not InputEventMouseButton { ButtonIndex: MouseButton.Left })
        {
            Forwarded?.Invoke(e);
            return;
        }
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                if (_asking is not null)
                    SetAsking(null);
                else
                    Close();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = TargetAt(button.Position + Position);
                if (button.Pressed)
                {
                    SetHeld(at);
                    break;
                }
                int held = _held;
                SetHeld(None);
                if (held == at)
                    Tap(at);
                break;
        }
        AcceptEvent();
    }

    /// <summary>What lies under <paramref name="p"/> (the parent's coordinates).</summary>
    private int TargetAt(Vector2 p)
    {
        var f = Frame;
        if (PhoneScenes.MenuCell(f).Contains(p.X, p.Y))
            return Cell;
        if (!IsOpen)
            return None;
        if (_asking is { } asking)
        {
            var q = Question(asking);
            return q.Cancel.Contains(p.X, p.Y) ? Cancel : q.Confirm.Contains(p.X, p.Y) ? Confirm
                : q.Box.Contains(p.X, p.Y) ? Inside : Outside;
        }
        int tile = PhoneMenu.TileAt(f, _tiles, p.X, p.Y);
        return tile >= 0 ? tile : PhoneMenu.Panel(f, _tiles).Contains(p.X, p.Y) ? Inside : Outside;
    }

    private void Tap(int target)
    {
        switch (target)
        {
            case Cell:
                if (IsOpen)
                    Close();
                else
                    Open();
                return;
            case Cancel:
                SetAsking(null);
                return;
            case Confirm when _asking is { } asking:
                Close();
                Chosen?.Invoke(asking.Command);
                return;
            case Outside:
                if (_asking is not null)
                    SetAsking(null);
                else
                    Close();
                return;
            case >= 0 when target < _tiles.Count && _tiles[target] is { Disabled: false } tile:
                TileTapped(tile);
                return;
        }
    }

    private void TileTapped(PhoneMenu.Tile tile)
    {
        switch (tile.Kind)
        {
            case PhoneMenu.Kind.Act:
                Close();
                Chosen?.Invoke(tile.Command);
                break;
            case PhoneMenu.Kind.Switch:
                Chosen?.Invoke(tile.Command);
                Refresh();
                break;
            case PhoneMenu.Kind.Asks:
                SetAsking(tile);
                break;
            case PhoneMenu.Kind.Page:
                _debug = tile.Command == PhoneMenu.Command.Debug;
                Refresh();
                break;
        }
    }

    private void SetAsking(PhoneMenu.Tile? tile)
    {
        _asking = tile;
        QueueRedraw();
    }

    private void SetHeld(int target)
    {
        if (target == _held)
            return;
        _held = target;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_phone is not { } f)
            return;
        var u = new PhoneUnits(f);
        DrawSetTransform(-Position, 0, new Vector2(u.K, u.K));
        var cell = u.Rect(PhoneScenes.MenuCell(f));
        if (!IsOpen)
        {
            SideCells.Draw(this, u, cell, SideCells.Icon.Menu, "Menu", held: _held == Cell);
            return;
        }
        DrawRect(new Rect2(Vector2.Zero, f.Screen.W / u.K, f.Screen.H / u.K), Veil);
        var panel = u.Rect(PhoneMenu.Panel(f, _tiles));
        Ui.DrawPanel(this, panel, u.K);
        var (tx, ty) = PhoneMenu.Title(f, _tiles);
        int title = u.Size(56);
        DrawString(_brush, new Vector2(tx / u.K, ty / u.K + (_brush.GetAscent(title) - _brush.GetDescent(title)) / 2), "Menu",
            fontSize: title, modulate: Ui.Name);
        var boxes = PhoneMenu.Tiles(f, _tiles);
        for (int i = 0; i < _tiles.Count; i++)
        {
            if (f.IsUpright)
                DrawRow(u, u.Rect(boxes[i]), _tiles[i], _held == i && _asking is null);
            else
                DrawTile(u, u.Rect(boxes[i]), _tiles[i], _held == i && _asking is null);
        }
        int sound = _tiles.ToList().FindIndex(t => t.Command == PhoneMenu.Command.Sound);
        if (f.IsUpright && sound >= 0 && sound + 1 < _tiles.Count && _tiles[sound + 1].Command == PhoneMenu.Command.Screenshot)
        {
            // The rule between Save Screenshot and the switches (PN1).
            float y = (boxes[sound].Y + boxes[sound + 1].Bottom) / 2 / u.K;
            DrawLine(new Vector2(boxes[sound].X / u.K + u.Px(20), y), new Vector2(boxes[sound].Right / u.K - u.Px(20), y), Ui.DimGold,
                Lacquer.Line(2, u.K));
        }
        SideCells.Draw(this, u, cell, SideCells.Icon.Close, "Close", lit: true, held: _held == Cell);
        if (_asking is { } asking)
            DrawQuestion(u, Question(asking), PhoneMenu.QuestionOf(asking.Command)!);
    }

    /// <summary>
    /// A row of the sheet held upright (docs/ui/phone.md, §25, PN1): its word at the left (a command that asks first with
    /// "…"), a switch's pill at the right reading On or Off, a page's ›; a greyed one dim.
    /// </summary>
    private void DrawRow(PhoneUnits u, Rect2 box, PhoneMenu.Tile tile, bool held)
    {
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.DimGold, false, Lacquer.Line(2, u.K));
        if (held && !tile.Disabled)
            DrawRect(box.Grow(-u.Px(4)), Held);
        var font = SideCells.Font;
        string word = tile.Kind == PhoneMenu.Kind.Asks ? tile.Label + "…" : tile.Label;
        int size = SideCells.Fit(font, word, u.Size(46), box.Size.X - u.Px(300));
        float cy = box.GetCenter().Y, right = box.End.X - u.Px(28);
        DrawString(font, new Vector2(box.Position.X + u.Px(36), cy + (font.GetAscent(size) - font.GetDescent(size)) / 2), word,
            fontSize: size, modulate: tile.Disabled ? Ui.OffText : Ui.Text);
        if (tile.Kind == PhoneMenu.Kind.Switch)
        {
            float w = u.Px(150), h = u.Px(72);
            var pill = new Rect2(right - w, cy - h / 2, w, h);
            var style = new StyleBoxFlat
            {
                BgColor = tile.On ? Color.Color8(150, 116, 52) : Color.Color8(40, 32, 26),
                BorderColor = tile.On && !tile.Disabled ? Ui.Gold : Ui.DimGold,
                AntiAliasing = true,
            };
            style.SetCornerRadiusAll((int)(h / 2));
            style.SetBorderWidthAll(Math.Max(1, (int)Math.Round(Lacquer.Line(2, u.K))));
            DrawStyleBox(style, pill);
            float knob = tile.On ? pill.End.X - h / 2 : pill.Position.X + h / 2;
            DrawCircle(new Vector2(knob, cy), h / 2 - u.Px(9), tile.Disabled ? Ui.OffText : tile.On ? Ui.Name : Ui.Quiet);
            string state = tile.On ? "On" : "Off";
            int small = u.Size(34);
            DrawString(font, new Vector2(pill.Position.X - u.Px(18) - Width(font, state, small),
                cy + (font.GetAscent(small) - font.GetDescent(small)) / 2), state, fontSize: small,
                modulate: tile.Disabled ? Ui.OffText : tile.On ? Ui.Text : Ui.Quiet);
        }
        else if (tile is { Kind: PhoneMenu.Kind.Page, Command: not PhoneMenu.Command.Back })
        {
            int arrow = u.Size(72);
            DrawString(font, new Vector2(right - u.Px(12) - Width(font, "›", arrow),
                cy + (font.GetAscent(arrow) - font.GetDescent(arrow)) / 2), "›", fontSize: arrow, modulate: Ui.Gold);
        }
    }

    /// <summary>A tile: its word over its small line; a switch's lit while on, a greyed one dim.</summary>

    private void DrawTile(PhoneUnits u, Rect2 box, PhoneMenu.Tile tile, bool held)
    {
        bool lit = tile is { Kind: PhoneMenu.Kind.Switch, On: true };
        DrawRect(box, lit ? Lit : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : Ui.DimGold, false, Lacquer.Line(2, u.K));
        if (held && !tile.Disabled)
            DrawRect(box.Grow(-u.Px(4)), Held);
        var font = SideCells.Font;
        var ink = tile.Disabled ? Ui.OffText : Ui.Text;
        float cx = box.GetCenter().X, cy = box.GetCenter().Y;
        var lines = Lines(font, tile.Label, u.Size(44), box.Size.X - u.Px(30));
        int size = lines.Size;
        float pitch = u.Px(50), top = cy - u.Px(26) - (lines.Text.Length - 1) * pitch;   // the last line where one line stands
        for (int i = 0; i < lines.Text.Length; i++)
            Centred(font, lines.Text[i], size, cx, top + i * pitch, ink);
        if (tile.Sub.Length > 0)
            Centred(font, tile.Sub, u.Size(tile.Kind == PhoneMenu.Kind.Page ? 48 : 32), cx, cy + u.Px(40),
                tile.Disabled ? Ui.OffText : lit ? Ui.Name : Ui.Quiet);
    }

    /// <summary>
    /// A tile's word in one line at <paramref name="size"/>, or — too wide for <paramref name="width"/> — in two at its space
    /// nearest the middle; still too wide, smaller.
    /// </summary>
    private static (string[] Text, int Size) Lines(Font font, string word, int size, float width)
    {
        string[] lines = [word];
        if (Width(font, word, size) > width && word.Contains(' '))
        {
            int mid = word.Length / 2, at = Enumerable.Range(0, word.Length).Where(i => word[i] == ' ').MinBy(i => Math.Abs(i - mid));
            lines = [word[..at], word[(at + 1)..]];
        }
        float widest = lines.Max(l => Width(font, l, size));
        return (lines, widest <= width ? size : Math.Max(8, (int)(size * width / widest)));
    }

    private static float Width(Font font, string text, int size) => font.GetStringSize(text, fontSize: size).X;

    /// <summary><paramref name="text"/> centred on (<paramref name="cx"/>, <paramref name="cy"/>), its line's middle there.</summary>
    private void Centred(Font font, string text, int size, float cx, float cy, Color ink) =>
        DrawString(font, new Vector2(cx - Width(font, text, size) / 2, cy + (font.GetAscent(size) - font.GetDescent(size)) / 2), text,
            fontSize: size, modulate: ink);

    /// <summary>The question's callout at its tile (the house's, as Treasure's): its title, its line, the red button | Cancel.</summary>
    private void DrawQuestion(PhoneUnits u, PhoneMenu.Callout callout, PhoneMenu.Question question)
    {
        var box = u.Rect(callout.Box);
        Ui.DrawPanel(this, box, u.K);
        float tx = callout.TipX / u.K, ty = callout.TipY / u.K, edge = callout.Below ? box.Position.Y : box.End.Y, w = u.Px(30);
        float inward = callout.Below ? 3 : -3;
        DrawColoredPolygon([new(tx - w, edge + inward), new(tx + w, edge + inward), new(tx, ty)], Ui.Panel);
        DrawPolyline([new(tx - w, edge), new(tx, ty), new(tx + w, edge)], Ui.Gold, Lacquer.Line(2, u.K), true);
        float x = box.Position.X + u.Px(40);
        int title = u.Size(52), line = u.Size(40);
        DrawString(_brush, new Vector2(x, box.Position.Y + u.Px(64) + (_brush.GetAscent(title) - _brush.GetDescent(title)) / 2),
            question.Title, fontSize: title, modulate: Ui.Name);
        var font = SideCells.Font;
        DrawString(font, new Vector2(x, box.Position.Y + u.Px(142) + (font.GetAscent(line) - font.GetDescent(line)) / 2),
            question.Line, fontSize: line, modulate: Ui.Text);
        var cancel = u.Rect(callout.Cancel);
        Ui.DrawRowBox(this, cancel, _held == Cancel, u.K);
        Centred(_brush, "Cancel", u.Size(46), cancel.GetCenter().X, cancel.GetCenter().Y, Ui.Text);
        var confirm = u.Rect(callout.Confirm);
        Ui.DrawRedButton(this, confirm, false, _held == Confirm, u.K);
        Centred(_brush, question.Confirm, u.Size(52), confirm.GetCenter().X, confirm.GetCenter().Y, Ui.Name);
    }
}
