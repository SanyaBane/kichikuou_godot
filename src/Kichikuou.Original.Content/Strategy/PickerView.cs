using Kichikuou.Content;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a point of a picker screen is to its hit test.</summary>
public enum PickerArea
{
    /// <summary>A cell of the grid: an entry 1-51.</summary>
    Cell,

    /// <summary>A button of the bar under the grid, 1-3 from the left.</summary>
    Button,

    /// <summary>Left of the grid.</summary>
    Outside,
}

/// <summary>
/// A picker screen drawn by hand (owner's decision 9) in the original's layout
/// (docs/strategy/survey-commands.md §7): the file's background and details panel, the 3×17 grid
/// of names from GameState in the colours of their state, the highlight under the entry or bar
/// button the script follows (<c>Tmp0033</c>, <c>Tmp0029</c>, at <c>Tmp0027</c>/<c>Tmp0028</c>),
/// the picture of that entry, the bar's labels from the player's files, and the two text panels
/// the script prints into (<c>B4 8</c>: the party; <c>B4 9</c>: the entry's details). Under our own
/// deployment table the deployment picker's grid is left out (<see cref="GamePresenter.DeploymentGridHidden"/>).
/// </summary>
public sealed class PickerView
{
    private readonly GamePresenter _owner;
    private readonly Dictionary<int, TextBox> _panels = [];
    private IReadOnlyList<string>? _haremNames;
    private Surface? _promptPanel;

    internal PickerView(PickerKind kind, Sys3Int mode, GamePresenter owner)
    {
        Kind = kind;
        Mode = mode;
        _owner = owner;
    }

    /// <summary>Which picker.</summary>
    public PickerKind Kind { get; }

    /// <summary>The mode it was opened in (<c>Sﾌﾞﾀｲ</c>: 1 attack, 2 defence, 3 search).</summary>
    public Sys3Int Mode { get; }

    /// <summary>The panel the script prints into now.</summary>
    internal TextBox? Panel { get; private set; }

    /// <summary>The text of panel <paramref name="window"/> (8 the party, 9 the details), or null.</summary>
    public TextBox? PanelText(int window) => _panels.GetValueOrDefault(window);

    /// <summary><c>B4 n</c>: the script prints into panel n now; it starts empty (the script restored its background).</summary>
    internal void SelectPanel(int window, (int X, int Y, int W, int H)? rect)
    {
        if (!_panels.TryGetValue(window, out var panel))
            _panels[window] = panel = new TextBox(rect ?? (window == 8 ? (8, 274, 296, 120) : (16, 208, 296, 37)));
        else if (rect is { } r && r != panel.Rect)
            panel.Open(r);
        panel.Clear();
        Panel = panel;
    }

    /// <summary>
    /// What is at a screen point, as the files' hit tests divide the screen (<c>Sﾌﾞﾀｲ.ADV:498-523</c>):
    /// the grid right of x 319 and above y 324 (cells 108×18 from (328, 18)), the bar below it
    /// (buttons from x 321, 427 and 533), the rest left of it.
    /// </summary>
    public (PickerArea Area, int Number) HitTest(int x, int y)
    {
        if (x <= 319)
            return (PickerArea.Outside, 0);
        if (y >= 324)
            return (PickerArea.Button, x > 532 ? 3 : x > 426 ? 2 : 1);
        int column = Math.Clamp((x - 328) / 108, 0, 2);
        int row = Math.Max(y - 18, 0) / 18;
        return (PickerArea.Cell, Math.Min(row * 3 + column + 1, 51));
    }

    /// <summary>
    /// Whether entry <paramref name="id"/> is on the list: a unit with a status, a girl of the
    /// harem's list, or in <c>Sｱｲﾃﾑ</c>'s views a unit with a status (0) or an item owned (1; no list in other views).
    /// </summary>
    public bool Listed(GameState s, int id) => id is >= 1 and <= 50 && Kind switch
    {
        PickerKind.Harem => s.Buffer[(Sys3Int)id].Val != 0,
        PickerKind.Items when s.Ui_ItemViewMode == 1 => s.Items[(Sys3Int)id].Holder != 0,
        PickerKind.Items when s.Ui_ItemViewMode != 0 => false,
        _ => s.Units[(Sys3Int)id].Status != 0,
    };

    /// <summary>Draws the screen over what is on <paramref name="screen"/>.</summary>
    internal void Draw(Surface screen, GameState s)
    {
        G(screen, Kind switch
        {
            PickerKind.Deployment => 817,
            PickerKind.Subordinates => 870,
            PickerKind.Items => 819,
            _ => s.StoryStage == 0 ? 694 : 818,
        }, keyed: true);
        G(screen, Background(s));
        if (Kind == PickerKind.Items && Prompt(s) is { } prompt)
            DrawPrompt(screen, s, prompt);

        int hovered = s.Tmp0033.Value;
        bool details = s.Tmp0026 != 0 && Listed(s, hovered);
        if (details)
            DetailsPicture(screen, s, hovered);

        bool grid = !(Kind == PickerKind.Deployment && _owner.DeploymentGridHidden);
        for (int id = 1; id <= 50; id++)
        {
            if (!grid || !Listed(s, id))
                continue;
            int x = 328 + (id - 1) % 3 * 108, y = 18 + (id - 1) / 3 * 18;
            Text(screen, Name(s, id), x, y, 12, 0);
            Text(screen, Name(s, id), x - 1, y, 12, NameColor(s, id));
        }

        if (s.Tmp0029.Value is > 500 and < 600)
            screen.Fill(s.Tmp0027.Value + 10, s.Tmp0028.Value, 70, 2, 240);
        else if (details && grid)
            screen.Fill(s.Tmp0027.Value, s.Tmp0028.Value + 18 + 14 + 2, 90, 2, 240);

        foreach (var (line, x, y, size) in BarLabels())
        {
            if (_owner.Literal(new ScriptRef(File, line)) is not { } label)
                continue;
            Text(screen, label, x + 1, y, size, 0);
            Text(screen, label, x, y, size, 255);
        }

        if (Kind == PickerKind.Deployment)
            PanelText(8)?.Draw(screen, _owner.Glyphs);
        if (details)
            PanelText(9)?.Draw(screen, _owner.Glyphs);
    }

    private string File => Kind switch
    {
        PickerKind.Deployment => "Sﾌﾞﾀｲ.ADV",
        PickerKind.Subordinates => "Sﾌﾞｶ.ADV",
        PickerKind.Items => "Sｱｲﾃﾑ.ADV",
        _ => "Sﾊｰﾚﾑ.ADV",
    };

    /// <summary>
    /// The details panel's background: 600, the harem's 696/700, or <c>Sｱｲﾃﾑ</c>'s by the view its
    /// screen was drawn for (<c>:5-13</c>: 600 the units, 692 the items) — a first pick turns the
    /// view without drawing the screen again, so while it is half made the other view's stays.
    /// </summary>
    private int Background(GameState s)
    {
        if (Kind == PickerKind.Harem)
            return s.StoryStage == 0 ? 696 : 700;
        if (Kind != PickerKind.Items)
            return 600;
        var drawnFor = Prompt(s) switch
        {
            ItemPrompt.UnitFirst => 0,
            ItemPrompt.ItemFirst => 1,
            _ => s.Ui_ItemViewMode.Value,
        };
        return drawnFor switch { 0 => 600, 1 => 692, _ => 0 };
    }

    /// <summary>The bar's labels: line, x, y and size of the <c>MS 1</c> texts (<c>Sﾌﾞﾀｲ.ADV:12-102</c> by mode, "End" of <c>Sﾌﾞｶ.ADV:21</c> / <c>Sﾊｰﾚﾑ.ADV:10</c>, <c>Sｱｲﾃﾑ.ADV:16-44</c>).</summary>
    private IEnumerable<(int Line, int X, int Y, int Size)> BarLabels() => Kind switch
    {
        PickerKind.Deployment when Mode.Value is >= 1 and <= 3 =>
            new[] { new[] { 14, 24, 33 }, [45, 54, 63], [75, 84, 93] }[Mode.Value - 1].Zip([340, 445, 550], (line, x) => (line, x, 333, 24)),
        PickerKind.Deployment => [],
        PickerKind.Subordinates => [(21, 562, 333, 24)],
        PickerKind.Items => [(16, 452 - 8, 332, 12), (25, 475 - 8, 346, 12), (36, 550 + 12, 333, 24)],
        _ => [(10, 562, 333, 24)],
    };

    private string Name(GameState s, int id)
    {
        if (Kind == PickerKind.Items && s.Ui_ItemViewMode == 1)
            return StringVar(s, s.Item_StrBase + (Sys3Int)id - 1);
        if (Kind != PickerKind.Harem)
            return StringVar(s, s.Unit_StrBase + ((Sys3Int)id - 1) * 3);
        _haremNames ??= _owner.StringTable(new ScriptRef(File, 57));   // #L_00848, 1: (Sﾊｰﾚﾑ.ADV:56-58)
        return id - 1 < _haremNames.Count ? Hankaku.ToHalfWidth(_haremNames[id - 1]) : "";
    }

    private static string StringVar(GameState s, Sys3Int index) =>
        index.Value < s.Strings.Length ? Hankaku.ToHalfWidth(s.Strings[index.Value]) : "";

    /// <summary>
    /// The name's colour (<c>Sﾌﾞﾀｲ.ADV:390-407</c>, <c>Sﾌﾞｶ.ADV:67-81</c>, <c>Sﾊｰﾚﾑ.ADV:56-67</c>,
    /// <c>Sｱｲﾃﾑ.ADV:181-226</c>): 255 at status 1, 15 at status 2 (<c>Sｱｲﾃﾑ</c>: 255), 13 in the party
    /// being built; the harem's are all 255; an item 255 in stock, 253 held, 12 without effect;
    /// other states keep the shadow's black.
    /// </summary>
    private int NameColor(GameState s, int id)
    {
        if (Kind == PickerKind.Harem)
            return 255;
        if (Kind == PickerKind.Items && s.Ui_ItemViewMode == 1)
        {
            var item = s.Items[(Sys3Int)id];
            return item.Effect == 0 ? 12 : item.Holder == 100 ? 255 : 253;
        }
        var unit = s.Units[(Sys3Int)id];
        if (Kind == PickerKind.Items)
            return unit.Status == 1 || unit.Status == 2 ? 255 : 0;
        int color = unit.Status == 1 ? 255 : unit.Status == 2 ? 15 : 0;
        if (Kind == PickerKind.Deployment && (id == s.Bt_PlayerParty1.Value || id == s.Bt_PlayerParty2.Value || id == s.Bt_PlayerParty3.Value || id == s.Bt_PlayerParty4.Value))
            color = 13;
        return color;
    }

    /// <summary>
    /// The picture of the entry under the pointer: the unit's portrait (<c>Sﾌﾞﾀｲ.ADV:467</c>,
    /// <c>Sﾌﾞｶ.ADV:144</c>), or the girl's (<c>Sﾊｰﾚﾑ.ADV:131-148</c>: in the prologue only Sill,
    /// Soul and Anasel have one; grown-up Mill another).
    /// </summary>
    private void DetailsPicture(Surface screen, GameState s, int id)
    {
        if (Kind == PickerKind.Items && s.Ui_ItemViewMode == 1)
        {
            G(screen, 750 + id);                                        // Sｱｲﾃﾑ.ADV:475
            return;
        }
        if (Kind != PickerKind.Harem)
        {
            G(screen, s.Units[(Sys3Int)id].PortraitCg.Value);
            return;
        }
        if (s.StoryStage == 0)
            G(screen, id switch { 1 => 699, 11 => 698, 12 => 697, _ => 0 });
        else
            G(screen, id + 700);
        if (s.F_Mill_State > 1 && id == 18)
            G(screen, 695);
    }

    /// <summary><c>Sｱｲﾃﾑ</c>'s half-made pick: which came first.</summary>
    public enum ItemPrompt
    {
        /// <summary>A unit, waiting for its item (<c>Ui_ItemUnitSel</c> alone).</summary>
        UnitFirst,

        /// <summary>An item, waiting for its unit (<c>Ui_ItemSel</c> alone).</summary>
        ItemFirst,
    }

    /// <summary>The item screen's half-made pick, or null (another picker, nothing or both picked).</summary>
    public ItemPrompt? Prompt(GameState s) => Kind != PickerKind.Items ? null
        : s.Ui_ItemUnitSel != 0 && s.Ui_ItemSel == 0 ? ItemPrompt.UnitFirst
        : s.Ui_ItemSel != 0 && s.Ui_ItemUnitSel == 0 ? ItemPrompt.ItemFirst
        : null;

    /// <summary>
    /// The three lines of the half-made pick's prompt (<c>Sｱｲﾃﾑ.ADV:104-110</c> "&lt;unit&gt; / is
    /// equipping / Which item?", <c>:121-128</c> "Equipping / &lt;item&gt; / Which character?"), or
    /// null without one. The strings are the picks' own, also for the bar's 997 (CMD-17).
    /// </summary>
    public IReadOnlyList<string>? PromptLines(GameState s)
    {
        string Line(int line) => _owner.ReadText(new ScriptRef(File, line)).Replace("\n", "");
        return Prompt(s) switch
        {
            ItemPrompt.UnitFirst => [StringVar(s, s.Unit_StrBase + (s.Ui_ItemUnitSel - 1) * 3), Line(107), Line(110)],
            ItemPrompt.ItemFirst => [Line(121), StringVar(s, s.Item_StrBase + s.Ui_ItemSel - 1), Line(128)],
            _ => null,
        };
    }

    /// <summary>
    /// <c>Sｱｲﾃﾑ.ADV:101-110</c>, <c>:118-128</c>: the first pick's prompt, which the script draws on
    /// the screen it keeps — the picture panel as it was at the pick (the background and the
    /// picked entry's picture) shrunk to 186×116 at (11, 273), the lines right of it at x 213.
    /// </summary>
    private void DrawPrompt(Surface screen, GameState s, ItemPrompt prompt)
    {
        _promptPanel ??= new Surface(Surface.ScreenWidth, Surface.ScreenHeight, indexed: false);
        _promptPanel.Palette = screen.Palette;
        _promptPanel.CopyFrom(screen, 6, 6, 314, 196, 6, 6);
        int picked = (prompt == ItemPrompt.UnitFirst ? s.Ui_ItemUnitSel : s.Ui_ItemSel).Value;
        if (picked is >= 1 and <= 50)                                   // the bar's 997 had no details shown
            G(_promptPanel, prompt == ItemPrompt.UnitFirst ? s.Units[(Sys3Int)picked].PortraitCg.Value : 750 + picked);
        screen.ScaledCopyFrom(_promptPanel, 6, 6, 314, 196, 11, 273, 186, 116, 0);
        var lines = PromptLines(s)!;
        for (int i = 0; i < lines.Count; i++)
            Text(screen, lines[i], 11 + 202, 273 + 64 + 16 * i, 12, 255);
    }

    private void G(Surface screen, int number, bool keyed = false)
    {
        if (number > 0 && _owner.Cgs.Get(number) is { } cg)
            screen.DrawCg(cg, cg.X, cg.Y, keyed ? Surface.SpriteKey : null);
    }

    private void Text(Surface screen, string text, int x, int y, int size, int color) =>
        MessageWriter.DrawString(screen, _owner.Glyphs, text, x, y, size, color);
}
