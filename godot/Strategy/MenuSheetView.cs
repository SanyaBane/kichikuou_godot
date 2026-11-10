using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Saves;

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
/// Import original save (docs/save-import.md, §8, the owner's S1) opens a page of the game folder's saves, read as it opens
/// (<see cref="PhoneMenu.Saves"/>): a save's tap asks the PC's question in the callout, Load closing the sheet and handing the
/// save over (<see cref="SaveChosen"/>); a refused one's says why, OK.
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
    private readonly Func<PhoneMenu.SaveShelf>? _readShelf;
    private Page _page;
    private PhoneMenu.SaveShelf _shelf = new([], "");
    private IReadOnlyList<PhoneMenu.Tile> _tiles = [];
    private PhoneMenu.Tile? _asking;                    // the tile whose question is up
    private int _held = None;

    /// <summary>The sheet's pages: the first, Debug's, the game folder's saves.</summary>
    private enum Page { Main, Debug, Saves }

    /// <summary>
    /// The sheet reading its tiles' state from <paramref name="settings"/> and, as the saves' page opens, the game folder's
    /// saves from <paramref name="shelf"/> (none without it).
    /// </summary>
    public MenuSheetView(Func<PhoneMenu.Settings> settings, Func<PhoneMenu.SaveShelf>? shelf = null)
    {
        _settings = settings;
        _readShelf = shelf;
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

    /// <summary>A save of the original whose question was answered Load (the sheet closed already).</summary>
    public event Action<OriginalSaveFile>? SaveChosen;

    /// <summary>An event over the closed cell but a left button's: the screen's own (Back's right click among them).</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>Whether the sheet is up.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The tiles shown: the first page or Debug's.</summary>
    public IReadOnlyList<PhoneMenu.Tile> Tiles => _tiles;

    /// <summary>The question up, or null.</summary>
    public PhoneMenu.Question? Asking => _asking is { } t ? QuestionFor(t) : null;

    /// <summary>The question <paramref name="tile"/> asks: a save's (its load, or why it is refused), else the command's.</summary>
    private PhoneMenu.Question? QuestionFor(PhoneMenu.Tile tile) => tile.Command == PhoneMenu.Command.OriginalSave
        ? _shelf.Saves[tile.Index] is var save && save.Refused ? PhoneMenu.RefusalOf(save) : PhoneMenu.ImportQuestion(save)
        : PhoneMenu.QuestionOf(tile.Command);

    /// <summary>The centre of the save of slot <paramref name="letter"/>'s tile on the saves' page, global coordinates.</summary>
    public Vector2 SaveCentre(string letter)
    {
        int i = _tiles.ToList().FindIndex(t => t.Command == PhoneMenu.Command.OriginalSave && _shelf.Saves[t.Index].Letter == letter);
        return i < 0 ? throw new InvalidOperationException($"No save {letter} on the Menu's page.") : Centre(PhoneMenu.Tiles(Frame, _tiles)[i]);
    }

    /// <summary>The question's confirming button's centre, global coordinates.</summary>
    public Vector2 ConfirmCentre => _asking is { } t ? Centre(Question(t).Confirm) : throw new InvalidOperationException("No question up.");

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
        _page = Page.Main;
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
        _tiles = _page switch
        {
            Page.Debug => PhoneMenu.Debug(settings),
            Page.Saves => PhoneMenu.Saves(_shelf),
            _ => PhoneMenu.Main(settings),
        };
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
        PhoneMenu.QuestionAt(Frame, PhoneMenu.Tiles(Frame, _tiles)[_tiles.ToList().IndexOf(tile)], QuestionLines(tile).Count);

    /// <summary>The lines of <paramref name="tile"/>'s question, each wrapped to the callout's room at the lines' size.</summary>
    private IReadOnlyList<string> QuestionLines(PhoneMenu.Tile tile)
    {
        var u = new PhoneUnits(Frame);
        float width = PhoneMenu.QuestionTextWidth(Frame) / u.K;
        int size = u.Size(40);
        return [.. (QuestionFor(tile)?.Line ?? "").Split('\n').SelectMany(line => Wrap(SideCells.Font, line, size, width))];
    }

    /// <summary><paramref name="text"/> in lines no wider than <paramref name="width"/>, broken at its spaces.</summary>
    private static IEnumerable<string> Wrap(Font font, string text, int size, float width)
    {
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string tried = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Width(font, tried, size) > width)
            {
                yield return line;
                line = word;
            }
            else
                line = tried;
        }
        yield return line;
    }

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
            bool notice = QuestionFor(asking)?.Confirm is not { Length: > 0 };       // OK alone, where Cancel stands
            return q.Cancel.Contains(p.X, p.Y) ? Cancel : !notice && q.Confirm.Contains(p.X, p.Y) ? Confirm
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
            case Confirm when _asking is { Command: PhoneMenu.Command.OriginalSave } save:
                var chosen = _shelf.Saves[save.Index];
                Close();
                SaveChosen?.Invoke(chosen);
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
            case PhoneMenu.Kind.Asks or PhoneMenu.Kind.Save:
                SetAsking(tile);
                break;
            case PhoneMenu.Kind.Page:
                _page = tile.Command switch
                {
                    PhoneMenu.Command.Debug => Page.Debug,
                    PhoneMenu.Command.ImportSave => Page.Saves,
                    _ => Page.Main,
                };
                if (_page == Page.Saves)
                    _shelf = _readShelf?.Invoke() ?? new PhoneMenu.SaveShelf([], "");       // read as the page opens
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
        DrawString(_brush, new Vector2(tx / u.K, ty / u.K + (_brush.GetAscent(title) - _brush.GetDescent(title)) / 2),
            _page == Page.Saves ? PhoneMenu.SavesTitle : "Menu", fontSize: title, modulate: Ui.Name);
        var boxes = PhoneMenu.Tiles(f, _tiles);
        // One size for the sheet's words (docs/ui/style.md, "Buttons side by side"): the longest's in its tile or row.
        var font = SideCells.Font;
        var worded = Enumerable.Range(0, _tiles.Count).Where(i => _tiles[i].Kind != PhoneMenu.Kind.Note).ToList();
        int words = f.IsPortrait
            ? SideCells.FitAll(font, u.Size(PhoneMenu.RowText),
                [.. worded.Select(i => (RowWord(_tiles[i]), u.Rect(boxes[i]).Size.X - u.Px(300)))])
            : worded.Select(i => Lines(font, _tiles[i].Label, u.Size(PhoneMenu.TileText),
                u.Rect(boxes[i]).Size.X - u.Px(30)).Size).DefaultIfEmpty(u.Size(PhoneMenu.TileText)).Min();
        for (int i = 0; i < _tiles.Count; i++)
        {
            if (_tiles[i].Kind == PhoneMenu.Kind.Note)
                DrawNote(u, u.Rect(boxes[i]), _tiles[i].Label);
            else if (_tiles[i].Kind == PhoneMenu.Kind.Save)
                DrawSave(u, u.Rect(boxes[i]), _tiles[i], f.IsPortrait, _held == i && _asking is null, words);
            else if (f.IsPortrait)
                DrawRow(u, u.Rect(boxes[i]), _tiles[i], _held == i && _asking is null, words);
            else
                DrawTile(u, u.Rect(boxes[i]), _tiles[i], _held == i && _asking is null, words);
        }
        int sound = _tiles.ToList().FindIndex(t => t.Command == PhoneMenu.Command.Sound);
        if (f.IsPortrait && sound >= 0 && sound + 1 < _tiles.Count && _tiles[sound + 1].Command == PhoneMenu.Command.Screenshot)
        {
            // The rule between Save Screenshot and the switches (PN1).
            float y = (boxes[sound].Y + boxes[sound + 1].Bottom) / 2 / u.K;
            DrawLine(new Vector2(boxes[sound].X / u.K + u.Px(20), y), new Vector2(boxes[sound].Right / u.K - u.Px(20), y), Ui.DimGold,
                Lacquer.Line(2, u.K));
        }
        SideCells.Draw(this, u, cell, SideCells.Icon.Close, "Close", lit: true, held: _held == Cell);
        if (_asking is { } asking)
            DrawQuestion(u, Question(asking), QuestionFor(asking)!, QuestionLines(asking));
    }

    /// <summary>
    /// A save's tile (S1): held landscape its word over its date and its stage, centred; upright a row with the word at the
    /// left, the date after it and the stage at the right (S1p). A refused one greyed, "cannot be imported" and its reason
    /// in a word where the date and the stage stand.
    /// </summary>
    private void DrawSave(PhoneUnits u, Rect2 box, PhoneMenu.Tile tile, bool upright, bool held, int size)
    {
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.DimGold, false, Lacquer.Frame(2, u.K));
        if (held)
            DrawRect(box.Grow(-u.Px(4)), Held);
        var font = SideCells.Font;
        var ink = tile.Refused ? Ui.OffText : Ui.Text;
        var quiet = tile.Refused ? Ui.OffText : Ui.Quiet;
        float cx = box.GetCenter().X, cy = box.GetCenter().Y;
        if (upright)
        {
            int small = u.Size(38);
            float x = box.Position.X + u.Px(36), baseline = (font.GetAscent(size) - font.GetDescent(size)) / 2;
            DrawString(font, new Vector2(x, cy + baseline), tile.Label, fontSize: size, modulate: ink);
            float smallBase = (font.GetAscent(small) - font.GetDescent(small)) / 2;
            DrawString(font, new Vector2(x + u.Px(230), cy + smallBase), tile.Date, fontSize: small, modulate: quiet);
            DrawString(font, new Vector2(box.End.X - u.Px(32) - Width(font, tile.Stage, small), cy + smallBase), tile.Stage,
                fontSize: small, modulate: quiet);
            return;
        }
        int line = SideCells.Fit(font, tile.Date, u.Size(32), box.Size.X - u.Px(30));
        Centred(font, tile.Label, size, cx, cy - u.Px(42), ink);
        Centred(font, tile.Date, line, cx, cy + u.Px(14), quiet);
        Centred(font, tile.Stage, line, cx, cy + u.Px(58), quiet);
    }

    /// <summary>The note of a page with no save (S1n): its paragraphs wrapped in its box, the first in the text's ink, the folder in gold.</summary>
    private void DrawNote(PhoneUnits u, Rect2 box, string note)
    {
        var font = SideCells.Font;
        int size = u.Size(36);
        float pitch = u.Px(54), x = box.Position.X + u.Px(30), y = box.Position.Y + u.Px(50);
        string[] paragraphs = note.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            var ink = p == 0 ? Ui.Text : p == paragraphs.Length - 1 ? Ui.Name : Ui.Quiet;
            foreach (string line in Wrap(font, paragraphs[p], size, box.Size.X - u.Px(60)))
            {
                DrawString(font, new Vector2(x, y + (font.GetAscent(size) - font.GetDescent(size)) / 2), line, fontSize: size, modulate: ink);
                y += pitch;
            }
        }
    }

    /// <summary>
    /// A row of the sheet held upright (docs/ui/phone.md, §25, PN1): its word at the left (a command that asks first with
    /// "…"), a switch's pill at the right reading On or Off, a page's ›; a greyed one dim.
    /// </summary>
    private void DrawRow(PhoneUnits u, Rect2 box, PhoneMenu.Tile tile, bool held, int size)
    {
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.DimGold, false, Lacquer.Frame(2, u.K));
        if (held && !tile.Disabled)
            DrawRect(box.Grow(-u.Px(4)), Held);
        var font = SideCells.Font;
        string word = RowWord(tile);
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

    /// <summary>A row's word upright: a command that asks first with "…".</summary>
    private static string RowWord(PhoneMenu.Tile tile) => tile.Kind == PhoneMenu.Kind.Asks ? tile.Label + "…" : tile.Label;

    /// <summary>A tile: its word over its small line at the sheet's <paramref name="size"/>; a switch's lit while on, a greyed one dim.</summary>
    private void DrawTile(PhoneUnits u, Rect2 box, PhoneMenu.Tile tile, bool held, int size)
    {
        bool lit = tile is { Kind: PhoneMenu.Kind.Switch, On: true };
        DrawRect(box, lit ? Lit : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : Ui.DimGold, false, Lacquer.Frame(2, u.K));
        if (held && !tile.Disabled)
            DrawRect(box.Grow(-u.Px(4)), Held);
        var font = SideCells.Font;
        var ink = tile.Disabled ? Ui.OffText : Ui.Text;
        float cx = box.GetCenter().X, cy = box.GetCenter().Y;
        var lines = Lines(font, tile.Label, size, box.Size.X - u.Px(30));
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

    /// <summary>
    /// The question's callout at its tile (the house's, as Treasure's): its title (shrunk to the callout's room), its lines,
    /// the red button | Cancel; a notice (no confirming word) OK alone, where Cancel stands.
    /// </summary>
    private void DrawQuestion(PhoneUnits u, PhoneMenu.Callout callout, PhoneMenu.Question question, IReadOnlyList<string> lines)
    {
        var box = u.Rect(callout.Box);
        Ui.DrawPanel(this, box, u.K);
        float tx = callout.TipX / u.K, ty = callout.TipY / u.K, edge = callout.Below ? box.Position.Y : box.End.Y, w = u.Px(30);
        float inward = callout.Below ? 3 : -3;
        DrawColoredPolygon([new(tx - w, edge + inward), new(tx + w, edge + inward), new(tx, ty)], Ui.Panel);
        DrawPolyline([new(tx - w, edge), new(tx, ty), new(tx + w, edge)], Ui.Gold, Lacquer.Line(2, u.K), true);
        float x = box.Position.X + u.Px(40);
        int title = SideCells.Fit(_brush, question.Title, u.Size(52), box.Size.X - 2 * u.Px(PhoneMenu.LineMargin)), line = u.Size(40);
        DrawString(_brush, new Vector2(x, box.Position.Y + u.Px(64) + (_brush.GetAscent(title) - _brush.GetDescent(title)) / 2),
            question.Title, fontSize: title, modulate: Ui.Name);
        var font = SideCells.Font;
        for (int i = 0; i < lines.Count; i++)
            DrawString(font, new Vector2(x, box.Position.Y + u.Px(142 + i * PhoneMenu.LinePitch) + (font.GetAscent(line) - font.GetDescent(line)) / 2),
                lines[i], fontSize: line, modulate: Ui.Text);
        var cancel = u.Rect(callout.Cancel);
        var confirm = u.Rect(callout.Confirm);
        var face = SideCells.Cinzel;
        if (question.Confirm.Length == 0)
        {
            int ok = SideCells.Fit(face, "OK", u.Size(PhoneMenu.ButtonText), cancel.Size.X - u.Px(30));
            Ui.DrawRowBox(this, cancel, _held == Cancel, u.K);
            Centred(face, "OK", ok, cancel.GetCenter().X, cancel.GetCenter().Y, Ui.Text);
            return;
        }
        // Cancel | the red button: Cinzel 700 at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.
        int size = SideCells.FitAll(face, u.Size(PhoneMenu.ButtonText), ("Cancel", cancel.Size.X - u.Px(30)),
            (question.Confirm, confirm.Size.X - u.Px(30)));
        Ui.DrawRowBox(this, cancel, _held == Cancel, u.K);
        Centred(face, "Cancel", size, cancel.GetCenter().X, cancel.GetCenter().Y, Ui.Text);
        Ui.DrawRedButton(this, confirm, false, _held == Confirm, u.K);
        Centred(face, question.Confirm, size, confirm.GetCenter().X, confirm.GetCenter().Y, Ui.Name);
    }
}
