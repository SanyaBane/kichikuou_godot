using System.Globalization;
using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own deployment screen around the <see cref="DeploymentTableView"/> (owner's decision 25,
/// docs/ui/deployment-table.md), in the lacquer look and the layout of our Subordinates screen (the owner's E1/E2):
/// over the whole screen — a dark lacquer backdrop with a faint gold wave pattern; the table's panel across the top
/// (a title band over the ledger, which <see cref="GameScreen"/> places in <see cref="TableArea"/>); the hovered
/// unit's picture panel at the bottom left, as the script would show it; the party at the bottom right as the
/// battle sets it up — the back row on the left, the front row on the right, a card per place with the member's
/// face, its place on a gold diamond and a remove button — with its totals against the budget and the bar's buttons:
/// those that send the party in red lacquer (Attack or Search; Plains and City in a defence), Stop small. Drawn by
/// hand in the 1280×800 screen's pixels, as our map panels (<see cref="Lacquer"/>). It decides nothing: the presses
/// go to <see cref="GameScreen"/>, which answers the script with the gestures it expects; a left click elsewhere
/// does nothing, a right click is the script's own (the last member out). A member's card under the pointer is
/// hovered as its row is (<see cref="HoveredUnit"/>): the script shows its details, the picture with them.
/// </summary>
internal sealed partial class DeploymentPanels : Control
{
    // In the 1280×800 screen's pixels (the script's 640×400 at 2×), after the mock-ups (tools/ui/mock_deployment.py, round e).
    private static readonly Rect2 TablePanel = new(16, 16, 1248, 344);

    /// <summary>Where the ledger goes, in the 1280×800 screen's pixels.</summary>
    public static readonly Rect2 TableArea = new(30, 66, 1220, 282);

    private static readonly Rect2 PictureArea = new(16, 368, 666, 416);
    private static readonly Rect2 PartyPanel = new(690, 368, 574, 416);
    private const float RowLabelsY = 428, CardsTop = 440, CardHeight = 104, CardGap = 8, TotalsY = 678;
    private static readonly Rect2 BarArea = new(704, 712, 546, 58);
    private const float StopWidth = 130, ButtonGap = 8;

    /// <summary>The strip right of the cards where the facing mark stands (the owner's U5), with the gap before it.</summary>
    private const float FacingStrip = 30, FacingGap = 6;

    /// <summary>A dragged face's size under the pointer.</summary>
    private const float GhostSize = 64;

    /// <summary>How far, in window pixels, a press moves before it is a drag.</summary>
    private const float DragDistance = 6;

    // The pressable parts: the bar's buttons 1-3, a member's remove button from RemoveButton + place.
    private const int NoPart = -1, RemoveButton = 10;

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _screen;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700), _cinzelLight = Lacquer.Cinzel(600);
    private FontFile _gothic;
    private FaceCrops _crops = new();
    private Vector2 _origin;
    private float _k = 1;                                   // window pixels to a screen pixel
    private DeploymentParty? _party;
    private string[] _labels = ["", "", ""];
    private int _mode;
    private string _date = "", _key = "";
    private ImageTexture? _picture;
    private int _hovered = NoPart, _pressed = NoPart, _hoveredPlace;
    private int _dragFrom;                                  // the place a left press fell on (0: none)
    private Vector2 _dragStart;
    private bool _dragging;
    private int _dropOn;                                    // the place under the pointer while dragging (0: none)

    /// <summary>The remove button of a member (the unit's number).</summary>
    public event Action<int>? RemovePressed;

    /// <summary>A bar button, 1-3 from the left as on the original's bar.</summary>
    public event Action<int>? ButtonPressed;

    /// <summary>A right press anywhere on the screen.</summary>
    public event Action? RightPressed;

    /// <summary>A member's card dragged onto another place: the places (1-4), from and to.</summary>
    public event Action<int, int>? Dropped;

    /// <summary>Whether a drop's clicks are under way: no drag starts until they are done.</summary>
    public bool Busy { get; set; }

    public DeploymentPanels(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;                     // over the whole screen; the table over it is its own
        Visible = false;
        _backdrop = new Layer(DrawBackdrop) { ClipContents = true };
        AddChild(_backdrop);
        _screen = new Layer(DrawScreen);
        AddChild(_screen);
    }

    /// <summary>Option → Display: the screen's text in the chosen font, as the table's.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        _screen.QueueRedraw();
    }

    /// <summary>Whether the pointer is over the screen.</summary>
    public bool HasPointer => Visible && GetGlobalRect().HasPoint(GetGlobalMousePosition());

    /// <summary>The unit whose card is under the pointer, or 0.</summary>
    public int HoveredUnit => Visible && MemberAt(PlaceAt(ScreenPoint(GetLocalMousePosition()))) is { } member ? member.Unit : 0;

    /// <summary>
    /// Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/> (in its parent's
    /// coordinates), <paramref name="pixelsPerScript"/> window pixels to a script pixel.
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        float k = pixelsPerScript / 2;
        var area = new Vector2(1280, 800) * k;
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area)
            return;
        _origin = origin;
        _k = k;
        foreach (var layer in new[] { _backdrop, _screen })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
        }
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits from Debug → Portraits (read as the screen opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _key = "";
    }

    /// <summary>
    /// The screen from <paramref name="state"/> and the bar's labels and mode from <paramref name="picker"/>, drawn
    /// again when anything shown changed; the party in <paramref name="order"/> while a drop's clicks run (the
    /// order it asked for, not the orders on the way).
    /// </summary>
    public void Refresh(GameState state, PickerView picker, IReadOnlyList<int>? order = null)
    {
        var party = DeploymentTable.Party(state, order);
        var details = DeploymentTable.Details(state);
        var labels = picker.BarLabelTexts();
        var (year, month, week) = StatusStrip.DateOf(state.Week.Value);
        string date = $"LP {year} · Month {month} · Week {week}";
        int portrait = details is null ? 0 : state.Units[details.Unit].PortraitCg.Value;
        string key = $"{string.Join(";", party.Places)}|{party.Troops}|{party.Cost}|{party.Gold}|{string.Join("|", labels)}"
            + $"|{picker.Mode.Value}|{date}|{portrait}|{Busy}|{Figures.Separator}";
        if (key == _key)
            return;
        _key = key;
        if (_party is null || !party.Places.SequenceEqual(_party.Places))
            EndDrag();
        _party = party;
        _labels = [.. labels];
        _mode = picker.Mode.Value;
        _date = date;
        _picture = _faces.PicturePanel(portrait);
        _screen.QueueRedraw();
    }

    /// <summary>
    /// Makes the first picture panel of <paramref name="portraits"/> not made yet, one a call (one a frame, ~8 ms): a row's
    /// first hover finds its picture ready.
    /// </summary>
    public void WarmPictures(IEnumerable<int> portraits)
    {
        if (portraits.FirstOrDefault(p => !_faces.HasPanel(p), -1) is var portrait and >= 0)
            _faces.PicturePanel(portrait);
    }

    /// <summary>A bar button as the mouse presses it (unattended play goes through here).</summary>
    public void PressButton(int button)
    {
        if (!HasButton(button))
            throw new InvalidOperationException($"The bar has no button {button} here.");
        ButtonPressed?.Invoke(button);
    }

    // ---------------------------------------------------------------- the parts and the input

    /// <summary>Whether the bar has button <paramref name="button"/> here ("---": none).</summary>
    private bool HasButton(int button) => _labels[button - 1].Trim().Trim('-', '－', 'ー') != "";

    /// <summary>The bar's buttons that send the party (1, and 2 in a defence) and Stop (3), with their places.</summary>
    private IEnumerable<(int Button, Rect2 Rect)> BarButtons()
    {
        int[] sends = [.. new[] { 1, 2 }.Where(HasButton)];
        float x = BarArea.Position.X;
        float w = sends.Length == 0 ? 0 : (BarArea.Size.X - StopWidth - ButtonGap * sends.Length) / sends.Length;
        foreach (int button in sends)
        {
            yield return (button, new Rect2(x, BarArea.Position.Y, w, BarArea.Size.Y));
            x += w + ButtonGap;
        }
        if (HasButton(3))
            yield return (3, new Rect2(BarArea.End.X - StopWidth, BarArea.Position.Y, StopWidth, BarArea.Size.Y));
    }

    /// <summary>Place <paramref name="place"/>'s card: the back row (3, 4) on the left, the front row (1, 2) on the right.</summary>
    private static Rect2 CardRect(int place)
    {
        float x0 = PartyPanel.Position.X + 14, x1 = PartyPanel.End.X - 14 - FacingStrip - FacingGap, mid = (x0 + x1) / 2;
        float w = mid - 3 - x0;
        float x = place is 3 or 4 ? x0 : mid + 3;
        float y = CardsTop + (place is 2 or 4 ? CardHeight + CardGap : 0);
        return new Rect2(x, y, w, CardHeight);
    }

    private static Rect2 RemoveRect(int place)
    {
        var card = CardRect(place);
        return new Rect2(card.End.X - 26, card.Position.Y + 6, 20, 20);
    }

    /// <summary>The offered place at <paramref name="at"/> (screen pixels), or 0.</summary>
    private int PlaceAt(Vector2 at)
    {
        for (int place = 1; place <= (_party?.Places.Count ?? 0); place++)
            if (CardRect(place).HasPoint(at))
                return place;
        return 0;
    }

    private DeploymentMember? MemberAt(int place) => place >= 1 && _party is { } p && place <= p.Places.Count ? p.Places[place - 1] : null;

    private int PartAt(Vector2 at)
    {
        foreach (var (button, rect) in BarButtons())
            if (rect.HasPoint(at))
                return button;
        int place = PlaceAt(at);
        return MemberAt(place) is not null && RemoveRect(place).HasPoint(at) ? RemoveButton + place : NoPart;
    }

    private Vector2 ScreenPoint(Vector2 local) => (local - _origin) / _k;

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                Hover(ScreenPoint(motion.Position));
                if (_dragFrom != 0)
                    DragTo(motion.Position);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                RightPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                var at = ScreenPoint(click.Position);
                int part = PartAt(at);
                if (click.Pressed)
                {
                    EndDrag();
                    _pressed = part;
                    _dragFrom = part == NoPart && !Busy && MemberAt(PlaceAt(at)) is not null ? PlaceAt(at) : 0;
                    _dragStart = click.Position;
                }
                else
                {
                    if (_dragging)
                    {
                        int from = _dragFrom, to = _dropOn;
                        EndDrag();
                        if (to != 0 && !Busy)
                            Dropped?.Invoke(from, to);
                    }
                    else if (part != NoPart && part == _pressed)
                        Click(part);
                    _dragFrom = 0;
                    _pressed = NoPart;
                }
                _screen.QueueRedraw();
                AcceptEvent();                                  // a left click elsewhere does nothing
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
            Hover(new Vector2(-1, -1));
    }

    private void Hover(Vector2 at)
    {
        int part = PartAt(at), place = PlaceAt(at);
        if (part == _hovered && place == _hoveredPlace)
            return;
        _hovered = part;
        _hoveredPlace = place;
        _screen.QueueRedraw();
    }

    private void Click(int part)
    {
        if (part >= RemoveButton)
        {
            if (MemberAt(part - RemoveButton) is { } member)
                RemovePressed?.Invoke(member.Unit);
        }
        else
            ButtonPressed?.Invoke(part);
    }

    public override string _GetTooltip(Vector2 atPosition) =>
        PartAt(ScreenPoint(atPosition)) >= RemoveButton ? "Take out of the party" : "";

    /// <summary>
    /// A left press on a member's card, moved further than <see cref="DragDistance"/>, drags it: the card dims, its
    /// face follows the pointer and the place under it glows; released over another place, the drop goes to
    /// <see cref="Dropped"/>. No drag starts while <see cref="Busy"/>.
    /// </summary>
    private void DragTo(Vector2 local)
    {
        if (!_dragging)
        {
            if (local.DistanceTo(_dragStart) < DragDistance || Busy)
                return;
            _dragging = true;
            _pressed = NoPart;
        }
        int over = PlaceAt(ScreenPoint(local));
        _dropOn = over != _dragFrom ? over : 0;
        _screen.QueueRedraw();
    }

    /// <summary>Any drag under way given up.</summary>
    private void EndDrag()
    {
        _dragFrom = 0;
        _dragging = false;
        _dropOn = 0;
        _screen.QueueRedraw();
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    private void DrawBackdrop(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        Lacquer.DrawWaveBackdrop(c, _k);
    }

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawTablePanel(c);
        DrawPicture(c);
        DrawParty(c);
        if (_dragging && MemberAt(_dragFrom) is { } dragged)
            DrawGhost(c, dragged);
    }

    /// <summary>The table's panel: the title band (the screen's name, the mode and the places offered, the date) over the ledger.</summary>
    private void DrawTablePanel(Control c)
    {
        Lacquer.DrawPanel(c, TablePanel, _k);
        var band = new Rect2(TablePanel.Position.X + 22, TablePanel.Position.Y + 8, TablePanel.Size.X - 44, 34);
        const string title = "Deployment";
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 24)), title, fontSize: 24, modulate: Lacquer.Name);
        float tw = _brush.GetStringSize(title, fontSize: 24).X;
        int places = _party?.Places.Count ?? 0;
        string mode = _mode switch { 1 => "Attack", 2 => "Defence", 3 => "Dungeon search", _ => "" };
        string subtitle = (mode == "" ? "" : mode + " · ") + (places == 1 ? "1 unit" : $"up to {places} units");
        c.DrawString(_cinzelLight, new Vector2(band.Position.X + tw + 16, Lacquer.Baseline(band, _cinzelLight, 13) + 1),
            subtitle, fontSize: 13, modulate: Lacquer.Subtitle);
        float dw = _cinzel.GetStringSize(_date, fontSize: 15).X;
        c.DrawString(_cinzel, new Vector2(band.End.X - dw, Lacquer.Baseline(band, _cinzel, 15) + 1), _date, fontSize: 15, modulate: Lacquer.Name);
        Lacquer.DrawDivider(c, TablePanel.Position.X + 16, TablePanel.End.X - 16, TablePanel.Position.Y + 44, _k);
    }

    /// <summary>The picture panel with a shadow under it (its gold frame is the original's).</summary>
    private void DrawPicture(Control c)
    {
        var p = PictureArea;
        for (int i = 10; i >= 1; i--)
            c.DrawRect(new Rect2(p.Position.X - i + 3, p.Position.Y - i + 8, p.Size.X + 2 * i, p.Size.Y + 2 * i), new Color(0, 0, 0, 0.06f));
        if (_picture is not null)
            c.DrawTextureRect(_picture, p, false);
        else
            c.DrawRect(p, Colors.Black);
    }

    /// <summary>The party's panel: its header with the budget, the rows' labels, the cards, the totals and the bar.</summary>
    private void DrawParty(Control c)
    {
        Lacquer.DrawPanel(c, PartyPanel, _k);
        if (_party is not { } party)
            return;
        var head = new Rect2(PartyPanel.Position.X + 18, PartyPanel.Position.Y + 10, PartyPanel.Size.X - 36, 30);
        c.DrawString(_brush, new Vector2(head.Position.X, Lacquer.Baseline(head, _brush, 20)), "Party", fontSize: 20, modulate: Lacquer.Name);
        float x = head.End.X;
        x = RightString(c, _cinzelLight, "K GOLD", 11, Lacquer.Subtitle, x, head) - 6;
        x = RightString(c, _cinzel, Figures.Number(party.Gold * 100), 20, Lacquer.Name, x, head) - 10;
        RightString(c, _cinzelLight, "Budget", 12, Lacquer.Label, x, head);
        Lacquer.DrawDivider(c, PartyPanel.Position.X + 14, PartyPanel.End.X - 14, PartyPanel.Position.Y + 44, _k);

        var back = CardRect(3);
        var front = CardRect(1);
        CentredString(c, _cinzel, "Back row", 13, Lacquer.Label, back.GetCenter().X, RowLabelsY + 5);
        const string frontLabel = "Front row";
        float fw = _cinzel.GetStringSize(frontLabel, fontSize: 13).X;
        CentredString(c, _cinzel, frontLabel, 13, Lacquer.Label, front.GetCenter().X - 8, RowLabelsY + 5);
        c.DrawString(_gothic, new Vector2(front.GetCenter().X - 8 + fw / 2 + 6, RowLabelsY + 5), "▶", fontSize: 11, modulate: Lacquer.Label);

        for (int place = 1; place <= party.Places.Count; place++)
            DrawCard(c, place, party.Places[place - 1]);
        if (_dropOn != 0)
            DrawDrop(c, _dropOn);
        DrawFacing(c);

        float left = PartyPanel.Position.X + 20;
        var totals = new Rect2(left, TotalsY - 14, PartyPanel.Size.X - 40, 28);
        DrawFigure(c, totals.Position.X, totals, "Troops", Figures.Number(party.Troops), Lacquer.Value);
        DrawFigure(c, totals.Position.X + 180, totals, "War expenses", Figures.GoldShort(party.Cost * 100),
            party.Affordable ? Lacquer.Value : Lacquer.Minus);
        c.DrawLine(new Vector2(PartyPanel.Position.X + 16, TotalsY + 18), new Vector2(PartyPanel.End.X - 16, TotalsY + 18), Lacquer.DimGold, Line(1));

        int sends = BarButtons().Count(b => b.Button != 3);
        string cost = Figures.GoldShort(party.Cost * 100);
        foreach (var (button, rect) in BarButtons())
        {
            bool hover = button == _hovered, down = hover && button == _pressed;
            string label = _labels[button - 1].Trim();
            if (button == 3)
            {
                if (_mode == 2)
                    label = "Back";                             // a defence's Stop goes back to our attack screen (owner, 2026-10-24)
                c.DrawRect(rect, down ? Lacquer.Row : hover ? Lacquer.RowHover : Lacquer.Row);
                c.DrawRect(rect, hover ? Lacquer.HoverLine : Lacquer.DimGold, false, Line(hover ? 2 : 1));
                CentredString(c, _cinzel, label, 18, hover ? Lacquer.Name : Lacquer.Text, rect.GetCenter().X, Lacquer.Baseline(rect, _cinzel, 18));
                continue;
            }
            Lacquer.DrawRedButton(c, rect, hover, down, _k);
            string? note = (_mode, button) switch
            {
                (2, 1) => "Fight on the plains",
                (2, 2) => "Fight in the city",
                (3, _) => $"Into the dungeon  ·  {cost}",
                _ => null,                                      // Attack alone (owner, 2026-10-24): the war expenses are above it
            };
            int size = sends == 1 ? 21 : 19;
            if (note is null)
            {
                CentredString(c, _brush, label, size + 3, Color.Color8(250, 226, 160), rect.GetCenter().X, Lacquer.Baseline(rect, _brush, size + 3));
                continue;
            }
            var top = new Rect2(rect.Position, new Vector2(rect.Size.X, rect.Size.Y * 0.62f));
            var bottom = new Rect2(rect.Position.X, rect.Position.Y + rect.Size.Y * 0.56f, rect.Size.X, rect.Size.Y * 0.38f);
            CentredString(c, _brush, label, size, Color.Color8(250, 226, 160), rect.GetCenter().X, Lacquer.Baseline(top, _brush, size));
            if (sends == 1)
                CentredString(c, _gothic, note, 13, Color.Color8(236, 206, 160), rect.GetCenter().X, Lacquer.Baseline(bottom, _gothic, 13));
            else
                CentredString(c, _cinzelLight, note, 11, Color.Color8(236, 206, 160), rect.GetCenter().X, Lacquer.Baseline(bottom, _cinzelLight, 11));
        }
    }

    /// <summary>
    /// The way the whole party faces (the owner's F5, 2026-10-24, tools/ui/mock_facing.py; in place of U5's triangle): a
    /// gold bracket holding the front row's two cards from the right — its ends short of the cards' edges, in the gap —,
    /// its middle drawn out into a head pointing at the enemy's side.
    /// </summary>
    private void DrawFacing(Control c)
    {
        var top = CardRect(1);
        var bottom = CardRect(2);
        float x = PartyPanel.End.X - 14 - FacingStrip + 1, ends = top.End.X + 2, tip = PartyPanel.End.X - 10;
        float y0 = top.Position.Y + 1, y1 = bottom.End.Y - 1, cy = (top.End.Y + bottom.Position.Y) / 2;
        float width = Line(2.5f);
        c.DrawPolyline([new(ends, y0), new(x, y0), new(x, cy - 16), new(x + 10, cy - 6)], Lacquer.Gold, width, true);
        c.DrawPolyline([new(ends, y1), new(x, y1), new(x, cy + 16), new(x + 10, cy + 6)], Lacquer.Gold, width, true);
        Lacquer.DrawSmoothPolygon(c, [new(x + 6, cy - 14), new(tip, cy), new(x + 6, cy + 14)], Lacquer.Name, _k);
    }

    /// <summary>A label in Cinzel and its figure in the game's font after it, centred in <paramref name="band"/>.</summary>
    private void DrawFigure(Control c, float x, Rect2 band, string label, string figure, Color colour)
    {
        c.DrawString(_cinzel, new Vector2(x, Lacquer.Baseline(band, _cinzel, 12)), label, fontSize: 12, modulate: Lacquer.Label);
        float lw = _cinzel.GetStringSize(label, fontSize: 12).X;
        c.DrawString(_gothic, new Vector2(x + lw + 10, Lacquer.Baseline(band, _gothic, 17)), figure, fontSize: 17, modulate: colour);
    }

    /// <summary>
    /// Place <paramref name="place"/>'s card: "empty" in a dashed frame where no member stands, else a dark row box —
    /// lit when hovered, dimmed while dragged — with the face in gold, the place on a gold diamond, the name, the
    /// troop type, the troops with their bar, the war expense and the remove button.
    /// </summary>
    private void DrawCard(Control c, int place, DeploymentMember? member)
    {
        var rect = CardRect(place);
        if (member is null)
        {
            c.DrawRect(rect, Lacquer.Row with { A = 0.6f });
            DrawDashed(c, rect, Lacquer.DimGold);
            CentredString(c, _cinzel, place.ToString(CultureInfo.InvariantCulture), 22, Lacquer.DimGold, rect.GetCenter().X, rect.GetCenter().Y + 2);
            CentredString(c, _cinzelLight, "empty", 12, Lacquer.DimGold, rect.GetCenter().X, rect.GetCenter().Y + 24);
            return;
        }
        bool dragged = _dragging && place == _dragFrom;
        float a = dragged ? 0.35f : 1;
        bool hover = !_dragging && place == _hoveredPlace;
        c.DrawRect(rect, (hover ? Lacquer.RowHover : Lacquer.Row) with { A = a });
        c.DrawRect(rect, (hover ? Lacquer.HoverLine : Lacquer.DimGold) with { A = a }, false, Line(hover ? 2 : 1));

        float size = rect.Size.Y - 16;
        var face = new Rect2(rect.Position + new Vector2(8, 8), new Vector2(size, size));
        c.DrawRect(face, Color.Color8(52, 40, 28) with { A = a });
        if (FaceTexture(member.FaceCg, size) is { } texture)
            c.DrawTextureRect(texture, face, false, new Color(1, 1, 1, a));
        c.DrawRect(face, Lacquer.Gold with { A = a }, false, Line(1));

        float tx = face.End.X + 12;
        Lacquer.DrawBadge(c, _cinzel, new Vector2(tx + 10, rect.Position.Y + 19), 11, place);
        if (dragged)
            Lacquer.DrawDiamond(c, new Vector2(tx + 10, rect.Position.Y + 19), 12, Lacquer.Row with { A = 0.65f });
        var nameBand = new Rect2(tx + 28, rect.Position.Y + 8, rect.End.X - 30 - tx - 28, 22);
        c.DrawString(_gothic, new Vector2(nameBand.Position.X, Lacquer.Baseline(nameBand, _gothic, 18)), member.Name,
            HorizontalAlignment.Left, nameBand.Size.X, 18, Lacquer.Name with { A = a });
        var typeBand = new Rect2(tx, rect.Position.Y + 32, rect.End.X - 12 - tx, 20);
        c.DrawString(_gothic, new Vector2(tx, Lacquer.Baseline(typeBand, _gothic, 14)), member.TroopType,
            HorizontalAlignment.Left, typeBand.Size.X, 14, Lacquer.Quiet with { A = a });

        // The troops as current/maximum under the type, the maximum quieter, the strength bar under them; Cost at the foot
        // (owner's mock-up C, 2026-10-24).
        var troopsLine = new Rect2(tx, rect.Position.Y + 52, rect.End.X - 12 - tx, 20);
        float troopsBase = troopsLine.Position.Y + 16;
        string now = Figures.Number(member.Troops);
        c.DrawString(_gothic, new Vector2(tx, troopsBase), now, fontSize: 15, modulate: Lacquer.Text with { A = a });
        float nowWidth = _gothic.GetStringSize(now, fontSize: 15).X;
        c.DrawString(_gothic, new Vector2(tx + nowWidth, troopsBase), "/" + Figures.Number(member.MaxTroops), fontSize: 15,
            modulate: Lacquer.Quiet with { A = a });
        DrawStrength(c, new Rect2(tx, troopsBase + 4, troopsLine.Size.X, 3), member.Troops, member.MaxTroops, a);
        var line = new Rect2(tx, rect.End.Y - 24, rect.End.X - 12 - tx, 20);
        string cost = Figures.GoldShort(member.Cost * 100);
        float cx = RightString(c, _gothic, cost, 15, Lacquer.Text with { A = a }, line.End.X, line) - 6;
        RightString(c, _cinzel, "Cost", 11, Lacquer.Quiet with { A = a }, cx, line);

        var remove = RemoveRect(place);
        bool lit = !_dragging && _hovered == RemoveButton + place;
        c.DrawRect(remove, (lit ? Color.Color8(80, 30, 20) : Color.Color8(40, 22, 16)) with { A = a });
        c.DrawRect(remove, (lit ? Lacquer.Minus : Color.Color8(150, 70, 52)) with { A = a }, false, Line(1));
        CentredString(c, _gothic, "×", 16, (lit ? Lacquer.Text : Lacquer.Minus) with { A = a }, remove.GetCenter().X,
            Lacquer.Baseline(remove, _gothic, 16));
    }

    /// <summary>The troops' strength under their figure: green from three quarters, amber from a third, else red (a look only).</summary>
    private static void DrawStrength(Control c, Rect2 bar, int troops, int max, float a)
    {
        c.DrawRect(bar, new Color(0, 0, 0, 0.47f * a));
        if (max <= 0)
            return;
        float f = Math.Clamp(troops / (float)max, 0, 1);
        var colour = f >= 0.75f ? Lacquer.Plus : f >= 0.35f ? Color.Color8(214, 170, 80) : Lacquer.Minus;
        c.DrawRect(bar with { Size = new Vector2(Math.Max(1, bar.Size.X * f), bar.Size.Y) }, colour with { A = a });
    }

    /// <summary>A frame of short dashes around <paramref name="rect"/>.</summary>
    private void DrawDashed(Control c, Rect2 rect, Color colour) => Lacquer.DrawDashed(c, rect, colour, _k);

    /// <summary>
    /// The place a drag would drop on (the owner's gold highlight, mock-up D3): a gold glow, a bright double frame and a
    /// tag on its top edge — "Swap" onto a member, "Move" onto an empty place (the member goes to the party's end).
    /// </summary>
    private void DrawDrop(Control c, int place)
    {
        var rect = CardRect(place);
        for (int i = 8; i >= 1; i--)
            c.DrawRect(rect.Grow(i), Lacquer.Name with { A = 0.035f });
        c.DrawRect(rect, Lacquer.Name with { A = 0.12f });
        c.DrawRect(rect, Lacquer.HoverLine, false, Line(3));
        c.DrawRect(rect.Grow(-5), Lacquer.Gold, false, Line(1));
        string tag = MemberAt(place) is null ? "Move" : "Swap";
        var box = new Rect2(rect.GetCenter().X - 34, rect.Position.Y - 12, 68, 24);
        c.DrawRect(box, Color.Color8(60, 44, 24));
        c.DrawRect(box, Lacquer.Gold, false, Line(1));
        CentredString(c, _cinzel, tag, 13, Lacquer.Name, box.GetCenter().X, Lacquer.Baseline(box, _cinzel, 13));
    }

    /// <summary>The dragged member's face under the pointer, framed and a little see-through.</summary>
    private void DrawGhost(Control c, DeploymentMember member)
    {
        var at = ScreenPoint(GetLocalMousePosition());
        var rect = new Rect2(at - new Vector2(GhostSize, GhostSize) / 2, new Vector2(GhostSize, GhostSize));
        for (int i = 8; i >= 1; i--)
            c.DrawRect(new Rect2(rect.Position.X - i + 3, rect.Position.Y - i + 6, rect.Size.X + 2 * i, rect.Size.Y + 2 * i), new Color(0, 0, 0, 0.07f));
        c.DrawRect(rect, Color.Color8(52, 40, 28) with { A = 0.9f });
        if (FaceTexture(member.FaceCg, GhostSize) is { } texture)
            c.DrawTextureRect(texture, rect, false, new Color(1, 1, 1, 0.9f));
        c.DrawRect(rect, Lacquer.HoverLine, false, Line(2));
    }

    /// <summary>A face cut to its square at <paramref name="size"/> screen pixels, as many window pixels as it covers.</summary>
    private ImageTexture? FaceTexture(int faceCg, float size) =>
        faceCg == 0 ? null : _faces.SquareTexture(faceCg, _faces.CropOf(faceCg, _crops), Math.Max(16, (int)Math.Round(size * _k)));

    /// <summary>Draws <paramref name="text"/> ending at <paramref name="right"/>, centred in <paramref name="band"/>; returns where it starts.</summary>
    private static float RightString(Control c, Font font, string text, int size, Color colour, float right, Rect2 band)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(right - w, Lacquer.Baseline(band, font, size)), text, fontSize: size, modulate: colour);
        return right - w;
    }

    private static void CentredString(Control c, Font font, string text, int size, Color colour, float centre, float baseline)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(centre - w / 2, baseline), text, fontSize: size, modulate: colour);
    }

    /// <summary>A layer of the screen drawn by its view (the backdrop under the table, the panels over it).</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw() => _paint(this);
    }
}
