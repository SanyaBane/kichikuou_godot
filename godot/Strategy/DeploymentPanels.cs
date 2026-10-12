using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The rest of our own deployment screen around the <see cref="DeploymentTableView"/> (owner's
/// decision 25, docs/ui/deployment-table.md): everything of <c>Sﾌﾞﾀｲ</c>'s screen but the
/// portrait and its description is covered — the details of the hovered unit
/// (<see cref="DeploymentTable.Details"/>), the party with a remove button per member and its
/// totals against the gold (<see cref="DeploymentTable.Party"/>), and the bar's three buttons
/// labelled from the player's files. It decides nothing: the presses go to <see cref="GameScreen"/>,
/// which answers the script with the gestures it expects; a left click elsewhere on it does
/// nothing, a right click is the script's own (the last member out).
/// </summary>
internal sealed partial class DeploymentPanels : Control
{
    /// <summary>The text size in script pixels, as the table's.</summary>
    private const float TextSize = 10;

    // The covered areas, in script pixels: the right half and the left part below the portrait.
    private static readonly Rect2 RightArea = new(320, 0, 320, 400);
    private static readonly Rect2 LeftArea = new(0, 202, 320, 198);
    private static readonly Rect2 DetailsArea = new(6, 204, 314, 46);
    private static readonly Rect2 PartyArea = new(6, 254, 314, 142);
    private static readonly Rect2 BarArea = new(322, 326, 316, 70);

    private static readonly Color Back = new(0.04f, 0.05f, 0.12f);
    private static readonly Color Border = new(0.18f, 0.23f, 0.42f);
    private static readonly Color Muted = new(0.56f, 0.59f, 0.73f);
    private static readonly Color Warning = new(0.88f, 0.48f, 0.48f);

    private readonly ColorRect _right;
    private readonly ColorRect _left;
    private readonly PanelContainer _details;
    private readonly RichTextLabel _detailsText;
    private readonly PanelContainer _party;
    private readonly GridContainer _partyGrid;
    private readonly RichTextLabel _partyTotals;
    private readonly HBoxContainer _bar;
    private readonly Button[] _buttons = new Button[3];
    private readonly Theme _theme = new();
    private string _detailsKey = "?", _partyKey = "?", _barKey = "?";

    /// <summary>The remove button of a member (the unit's number).</summary>
    public event Action<int>? RemovePressed;

    /// <summary>A bar button, 1-3 from the left.</summary>
    public event Action<int>? ButtonPressed;

    /// <summary>A right press anywhere on the panels.</summary>
    public event Action? RightPressed;

    public DeploymentPanels(GameFont font)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Theme = _theme;
        _theme.SetColor("font_color", "Label", new Color(0.95f, 0.95f, 0.95f));
        _theme.SetColor("default_color", "RichTextLabel", new Color(0.95f, 0.95f, 0.95f));
        _right = Backdrop();
        _left = Backdrop();

        _details = Panel();
        _detailsText = Text();
        _details.AddChild(_detailsText);

        _party = Panel();
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _partyGrid = new GridContainer { Columns = 5, MouseFilter = MouseFilterEnum.Ignore };
        _partyGrid.AddThemeConstantOverride("h_separation", 10);
        _partyGrid.AddThemeConstantOverride("v_separation", 0);
        _partyTotals = Text();
        _partyTotals.SizeFlagsVertical = SizeFlags.ExpandFill;
        _partyTotals.VerticalAlignment = VerticalAlignment.Bottom;
        column.AddChild(_partyGrid);
        column.AddChild(_partyTotals);
        _party.AddChild(column);

        _bar = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _bar.AddThemeConstantOverride("separation", 6);
        AddChild(_bar);
        for (int i = 0; i < 3; i++)
        {
            int button = i + 1;
            var b = new Button { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            b.AddThemeStyleboxOverride("normal", ButtonBox(new Color(0.14f, 0.19f, 0.37f)));
            b.AddThemeStyleboxOverride("hover", ButtonBox(new Color(0.22f, 0.29f, 0.52f)));
            b.AddThemeStyleboxOverride("pressed", ButtonBox(new Color(0.1f, 0.13f, 0.27f)));
            b.AddThemeStyleboxOverride("disabled", ButtonBox(new Color(0.08f, 0.1f, 0.18f)));
            b.AddThemeColorOverride("font_disabled_color", new Color(0.35f, 0.38f, 0.5f));
            b.Pressed += () => ButtonPressed?.Invoke(button);
            b.GuiInput += RightClick;
            _buttons[i] = b;
            _bar.AddChild(b);
        }
        SetFont(font);
    }

    /// <summary>Option → Fonts: the panels' text in the chosen font, as the table's.</summary>
    public void SetFont(GameFont font) => _theme.DefaultFont = DeploymentTableView.LoadFont(font);

    /// <summary>Whether the pointer is over the covered areas.</summary>
    public bool HasPointer => Visible && (_right.GetGlobalRect().HasPoint(GetGlobalMousePosition())
        || _left.GetGlobalRect().HasPoint(GetGlobalMousePosition()));

    /// <summary>
    /// Covers the areas over the screen whose script pixel (0, 0) is at <paramref name="origin"/>
    /// (in its parent's coordinates), <paramref name="pixelsPerScript"/> screen pixels to a script pixel.
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        void At(Control c, Rect2 area)
        {
            c.Position = origin + area.Position * pixelsPerScript;
            c.Size = area.Size * pixelsPerScript;
        }
        At(_right, RightArea);
        At(_left, LeftArea);
        At(_details, DetailsArea);
        At(_party, PartyArea);
        At(_bar, BarArea);
        _theme.DefaultFontSize = Math.Max(8, (int)Math.Round(TextSize * pixelsPerScript));
        foreach (var b in _buttons)
            b.AddThemeFontSizeOverride("font_size", Math.Max(10, (int)Math.Round(16 * pixelsPerScript)));
    }

    /// <summary>The panels from <paramref name="state"/> and the bar's labels from <paramref name="picker"/>, filled in again when anything shown changed.</summary>
    public void Refresh(GameState state, PickerView picker)
    {
        var details = DeploymentTable.Details(state);
        string detailsKey = details?.ToString() ?? "";
        if (detailsKey != _detailsKey)
        {
            _detailsKey = detailsKey;
            _detailsText.Text = details is null ? "" :
                $"{details.Name}   {Label("Expense")} {details.Cost * 100}K   {Label("Troops")} {details.Troops}/{details.MaxTroops}\n"
                + $"{Label("Rows")} {(details.Rows == "" ? "—" : details.Rows)}   {Label("Item")} {(details.Item == "" ? "—" : details.Item)}";
        }

        var party = DeploymentTable.Party(state);
        string partyKey = string.Join(";", party.Places) + $"|{party.Troops}|{party.Cost}|{party.Gold}";
        if (partyKey != _partyKey)
        {
            _partyKey = partyKey;
            FillParty(party);
        }

        var labels = picker.BarLabelTexts();
        string barKey = string.Join("|", labels);
        if (barKey != _barKey)
        {
            _barKey = barKey;
            for (int i = 0; i < 3; i++)
            {
                _buttons[i].Text = labels[i];
                _buttons[i].Disabled = labels[i].Trim().Trim('-', '－', 'ー') == "";      // "---": no button there
            }
        }
    }

    /// <summary>A bar button as the mouse presses it (unattended play goes through here).</summary>
    public void PressButton(int button)
    {
        if (_buttons[button - 1].Disabled)
            throw new InvalidOperationException($"The bar has no button {button} here.");
        ButtonPressed?.Invoke(button);
    }

    private void FillParty(DeploymentParty party)
    {
        foreach (var child in _partyGrid.GetChildren())
        {
            _partyGrid.RemoveChild(child);
            child.QueueFree();
        }
        void Cell(string text, Color? color = null, bool right = false, bool expand = false)
        {
            var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.Fill };
            if (right)
                label.HorizontalAlignment = HorizontalAlignment.Right;
            if (color is { } c)
                label.AddThemeColorOverride("font_color", c);
            _partyGrid.AddChild(label);
        }
        Cell("●", Muted);
        Cell("Unit", Muted, expand: true);
        Cell("Expense", Muted, right: true);
        Cell("Troops", Muted, right: true);
        Cell("", Muted);
        foreach (var (member, place) in party.Places.Select((m, i) => (m, i + 1)))
        {
            if (member is null)
            {
                Cell(place.ToString(), Muted);
                Cell("—", Muted, expand: true);
                Cell("", Muted);
                Cell("", Muted);
                Cell("", Muted);
                continue;
            }
            Cell(place.ToString());
            Cell(member.Name, expand: true);
            Cell($"{member.Cost * 100}K", right: true);
            Cell(member.Troops.ToString(), right: true);
            var remove = new Button { Text = "×", Flat = true, FocusMode = FocusModeEnum.None, TooltipText = "Take out of the party" };
            remove.AddThemeColorOverride("font_color", Warning);
            remove.AddThemeColorOverride("font_hover_color", Warning.Lightened(0.3f));
            foreach (var box in new[] { "normal", "hover", "pressed", "focus" })
                remove.AddThemeStyleboxOverride(box, new StyleBoxEmpty());     // as tall as the labels
            int unit = member.Unit;
            remove.Pressed += () => RemovePressed?.Invoke(unit);
            remove.GuiInput += RightClick;
            _partyGrid.AddChild(remove);
        }
        string cost = $"{party.Cost * 100}K";
        _partyTotals.Text = $"{Label("Troop total")} {party.Troops}      {Label("War expenses")} "
            + (party.Affordable ? cost : $"[color=#{Warning.ToHtml(false)}]{cost}[/color]")
            + $" {Label("of")} {party.Gold * 100}K";
    }

    private static string Label(string text) => $"[color=#{Muted.ToHtml(false)}]{text}[/color]";

    private ColorRect Backdrop()
    {
        var rect = new ColorRect { Color = Back, MouseFilter = MouseFilterEnum.Stop };
        rect.GuiInput += e =>
        {
            RightClick(e);
            if (e is InputEventMouseButton)
                rect.AcceptEvent();                                     // a left click on the panels does nothing
        };
        AddChild(rect);
        return rect;
    }

    private void RightClick(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
            RightPressed?.Invoke();
    }

    private PanelContainer Panel()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Back, BorderColor = Border,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 3, ContentMarginBottom = 3,
        });
        AddChild(panel);
        return panel;
    }

    private static RichTextLabel Text() => new()
    {
        BbcodeEnabled = true,
        ScrollActive = false,
        FitContent = true,
        AutowrapMode = TextServer.AutowrapMode.Off,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static StyleBoxFlat ButtonBox(Color color) => new()
    {
        BgColor = color, BorderColor = new Color(0.35f, 0.42f, 0.69f),
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
    };
}
