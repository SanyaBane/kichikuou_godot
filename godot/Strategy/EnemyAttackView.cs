using System.Globalization;
using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our screen for an enemy attack on a province of ours (our interface; docs/ui/enemy-attack.md, §6; owner, 2026-10-24,
/// mock-ups B and B4): the original's screen dimmed and blurred under it; a title band with the attacker's flag and the
/// announcement; the province left — its picture and what its battles give the defender —, "VS", the army's four places
/// right, unknown until a ninja scouts them and then the report's cards in place; at the foot Maris's line and the
/// answers to <c>FIGT_B</c>'s menu: Intercept, Ninja Scout (a list of the ninjas in service, the spent ones dimmed) and
/// Abandon City (asked first, at its button). It decides nothing of the rules: <see cref="ChooseAsync"/> gives the
/// menu's code and, for a scouting, the ninja's. Points are layout pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class EnemyAttackView : Control
{
    /// <summary>The player's answer: <c>FIGT_B</c>'s menu code (1 Intercept, 2 Ninja Scout, 3 Abandon City) and, for 2, the ninja's.</summary>
    public readonly record struct Answer(int Code, int Ninja);

    private static readonly Rect2 TitleBand = new(32, 24, 1216, 72);
    private static readonly Rect2 ProvincePanel = new(32, 116, 472, 496);
    private static readonly Rect2 ArmyPanel = new(600, 116, 648, 496);
    private static readonly Rect2 Foot = new(32, 632, 1216, 148);
    private static readonly Rect2 PictureArea = new(54, 158, 428, 285);
    private static readonly Rect2 AdvisorArea = new(50, 646, 122, 120);
    private static readonly Rect2 InterceptButton = new(548, 660, 252, 92);
    private static readonly Rect2 ScoutButton = new(822, 676, 190, 52);
    private static readonly Rect2 AbandonButton = new(1030, 676, 196, 52);
    private static readonly Vector2 CardSize = new(282, 150);
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
    private readonly Control _sheet;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontVariation _bold = new() { VariationEmbolden = 0.8f };
    private Font? _font;
    private FaceCrops _crops = new();
    private DeploymentPalette _colours = DeploymentPalette.Chosen;
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
        var backdrop = new ColorRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = new Shader { Code = BlurShader } },
        };
        ((ShaderMaterial)backdrop.Material).SetShaderParameter("dim", Dim);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);
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
        _bold.BaseFont = _font;
        _callout.SetFont(font);
        _sheet.QueueRedraw();
    }

    /// <summary>Option → Display: the army's cards in <paramref name="palette"/>'s colours.</summary>
    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
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

    /// <summary>Lays the screen out over the area whose layout pixel (0, 0) is at <paramref name="origin"/>, <paramref name="scale"/> window pixels to a layout pixel.</summary>
    public void Place(Vector2 origin, float scale)
    {
        _callout.Place(new Rect2(origin, new Vector2(1280, 800) * scale));
        if (origin == _origin && scale == _k)
            return;
        (_origin, _k) = (origin, scale);
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
        if (_attack is not { } attack || _callout.MenuOpen)
            return;
        _listOpen = false;
        _callout.Show([$"Abandon {attack.Province} to the {attack.Army}?", "No battle: the city goes to the enemy."],
            AbandonButton, waiting: false);
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
        if (InterceptButton.HasPoint(p))
            return (Spot.Intercept, 0);
        if (ScoutButton.HasPoint(p) && attack.CanScout)
            return (Spot.Scout, 0);
        if (AbandonButton.HasPoint(p))
            return (Spot.Abandon, 0);
        return (Spot.None, 0);
    }

    private static Rect2 ListRect(EnemyAttack attack)
    {
        float h = 2 * ListPad + ListHead + attack.Ninjas.Count * (ListRow + ListGap) - ListGap;
        float x = Math.Min(ScoutButton.GetCenter().X - ListWidth / 2, 1280 - 24 - ListWidth);
        return new Rect2(x, ScoutButton.Position.Y - 16 - h, ListWidth, h);
    }

    private static Rect2 ListRowRect(EnemyAttack attack, int i)
    {
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
        _sheet.DrawSetTransform(_origin, 0, new Vector2(_k, _k));
        DrawTitle(attack);
        DrawProvince(attack);
        _sheet.DrawString(_brush, new Vector2(552 - _brush.GetStringSize("VS", fontSize: 34).X / 2, 376), "VS",
            fontSize: 34, modulate: Lacquer.Gold);
        DrawArmy(attack);
        DrawFoot(attack);
        if (_listOpen)
            DrawList(attack);
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void DrawTitle(EnemyAttack attack)
    {
        Lacquer.DrawPanel(_sheet, TitleBand, _k);
        if (Picture(attack.FlagCg) is { } flag)
            _sheet.DrawTextureRect(flag, new Rect2(54, 41, 48, 38), false);
        _sheet.DrawString(_brush, new Vector2(116, 70), "Enemy Attack", fontSize: 28, modulate: Lacquer.Name);
        float x = 116 + _brush.GetStringSize("Enemy Attack", fontSize: 28).X + 24;
        _sheet.DrawString(_font!, new Vector2(x, 68), $"The {attack.Army} appeared around {attack.Province}.",
            fontSize: 19, modulate: Lacquer.Text);
        string date = attack.Date.Replace(", ", " · ");
        float dw = _cinzel.GetStringSize(date, fontSize: 13).X;
        _sheet.DrawString(_cinzel, new Vector2(1226 - dw, 66), date, fontSize: 13, modulate: Lacquer.Subtitle);
    }

    private void Heading(float x, float y, string text, float right)
    {
        _sheet.DrawString(_cinzel, new Vector2(x, y + 12), text, fontSize: 13, modulate: Lacquer.Subtitle);
        float end = x + _cinzel.GetStringSize(text, fontSize: 13).X + 10;
        _sheet.DrawLine(new Vector2(end, y + 8), new Vector2(right, y + 8), Lacquer.DimGold, Lacquer.Line(1, _k));
    }

    private void DrawProvince(EnemyAttack attack)
    {
        Lacquer.DrawPanel(_sheet, ProvincePanel, _k);
        Heading(54, 132, "DEFENDING · " + attack.Province.ToUpperInvariant(), 482);
        if (Picture(attack.PictureCg) is { } picture)
            _sheet.DrawTextureRect(picture, PictureArea, false);
        _sheet.DrawRect(PictureArea, Lacquer.Gold, false, Lacquer.Line(1, _k));
        (string Label, string Value)[] rows =
        [
            ("Walls in a city battle", attack.Walls == 0 ? "none" : $"+{attack.Walls}%"),
            ("City battle limit", Figures.Number(attack.CityLimit)),
            ("Plains battle limit", Figures.Number(EnemyAttack.PlainsLimit)),
            ("Revenue", Figures.Number(attack.Revenue) + "K GOLD"),
        ];
        for (int i = 0; i < rows.Length; i++)
        {
            float y = 456 + i * 36;
            _sheet.DrawString(_font!, new Vector2(54, y + 15), rows[i].Label, fontSize: 15, modulate: Lacquer.Label);
            float w = _cinzel.GetStringSize(rows[i].Value, fontSize: 17).X;
            _sheet.DrawString(_cinzel, new Vector2(482 - w, y + 16), rows[i].Value, fontSize: 17, modulate: Lacquer.Value);
            if (i < rows.Length - 1)
                _sheet.DrawLine(new Vector2(54, y + 31), new Vector2(482, y + 31), Color.Color8(60, 46, 30), Lacquer.Line(1, _k));
        }
    }

    private void DrawArmy(EnemyAttack attack)
    {
        Lacquer.DrawPanel(_sheet, ArmyPanel, _k);
        Heading(622, 132, "ATTACKING · " + attack.Army.ToUpperInvariant(), 1226);
        var report = attack.Report;
        for (int slot = 1; slot <= 4; slot++)
        {
            var card = new Rect2(632 + (slot - 1) / 2 * 300, 170 + (slot - 1) % 2 * 166, CardSize.X, CardSize.Y);
            if (report is not null)
            {
                _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                var at = new Rect2(_origin + card.Position * _k, card.Size * _k);
                ScoutReportView.DrawCard(_sheet, at, slot, report.Places[slot - 1], _k, _font!, _bold, _colours, _faces,
                    _crops, Px(17), Px(26));
                _sheet.DrawSetTransform(_origin, 0, new Vector2(_k, _k));
            }
            else
            {
                _sheet.DrawRect(card, Color.Color8(30, 26, 24));
                _sheet.DrawRect(card, Lacquer.OffLine, false, Lacquer.Line(1, _k));
                _sheet.DrawString(_cinzel, card.Position + new Vector2(10, 22), slot.ToString(CultureInfo.InvariantCulture),
                    fontSize: 14, modulate: Lacquer.OffText);
                float qw = _brush.GetStringSize("?", fontSize: 30).X;
                _sheet.DrawString(_brush, card.GetCenter() + new Vector2(-qw / 2, 14), "?", fontSize: 30,
                    modulate: Color.Color8(84, 74, 62));
            }
        }
        _sheet.DrawString(_font!, new Vector2(632, 516), "front row", fontSize: 13, modulate: Lacquer.OffText);
        _sheet.DrawString(_font!, new Vector2(932, 516), "back row", fontSize: 13, modulate: Lacquer.OffText);
        string commanders = report is null ? "Commanders ??" : $"Commanders {report.Commanders}";
        string troops = report is null ? "TOTAL TROOPS ??" : "TOTAL TROOPS " + Figures.Number(report.Troops);
        _sheet.DrawString(_font!, new Vector2(632, 556), commanders, fontSize: 17,
            modulate: report is null ? Lacquer.OffText : Lacquer.Label);
        float tw = _cinzel.GetStringSize(troops, fontSize: 20).X;
        _sheet.DrawString(_cinzel, new Vector2(1216 - tw, 558), troops, fontSize: 20,
            modulate: report is null ? Lacquer.OffText : Lacquer.Value);
        string note = report is null
            ? attack.CanScout ? "Send a ninja to learn them." : "No ninja can be sent this week."
            : $"{NinjaName(attack, report.NinjaCg)}'s report";
        _sheet.DrawString(_font!, new Vector2(632, 582), note, fontSize: 13, modulate: Lacquer.Quiet);
    }

    private static string NinjaName(EnemyAttack attack, int portrait) =>
        attack.Ninjas.FirstOrDefault(n => n.Portrait == portrait)?.Name ?? "The ninja";

    private void DrawFoot(EnemyAttack attack)
    {
        Lacquer.DrawPanel(_sheet, Foot, _k);
        if (Picture(EnemyAttack.AdvisorCg) is { } maris)
            _sheet.DrawTextureRectRegion(maris, AdvisorArea, new Rect2(0, 0, 192, 190));
        _sheet.DrawRect(AdvisorArea, Lacquer.DimGold, false, Lacquer.Line(1, _k));
        _sheet.DrawString(_cinzel, new Vector2(188, 662), "MARIS", fontSize: 13, modulate: Lacquer.Subtitle);
        var lines = attack.AdvisorSays;
        for (int i = 0; i < lines.Count && i < 4; i++)
            _sheet.DrawString(_font!, new Vector2(188, 690 + i * 24), lines[i], HorizontalAlignment.Left, 340, 18,
                Lacquer.Text);

        bool Hovered(Spot spot) => _hover.Spot == spot && !_callout.MenuOpen;
        bool Down(Spot spot) => _pressed.Spot == spot && _hover.Spot == spot;
        Lacquer.DrawRedButton(_sheet, InterceptButton, Hovered(Spot.Intercept), Down(Spot.Intercept), _k);
        Centred(_brush, "Intercept", 22, InterceptButton, 0.40f, Lacquer.Name);
        Centred(_font!, "choose the units", 13, InterceptButton, 0.76f, Color.Color8(236, 206, 160));

        int free = attack.Ninjas.Count(n => n.Free);
        bool canScout = attack.CanScout;
        if (canScout)
            Lacquer.DrawRowBox(_sheet, ScoutButton, Hovered(Spot.Scout) || _listOpen, _k);
        else
        {
            _sheet.DrawRect(ScoutButton, Lacquer.OffButton);
            _sheet.DrawRect(ScoutButton, Lacquer.OffLine, false, Lacquer.Line(1, _k));
        }
        Centred(_cinzel, "Ninja Scout", 17, ScoutButton, 0.40f,
            !canScout ? Lacquer.OffText : Hovered(Spot.Scout) || _listOpen ? Lacquer.Name : Lacquer.Text);
        string sub = attack.Ninjas.Count == 0 ? "no ninja in service" : $"{free} of {attack.Ninjas.Count} free";
        Centred(_font!, sub, 13, ScoutButton, 0.76f, canScout ? Lacquer.Quiet : Lacquer.OffText);

        Lacquer.DrawRowBox(_sheet, AbandonButton, Hovered(Spot.Abandon) || _callout.MenuOpen, _k);
        Centred(_cinzel, "Abandon City", 17, AbandonButton, 0.5f,
            Hovered(Spot.Abandon) || _callout.MenuOpen ? Lacquer.Name : Lacquer.Text);
    }

    private void DrawList(EnemyAttack attack)
    {
        var list = ListRect(attack);
        Lacquer.DrawPanel(_sheet, list, _k);
        float tip = ScoutButton.GetCenter().X;
        _sheet.DrawColoredPolygon([new(tip - 10, list.End.Y - 2), new(tip + 10, list.End.Y - 2), new(tip, list.End.Y + 12)],
            Lacquer.Gold);
        _sheet.DrawString(_cinzel, new Vector2(list.Position.X + ListPad + 4, list.Position.Y + ListPad + 14), "SEND TO SCOUT",
            fontSize: 13, modulate: Lacquer.Subtitle);
        for (int i = 0; i < attack.Ninjas.Count; i++)
        {
            var ninja = attack.Ninjas[i];
            var row = ListRowRect(attack, i);
            bool hover = ninja.Free && _hover == (Spot.Ninja, ninja.Code);
            if (ninja.Free)
                Lacquer.DrawRowBox(_sheet, row, hover, _k);
            else
            {
                _sheet.DrawRect(row, Lacquer.OffButton);
                _sheet.DrawRect(row, Lacquer.OffLine, false, Lacquer.Line(1, _k));
            }
            var face = new Rect2(row.Position + new Vector2(5, 5), new Vector2(40, 40));
            if (_faces.SquareTexture(ninja.Portrait, _faces.CropOf(ninja.Portrait, _crops), (int)Math.Round(40 * _k)) is { } square)
                _sheet.DrawTextureRect(square, face, false, ninja.Free ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
            _sheet.DrawRect(face, Lacquer.DimGold, false, Lacquer.Line(1, _k));
            _sheet.DrawString(_cinzel, row.Position + new Vector2(56, 22), ninja.Name, fontSize: 16,
                modulate: !ninja.Free ? Lacquer.OffText : hover ? Lacquer.Name : Lacquer.Text);
            _sheet.DrawString(_font!, row.Position + new Vector2(56, 42), ninja.Free ? "free this week" : "used this week",
                fontSize: 13, modulate: ninja.Free ? Lacquer.Quiet : Lacquer.OffText);
        }
    }

    private void Centred(Font font, string text, int size, Rect2 rect, float at, Color colour)
    {
        var s = font.GetStringSize(text, fontSize: size);
        float y = rect.Position.Y + rect.Size.Y * at + font.GetAscent(size) - s.Y / 2;
        _sheet.DrawString(font, new Vector2(rect.GetCenter().X - s.X / 2, y), text, fontSize: size, modulate: colour);
    }

    private int Px(float size) => Math.Max(8, (int)Math.Round(size * _k));
}
