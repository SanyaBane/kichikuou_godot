using System.Globalization;
using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our screen for an enemy attack on a province of ours (our interface; docs/ui/enemy-attack.md, §6; owner, 2026-10-24,
/// mock-ups B and B4): the original's screen dimmed and blurred under it; a title band with the attacker's flag and the
/// announcement; the province left — its picture and what its battles give the defender —, "VS", the army's four places
/// right as our party's cards (mock-up E3), unknown until a ninja scouts them and then filled in place; at the foot
/// Maris's face square (§11) and line and the answers to <c>FIGT_B</c>'s menu: Intercept, Ninja Scout (a list of the ninjas in service, the
/// spent ones dimmed) and Abandon City (asked first, at its button). It decides nothing of the rules:
/// <see cref="ChooseAsync"/> gives the menu's code and, for a scouting, the ninja's. Points are layout pixels of the
/// 1280×800 screen; on the phone held landscape the screen spans the whole width in its own layout
/// (<c>EnemyAttackView.Phone.cs</c>), held upright it is stacked over the whole room (<c>EnemyAttackView.Portrait.cs</c>).
/// </summary>
internal sealed partial class EnemyAttackView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>The player's answer: <c>FIGT_B</c>'s menu code (1 Intercept, 2 Ninja Scout, 3 Abandon City) and, for 2, the ninja's.</summary>
    public readonly record struct Answer(int Code, int Ninja);

    // The owner's layout A (2026-10-24, tools/ui/mock_attack_layout.py): the title band, the two panels and the foot one
    // block 20 apart, centred on the screen.
    private const float PanelsHeight = 404, BlockGap = 20;
    private const float TitleTop = (800 - (72 + BlockGap + PanelsHeight + BlockGap + 148)) / 2;
    private const float PanelsTop = TitleTop + 72 + BlockGap, FootTop = PanelsTop + PanelsHeight + BlockGap;
    private static readonly Rect2 TitleBand = new(32, TitleTop, 1216, 72);
    private static readonly Rect2 ProvincePanel = new(32, PanelsTop, 472, PanelsHeight);
    private static readonly Rect2 ArmyPanel = new(600, PanelsTop, 648, PanelsHeight);

    // The army's cards (mock-up E3): the deployment screen's size, a strip for the bracket left of them, the headers, the
    // cards and the totals (328 tall) centred in the panel under its heading.
    private static readonly Vector2 ArmyCardSize = new(252, 104);
    private const float ArmyStrip = 30, ArmyStripGap = 6, ArmyColumnGap = 6, ArmyRowGap = 8;
    private const float ArmyFrontX = 600 + (648 - (ArmyStrip + ArmyStripGap + 252 * 2 + ArmyColumnGap)) / 2 + ArmyStrip + ArmyStripGap;
    private const float ArmyCardsTop = PanelsTop + 34 + (PanelsHeight - 50 - 328) / 2 + 24;
    private static readonly Rect2 Foot = new(32, FootTop, 1216, 148);
    private static readonly Rect2 PictureArea = new(32 + (472 - 300) / 2, PanelsTop + 42, 300, 200);
    // The owner's M2 (2026-10-27, tools/ui/mock_attack_advisor.py): Maris's face square at the army's cards' size, its
    // top at her name's capitals (Cinzel 13 on FootTop + 30), the name and her line right of it.
    private static readonly Rect2 AdvisorFace = new(50, FootTop + 21, 88, 88);
    private const float AdvisorTextX = 156;
    // The answers' row: Intercept, Ninja Scout, Abandon City; in the touch mode mirrored, Abandon City first and Intercept
    // last (owner, 2026-10-27).
    private const float ButtonsLeft = 548, ButtonsRight = 1226;
    private static readonly Rect2 InterceptButton = Mirrored(new(548, FootTop + 28, 252, 92));
    private static readonly Rect2 ScoutButton = Mirrored(new(822, FootTop + 28, 190, 92));
    private static readonly Rect2 AbandonButton = Mirrored(new(1030, FootTop + 44, 196, 52));
    private const float ListWidth = 260, ListRow = 50, ListGap = 6, ListPad = 12, ListHead = 26;
    private const float Dim = 0.66f;

    private const string BlurShader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
        uniform float dim = 0.66;
        void fragment() {
            COLOR = vec4(textureLod(screen, SCREEN_UV, 3.0).rgb * (1.0 - dim), 1.0);
        }
        """;

    private enum Spot { None, Intercept, Scout, Abandon, Ninja }

    private readonly FacePictures _faces;
    private readonly TreasureCallout _callout;
    private readonly ColorRect _blur;
    private readonly Control _sheet;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600), _cinzelBold = Lacquer.Cinzel(700);
    private Font? _font;
    private FaceCrops _crops = new();
    private EnemyAttack? _attack;
    private readonly Dictionary<int, ImageTexture?> _pictures = [];
    private TaskCompletionSource<Answer>? _answer;
    private bool _listOpen;
    private (Spot Spot, int Ninja) _hover, _pressed;
    private Vector2 _origin;
    private float _k = 1;

    public EnemyAttackView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        _blur = new ColorRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = new Shader { Code = BlurShader } },
        };
        ((ShaderMaterial)_blur.Material).SetShaderParameter("dim", Dim);
        _blur.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_blur);
        _sheet = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _sheet.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _sheet.Draw += DrawSheet;
        AddChild(_sheet);
        _callout = new TreasureCallout(font);
        AddChild(_callout);
        SetFont(font);
    }

    /// <summary>The attack shown, or null.</summary>
    public EnemyAttack? Attack => _attack;

    /// <summary>Whether the player is being asked whether to abandon the city.</summary>
    public bool Asking => _callout.MenuOpen;

    /// <summary>Whether the ninjas' list is open.</summary>
    public bool ListOpen => _listOpen;

    /// <summary>Whether the screen waits for the player's answer.</summary>
    public bool Choosing => _answer is not null;

    /// <summary>Option → Display: the text in the chosen font.</summary>
    public void SetFont(GameFont font)
    {
        _font = DeploymentTableView.LoadFont(font);
        _callout.SetFont(font);
        _sheet.QueueRedraw();
    }


    /// <summary>Shows <paramref name="attack"/> (drawn again only when it changed); the faces' squares read as it opens.</summary>
    public void Show(EnemyAttack attack)
    {
        if (!Visible)
        {
            _crops = FaceCropFile.Load();
            Visible = true;
        }
        if (attack.Equals(_attack))
            return;
        _attack = attack;
        _sheet.QueueRedraw();
    }

    /// <summary>Hides the screen; an answer still awaited is cancelled.</summary>
    public void Close()
    {
        Visible = false;
        _attack = null;
        _listOpen = false;
        _hover = _pressed = (Spot.None, 0);
        _callout.CloseMenu();
        _callout.Hide();
        _answer?.TrySetCanceled();
        _answer = null;
    }

    /// <summary>
    /// Lays the screen out over the area whose layout pixel (0, 0) is at <paramref name="origin"/>, <paramref name="scale"/>
    /// window pixels to a layout pixel; on the phone, held either way, over the whole screen in its frame's layout pixels
    /// (LA2, PA1).
    /// </summary>
    public void Place(Vector2 origin, float scale)
    {
        var phone = Device.Held(this);
        if (phone is not null)
        {
            (origin, scale) = (Vector2.Zero, phone.Scale);
            _callout.PlacePhone(phone);
        }
        else
            _callout.Place(new Rect2(origin, new Vector2(1280, 800) * scale));
        if (origin == _origin && scale == _k && phone == _phone)
            return;
        (_origin, _k, _phone) = (origin, scale, phone);
        _blur.Visible = phone is null;                                  // the phone's screen covers it with the wave backdrop
        if (_callout.MenuOpen)
            ShowQuestion();
        _sheet.QueueRedraw();
    }

    /// <summary>Waits for the player's answer to the menu.</summary>
    public Task<Answer> ChooseAsync()
    {
        _answer = new TaskCompletionSource<Answer>();
        return _answer.Task;
    }

    /// <summary>Opens or closes the ninjas' list (also for <c>--auto</c>).</summary>
    public void SetList(bool open)
    {
        _listOpen = open && _attack is { CanScout: true };
        _sheet.QueueRedraw();
    }

    /// <summary>Answers as a click would (<c>--auto</c>): Intercept, the ninja <paramref name="ninja"/>, or Abandon City's question.</summary>
    public void Choose(int code, int ninja = 0)
    {
        if (code == 3)
            _ = AskAbandonAsync();
        else
            Give(new Answer(code, ninja));
    }

    /// <summary>Answers the open question (<c>--auto</c>): Abandon (yes) or Keep.</summary>
    public void AnswerAbandon(bool yes) => _callout.Choose(yes ? 1 : 2);

    private void Give(Answer answer)
    {
        _listOpen = false;
        var waiting = _answer;
        _answer = null;
        waiting?.TrySetResult(answer);
        _sheet.QueueRedraw();
    }

    private async Task AskAbandonAsync()
    {
        if (_attack is null || _callout.MenuOpen)
            return;
        _listOpen = false;
        ShowQuestion();
        _callout.OpenMenu(["Abandon", "Keep"]);
        int answer;
        try
        {
            answer = await _callout.ChooseAsync();
        }
        catch (TaskCanceledException)
        {
            return;
        }
        finally
        {
            _callout.CloseMenu();
            _callout.Hide();
        }
        if (answer == 1)
            Give(new Answer(3, 0));
        _sheet.QueueRedraw();
    }

    /// <summary>Abandon City's question at its button, in the callout's layout (laid out again when the screen is).</summary>
    private void ShowQuestion()
    {
        if (_attack is { } attack)
            _callout.Show([$"Abandon {attack.Province} to the {attack.Army}?", "No battle: the city goes to the enemy."],
                AbandonCell, waiting: false);
    }

    // ------------------------------------------------------------------ input

    public override void _GuiInput(InputEvent e)
    {
        if (_attack is null || _callout.MenuOpen)
            return;
        if (e is InputEventMouseMotion motion)
        {
            var hover = SpotAt(Layout(motion.Position));
            if (hover != _hover)
            {
                _hover = hover;
                _sheet.QueueRedraw();
            }
        }
        else if (e is InputEventMouseButton { ButtonIndex: Godot.MouseButton.Right, Pressed: true } && _listOpen)
        {
            SetList(false);
            AcceptEvent();
        }
        else if (e is InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left } button)
        {
            var spot = SpotAt(Layout(button.Position));
            if (button.Pressed)
                _pressed = spot;
            else if (spot == _pressed)
                Click(spot);
            if (!button.Pressed)
                _pressed = (Spot.None, 0);
            AcceptEvent();
            _sheet.QueueRedraw();
        }
    }

    private void Click((Spot Spot, int Ninja) spot)
    {
        switch (spot.Spot)
        {
            case Spot.Intercept:
                Give(new Answer(1, 0));
                break;
            case Spot.Scout:
                SetList(!_listOpen);
                break;
            case Spot.Ninja:
                Give(new Answer(2, spot.Ninja));
                break;
            case Spot.Abandon:
                _ = AskAbandonAsync();
                break;
            default:
                SetList(false);
                break;
        }
    }

    private Vector2 Layout(Vector2 window) => (window - _origin) / _k;

    private (Spot, int) SpotAt(Vector2 p)
    {
        if (_attack is not { } attack)
            return (Spot.None, 0);
        if (_listOpen)
        {
            for (int i = 0; i < attack.Ninjas.Count; i++)
                if (ListRowRect(attack, i).HasPoint(p))
                    return attack.Ninjas[i].Free ? (Spot.Ninja, attack.Ninjas[i].Code) : (Spot.None, 0);
            if (ListRect(attack).HasPoint(p))
                return (Spot.None, 0);
        }
        if (InterceptRect.HasPoint(p))
            return (Spot.Intercept, 0);
        if (ScoutRect.HasPoint(p) && attack.CanScout)
            return (Spot.Scout, 0);
        if (AbandonRect.HasPoint(p))
            return (Spot.Abandon, 0);
        return (Spot.None, 0);
    }

    /// <summary>Intercept in the screen's layout pixels (the phone's own, LA2 or PA1).</summary>
    private Rect2 InterceptRect => _phone is { } f
        ? new PhoneUnits(f).Rect(f.IsPortrait ? PhoneAttackPortrait.Intercept(f) : PhoneAttack.Intercept(f))
        : InterceptButton;

    /// <summary>Ninja Scout in the screen's layout pixels.</summary>
    private Rect2 ScoutRect => _phone is { } f
        ? new PhoneUnits(f).Rect(f.IsPortrait ? PhoneAttackPortrait.NinjaScout(f) : PhoneAttack.NinjaScout(f))
        : ScoutButton;

    /// <summary>Abandon City in the screen's layout pixels.</summary>
    private Rect2 AbandonRect => _phone is { } f ? new PhoneUnits(f).Rect(PhoneAbandon(f)) : AbandonButton;

    private static PhoneFrame.Box PhoneAbandon(PhoneFrame f) => f.IsPortrait ? PhoneAttackPortrait.Abandon(f) : PhoneAttack.Abandon(f);

    /// <summary>Abandon City in the callout's layout: the PC's layout pixels, or the phone's reference pixels over its whole screen.</summary>
    private Rect2 AbandonCell
    {
        get
        {
            if (_phone is not { } f)
                return AbandonButton;
            var box = PhoneAbandon(f);
            return new Rect2(box.X / f.Unit, box.Y / f.Unit, box.W / f.Unit, box.H / f.Unit);
        }
    }

    private Rect2 ListRect(EnemyAttack attack)
    {
        if (_phone is { } f)
            return new PhoneUnits(f).Rect(f.IsPortrait ? PhoneAttackPortrait.List(f, attack.Ninjas.Count) : PhoneAttack.List(f, attack.Ninjas.Count));
        float h = 2 * ListPad + ListHead + attack.Ninjas.Count * (ListRow + ListGap) - ListGap;
        float x = Math.Min(ScoutButton.GetCenter().X - ListWidth / 2, 1280 - 24 - ListWidth);
        return new Rect2(x, ScoutButton.Position.Y - 16 - h, ListWidth, h);
    }

    /// <summary><paramref name="button"/> as the PC lays it out, in the touch mode mirrored within the answers' row.</summary>
    private static Rect2 Mirrored(Rect2 button) =>
        TouchMode.On ? button with { Position = new(ButtonsLeft + ButtonsRight - button.End.X, button.Position.Y) } : button;

    private Rect2 ListRowRect(EnemyAttack attack, int i)
    {
        if (_phone is { } f)
            return new PhoneUnits(f).Rect(f.IsPortrait
                ? PhoneAttackPortrait.ListRowBox(f, i, attack.Ninjas.Count)
                : PhoneAttack.ListRowBox(f, i, attack.Ninjas.Count));
        var list = ListRect(attack);
        return new Rect2(list.Position.X + ListPad, list.Position.Y + ListPad + ListHead + i * (ListRow + ListGap),
            ListWidth - 2 * ListPad, ListRow);
    }

    // ------------------------------------------------------------------ drawing

    private ImageTexture? Picture(int cg)
    {
        if (!_pictures.TryGetValue(cg, out var texture))
            _pictures[cg] = texture = _faces.Picture(cg) is { } image ? ImageTexture.CreateFromImage(image) : null;
        return texture;
    }

    private void DrawSheet()
    {
        if (_attack is not { } attack || _font is null)
            return;
        if (_phone is { } phone)
        {
            if (phone.IsPortrait)
                DrawPortrait(attack, phone);
            else
                DrawPhone(attack, phone);
            return;
        }
        _sheet.DrawSetTransform(_origin, 0, new Vector2(_k, _k));
        DrawTitle(attack);
        DrawProvince(attack);
        _sheet.DrawString(_brush, new Vector2(552 - _brush.GetStringSize("VS", fontSize: 34).X / 2, PanelsTop + PanelsHeight / 2 + 12), "VS",
            fontSize: 34, modulate: Ui.Gold);
        DrawArmy(attack);
        DrawFoot(attack);
        if (_listOpen)
            DrawList(attack);
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void DrawTitle(EnemyAttack attack)
    {
        Ui.DrawPanel(_sheet, TitleBand, _k);
        if (Picture(attack.FlagCg) is { } flag)
            _sheet.DrawTextureRect(flag, new Rect2(54, TitleTop + 17, 48, 38), false);
        _sheet.DrawString(_brush, new Vector2(116, TitleTop + 46), "Enemy Attack", fontSize: 28, modulate: Ui.Name);
        float x = 116 + _brush.GetStringSize("Enemy Attack", fontSize: 28).X + 24;
        _sheet.DrawString(_font!, new Vector2(x, TitleTop + 44), $"The {attack.Army} appeared around {attack.Province}.",
            fontSize: 19, modulate: Ui.Text);
        string date = attack.Date.Replace(", ", " · ");
        float dw = _cinzelBold.GetStringSize(date, fontSize: 16).X;           // as the other screens' title bands have it
        _sheet.DrawString(_cinzelBold, new Vector2(1226 - dw, TitleTop + 42), date, fontSize: 16, modulate: Ui.Name);
    }

    private void Heading(float x, float y, string text, float right)
    {
        _sheet.DrawString(_cinzel, new Vector2(x, y + 12), text, fontSize: 13, modulate: Ui.Subtitle);
        float end = x + _cinzel.GetStringSize(text, fontSize: 13).X + 10;
        _sheet.DrawLine(new Vector2(end, y + 8), new Vector2(right, y + 8), Ui.DimGold, Lacquer.Line(1, _k));
    }

    private void DrawProvince(EnemyAttack attack)
    {
        Ui.DrawPanel(_sheet, ProvincePanel, _k);
        Heading(54, PanelsTop + 16, "DEFENDING · " + attack.Province.ToUpperInvariant(), 482);
        if (Picture(attack.PictureCg) is { } picture)
            _sheet.DrawTextureRect(picture, PictureArea, false);
        _sheet.DrawRect(PictureArea, Ui.Gold, false, Lacquer.Line(1, _k));
        var rows = FigureRows(attack);
        for (int i = 0; i < rows.Length; i++)
        {
            float y = PictureArea.End.Y + 12 + i * 36;
            _sheet.DrawString(_font!, new Vector2(54, y + 15), rows[i].Label, fontSize: 15, modulate: Ui.Label);
            float w = _cinzel.GetStringSize(rows[i].Value, fontSize: 17).X;
            _sheet.DrawString(_cinzel, new Vector2(482 - w, y + 16), rows[i].Value, fontSize: 17, modulate: Ui.Value);
            if (i < rows.Length - 1)
                _sheet.DrawLine(new Vector2(54, y + 31), new Vector2(482, y + 31), Ui.Seam, Lacquer.Line(1, _k));
        }
    }

    /// <summary>What the province's battles give the defender, under its picture.</summary>
    private static (string Label, string Value)[] FigureRows(EnemyAttack attack) =>
    [
        ("Walls in a city battle", attack.Walls == 0 ? "none" : $"+{attack.Walls}%"),
        ("City battle limit", Figures.Number(attack.CityLimit)),
        ("Plains battle limit", Figures.Number(EnemyAttack.PlainsLimit)),
        ("Revenue", Figures.Number(attack.Revenue) + "K GOLD"),
    ];

    /// <summary>
    /// The army (owner's mock-up E3, 2026-10-24): our party's cards from the deployment screen, the faces on their right
    /// (the enemy across the "VS" looks at us), the front row (1, 2) on the left held by a gold bracket whose head points
    /// at us; unknown places dashed until a ninja scouts them, an empty one as our empty place; the totals under them.
    /// </summary>
    private void DrawArmy(EnemyAttack attack)
    {
        Ui.DrawPanel(_sheet, ArmyPanel, _k);
        Heading(622, PanelsTop + 16, "ATTACKING · " + attack.Army.ToUpperInvariant(), 1226);
        var report = attack.Report;

        var front = ArmyCard(1);
        float labelsY = front.Position.Y - 12;
        CentredAt(_cinzel, "Front row", 13, front.GetCenter().X, labelsY, Ui.Label);   // the bracket says the way (F5t)
        CentredAt(_cinzel, "Back row", 13, ArmyCard(3).GetCenter().X, labelsY, Ui.Label);

        for (int slot = 1; slot <= 4; slot++)
        {
            if (report is null)
                DrawUnknownCard(slot);
            else if (report.Places[slot - 1] is { } commander)
                DrawEnemyCard(slot, commander);
            else
                DrawEmptyCard(slot);
        }
        DrawArmyBracket();

        float y = ArmyCard(2).End.Y + 40, left = front.Position.X, right = ArmyCard(4).End.X;
        _sheet.DrawString(_cinzel, new Vector2(left, y), "Commanders", fontSize: 12, modulate: Ui.Label);
        float cw = _cinzel.GetStringSize("Commanders", fontSize: 12).X;
        _sheet.DrawString(_font!, new Vector2(left + cw + 10, y),
            report is null ? "??" : report.Commanders.ToString(CultureInfo.InvariantCulture), fontSize: 17,
            modulate: report is null ? Ui.OffText : Ui.Value);
        string troops = report is null ? "??" : Figures.Number(report.Troops);
        float tx = right - _font!.GetStringSize(troops, fontSize: 17).X;
        _sheet.DrawString(_font, new Vector2(tx, y), troops, fontSize: 17, modulate: report is null ? Ui.OffText : Ui.Value);
        _sheet.DrawString(_cinzel, new Vector2(tx - 10 - _cinzel.GetStringSize("Total troops", fontSize: 12).X, y), "Total troops",
            fontSize: 12, modulate: Ui.Label);
        _sheet.DrawLine(new Vector2(left, y + 14), new Vector2(right, y + 14), Ui.DimGold, Lacquer.Line(1, _k));
        string note = report is null
            ? attack.CanScout ? "Send a ninja to learn them." : "No ninja can be sent this week."
            : $"{NinjaName(attack, report.NinjaCg)}'s report";
        _sheet.DrawString(_font, new Vector2(left, y + 36), note, fontSize: 13, modulate: Ui.Quiet);
    }

    /// <summary>Place <paramref name="slot"/>'s card: the front row (1, 2) left next to the bracket, the back row (3, 4) right; the block centred in the panel.</summary>
    private static Rect2 ArmyCard(int slot)
    {
        float x = ArmyFrontX + (slot is 1 or 2 ? 0 : ArmyCardSize.X + ArmyColumnGap);
        float y = ArmyCardsTop + (slot is 2 or 4 ? ArmyCardSize.Y + ArmyRowGap : 0);
        return new Rect2(x, y, ArmyCardSize.X, ArmyCardSize.Y);
    }

    /// <summary>A scouted commander as our party's card shows a unit, the face on the right: place, name, type, troops.</summary>
    private void DrawEnemyCard(int slot, ScoutedCommander commander)
    {
        var rect = ArmyCard(slot);
        _sheet.DrawRect(rect, Ui.Row);
        _sheet.DrawRect(rect, Ui.DimGold, false, Lacquer.Line(1, _k));
        float size = rect.Size.Y - 16;
        var face = new Rect2(rect.End.X - 8 - size, rect.Position.Y + 8, size, size);
        DrawFace(face, commander.Picture);

        float tx = rect.Position.X + 12, right = face.Position.X - 12;
        Ui.DrawBadge(_sheet, _cinzel, new Vector2(tx + 10, rect.Position.Y + 19), 11, slot);
        _sheet.DrawString(_font!, new Vector2(tx + 28, rect.Position.Y + 25), commander.Name, HorizontalAlignment.Left,
            right - tx - 28, 18, Ui.Name);
        _sheet.DrawString(_font!, new Vector2(tx, rect.Position.Y + 47), commander.TroopType == "" ? "—" : commander.TroopType,
            HorizontalAlignment.Left, right - tx, 14, Ui.Quiet);
        float baseline = rect.End.Y - 14;
        _sheet.DrawString(_cinzel, new Vector2(tx, baseline), "Troops", fontSize: 11, modulate: Ui.Label);
        string troops = commander.Troops == 0 ? "none" : Figures.Number(commander.Troops);
        _sheet.DrawString(_font!, new Vector2(right - _font!.GetStringSize(troops, fontSize: 17).X, baseline), troops,
            fontSize: 17, modulate: Ui.Value);
        _sheet.DrawLine(new Vector2(tx, baseline + 5), new Vector2(right, baseline + 5), Ui.DimGold, Lacquer.Line(1, _k));
    }

    /// <summary>CG <paramref name="cg"/>'s face square (0: none) on <see cref="UiPalette.FaceBack"/> in a gold line.</summary>
    private void DrawFace(Rect2 face, int cg)
    {
        _sheet.DrawRect(face, Ui.FaceBack);
        if (cg != 0 && _faces.SquareTexture(cg, _faces.CropOf(cg, _crops), Math.Max(16, (int)Math.Round(face.Size.X * _k))) is { } texture)
            _sheet.DrawTextureRect(texture, face, false);
        _sheet.DrawRect(face, Ui.Gold, false, Lacquer.Line(1, _k));
    }

    /// <summary>A place not scouted yet: dashed, a "?" where the face goes, "Unknown".</summary>
    private void DrawUnknownCard(int slot)
    {
        var rect = ArmyCard(slot);
        _sheet.DrawRect(rect, Ui.Row with { A = 0.6f });
        Lacquer.DrawDashed(_sheet, rect, Ui.DimGold, _k);
        float size = rect.Size.Y - 16;
        var face = new Rect2(rect.End.X - 8 - size, rect.Position.Y + 8, size, size);
        Lacquer.DrawDashed(_sheet, face, Ui.OffLine, _k, 4, 8);
        CentredAt(_brush, "?", 30, face.GetCenter().X, face.GetCenter().Y + 12, Ui.DimGold);

        float tx = rect.Position.X + 12, right = face.Position.X - 12;
        var at = new Vector2(tx + 10, rect.Position.Y + 19);
        Lacquer.DrawDiamond(_sheet, at, 11, Ui.DimGold);
        Lacquer.DrawDiamond(_sheet, at, 9, Ui.Row);
        CentredAt(_cinzel, slot.ToString(CultureInfo.InvariantCulture), 12, at.X, at.Y + 5, Ui.DimGold);
        _sheet.DrawString(_cinzel, new Vector2(tx + 28, rect.Position.Y + 25), "Unknown", fontSize: 13, modulate: Ui.OffText);
        float baseline = rect.End.Y - 14;
        _sheet.DrawString(_cinzel, new Vector2(tx, baseline), "Troops", fontSize: 11, modulate: Ui.OffText);
        _sheet.DrawString(_font!, new Vector2(right - _font!.GetStringSize("??", fontSize: 17).X, baseline), "??",
            fontSize: 17, modulate: Ui.OffText);
    }

    /// <summary>A place the army leaves empty: our party's empty place.</summary>
    private void DrawEmptyCard(int slot)
    {
        var rect = ArmyCard(slot);
        _sheet.DrawRect(rect, Ui.Row with { A = 0.6f });
        Lacquer.DrawDashed(_sheet, rect, Ui.DimGold, _k);
        CentredAt(_cinzel, slot.ToString(CultureInfo.InvariantCulture), 22, rect.GetCenter().X, rect.GetCenter().Y + 2, Ui.DimGold);
        CentredAt(_cinzel, "empty", 12, rect.GetCenter().X, rect.GetCenter().Y + 24, Ui.DimGold);
    }

    /// <summary>The deployment screen's bracket mirrored: the front row's two cards held from the left, its head pointing at us.</summary>
    private void DrawArmyBracket() =>
        FacingBracket.DrawPc(_sheet, Ui, _k, ArmyFrontX, ArmyCard(1).Position.Y + 1, ArmyCard(2).End.Y - 1, right: false);

    /// <summary><paramref name="text"/> centred on <paramref name="centre"/> at <paramref name="baseline"/>.</summary>
    private void CentredAt(Font font, string text, int size, float centre, float baseline, Color colour) =>
        _sheet.DrawString(font, new Vector2(centre - font.GetStringSize(text, fontSize: size).X / 2, baseline), text,
            fontSize: size, modulate: colour);

    private static string NinjaName(EnemyAttack attack, int portrait) =>
        attack.Ninjas.FirstOrDefault(n => n.Portrait == portrait)?.Name ?? "The ninja";

    private void DrawFoot(EnemyAttack attack)
    {
        Ui.DrawPanel(_sheet, Foot, _k);
        DrawFace(AdvisorFace, EnemyAttack.AdvisorCg);
        _sheet.DrawString(_cinzel, new Vector2(AdvisorTextX, FootTop + 30), "MARIS", fontSize: 13, modulate: Ui.Subtitle);
        var lines = attack.AdvisorSays;
        for (int i = 0; i < lines.Count && i < 4; i++)
            _sheet.DrawString(_font!, new Vector2(AdvisorTextX, FootTop + 58 + i * 24), lines[i], HorizontalAlignment.Left,
                ButtonsLeft - 20 - AdvisorTextX, 18, Ui.Text);

        bool Hovered(Spot spot) => _hover.Spot == spot && !_callout.MenuOpen;
        bool Down(Spot spot) => _pressed.Spot == spot && _hover.Spot == spot;
        Ui.DrawRedButton(_sheet, InterceptButton, Hovered(Spot.Intercept), Down(Spot.Intercept), _k);
        Centred(_brush, "Intercept", 22, InterceptButton, 0.40f, Ui.Name);
        Centred(_font!, "choose the units", 13, InterceptButton, 0.76f, Color.Color8(236, 206, 160));

        int free = attack.Ninjas.Count(n => n.Free);
        bool canScout = attack.CanScout;
        if (canScout)
            Ui.DrawRowBox(_sheet, ScoutButton, Hovered(Spot.Scout) || _listOpen, _k);
        else
        {
            _sheet.DrawRect(ScoutButton, Ui.OffButton);
            _sheet.DrawRect(ScoutButton, Ui.OffLine, false, Lacquer.Line(1, _k));
        }
        Centred(_cinzel, "Ninja Scout", 17, ScoutButton, 0.40f,
            !canScout ? Ui.OffText : Hovered(Spot.Scout) || _listOpen ? Ui.Name : Ui.Text);
        string sub = attack.Ninjas.Count == 0 ? "no ninja in service" : $"{free} of {attack.Ninjas.Count} free";
        Centred(_font!, sub, 13, ScoutButton, 0.76f, canScout ? Ui.Quiet : Ui.OffText);

        // The owner's P1 (2026-10-26): giving the city up is a loss, so the danger button, a white flag for Delete's bin.
        bool abandonLit = Hovered(Spot.Abandon) || _callout.MenuOpen;
        Ui.DrawDangerButton(_sheet, AbandonButton, abandonLit, _k);
        const string abandon = "Abandon City";
        var size = _cinzel.GetStringSize(abandon, fontSize: 17);
        float x = AbandonButton.GetCenter().X - (size.X + 24) / 2, middle = AbandonButton.GetCenter().Y;
        DrawWhiteFlag(x, middle - 10);
        _sheet.DrawString(_cinzel, new Vector2(x + 24, middle + _cinzel.GetAscent(17) - size.Y / 2), abandon, fontSize: 17,
            modulate: abandonLit ? Ui.Name : Ui.DangerText);
    }

    /// <summary>A white flag on its pole, 16 wide and 18 high times <paramref name="s"/>, its top left at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private void DrawWhiteFlag(float x, float y, float s = 1)
    {
        _sheet.DrawLine(new(x + 2 * s, y), new(x + 2 * s, y + 18 * s), Ui.Text, Lacquer.Line(1.6f * s, _k), true);
        Lacquer.DrawSmoothPolygon(_sheet,
            [new(x + 3 * s, y + s), new(x + 9 * s, y + 3 * s), new(x + 16 * s, y + s), new(x + 16 * s, y + 10 * s), new(x + 9 * s, y + 12 * s),
                new(x + 3 * s, y + 10 * s)],
            Ui.Text, _k);
    }

    private void DrawList(EnemyAttack attack)
    {
        var list = ListRect(attack);
        Ui.DrawPanel(_sheet, list, _k);
        float tip = ScoutButton.GetCenter().X;
        _sheet.DrawColoredPolygon([new(tip - 10, list.End.Y - 2), new(tip + 10, list.End.Y - 2), new(tip, list.End.Y + 12)],
            Ui.Gold);
        _sheet.DrawString(_cinzel, new Vector2(list.Position.X + ListPad + 4, list.Position.Y + ListPad + 14), "SEND TO SCOUT",
            fontSize: 13, modulate: Ui.Subtitle);
        for (int i = 0; i < attack.Ninjas.Count; i++)
        {
            var ninja = attack.Ninjas[i];
            var row = ListRowRect(attack, i);
            bool hover = ninja.Free && _hover == (Spot.Ninja, ninja.Code);
            if (ninja.Free)
                Ui.DrawRowBox(_sheet, row, hover, _k);
            else
            {
                _sheet.DrawRect(row, Ui.OffButton);
                _sheet.DrawRect(row, Ui.OffLine, false, Lacquer.Line(1, _k));
            }
            var face = new Rect2(row.Position + new Vector2(5, 5), new Vector2(40, 40));
            if (_faces.SquareTexture(ninja.Portrait, _faces.CropOf(ninja.Portrait, _crops), (int)Math.Round(40 * _k)) is { } square)
                _sheet.DrawTextureRect(square, face, false, ninja.Free ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
            _sheet.DrawRect(face, Ui.DimGold, false, Lacquer.Line(1, _k));
            _sheet.DrawString(_cinzel, row.Position + new Vector2(56, 22), ninja.Name, fontSize: 16,
                modulate: !ninja.Free ? Ui.OffText : hover ? Ui.Name : Ui.Text);
            _sheet.DrawString(_font!, row.Position + new Vector2(56, 42), ninja.Free ? "free this week" : "used this week",
                fontSize: 13, modulate: ninja.Free ? Ui.Quiet : Ui.OffText);
        }
    }

    private void Centred(Font font, string text, int size, Rect2 rect, float at, Color colour)
    {
        var s = font.GetStringSize(text, fontSize: size);
        float y = rect.Position.Y + rect.Size.Y * at + font.GetAscent(size) - s.Y / 2;
        _sheet.DrawString(font, new Vector2(rect.GetCenter().X - s.X / 2, y), text, fontSize: size, modulate: colour);
    }
}
