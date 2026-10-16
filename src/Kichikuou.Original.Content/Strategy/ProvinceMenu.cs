using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a province menu's command does, for its tag on our panel (ours: the original has no such tags).</summary>
public enum ProvinceAction
{
    /// <summary>Temporary Levy.</summary>
    Levy,

    /// <summary>Bolster Defense.</summary>
    Build,

    /// <summary>Dig Giant Hole, Set Poison Food: the schemes against the generals.</summary>
    Plot,

    /// <summary>One of the city's places (the cells, the academy …), or the AL Church.</summary>
    Visit,

    /// <summary>The Ruins Search.</summary>
    Search,

    /// <summary>Attack, or Pillage in the prologue.</summary>
    Battle,

    /// <summary>Ninja Maneuver.</summary>
    Ninja,
}

/// <summary>
/// One command of a province menu: <paramref name="Choice"/> is the answer to the menu (its 1-based
/// place), <paramref name="Text"/> the item's text without its ○/× mark; a × item (<paramref name="Enabled"/>
/// false) only opens the menu again in the original, so our panel does not offer it.
/// </summary>
public sealed record ProvinceMenuItem(int Choice, string Text, ProvinceAction Action, bool Enabled);

/// <summary>
/// The first menu of a city's file — <c>Cｼﾊｲﾄｼ</c> (ours), <c>Cﾃｷﾄｼ</c> (next to ours), <c>Cﾃｷﾄｼ2</c> (any
/// other) — as our panel shows it over the map (docs/ui/command-screen.md, "The province menu"): the
/// city's name, its army as the files print it, its picture (<c>WAO_SUB</c> 331, <c>Prov.BgCg</c>), the
/// card's figures (<see cref="MapMarkers.CardOf"/>) and the commands; <see cref="Return"/> is Return's
/// choice. The script is answered as the original's menu is: the item's choice, or 0 for a cancel.
/// </summary>
public sealed record ProvinceMenu(
    int Province,
    string Name,
    string Army,
    int Picture,
    IReadOnlyList<string> Card,
    IReadOnlyList<ProvinceMenuItem> Items,
    int Return)
{
    private const string OwnFile = "Cｼﾊｲﾄｼ.ADV", EnemyFile = "Cﾃｷﾄｼ.ADV", DistantFile = "Cﾃｷﾄｼ2.ADV";

    /// <summary>
    /// The province menu <paramref name="items"/> make, their <paramref name="texts"/> as shown (mark and
    /// text), or null when they are not a city's first menu: <c>Cｼﾊｲﾄｼ.ADV:32-507</c> (its Return at
    /// <c>:502</c>), <c>Cﾃｷﾄｼ.ADV:52-82</c> (Return at <c>:81</c>), <c>Cﾃｷﾄｼ2.ADV:50-56</c> (Return at
    /// <c>:55</c>). <paramref name="text"/> reads a text run (its numbers as zeros): the army's name is its first
    /// line that is not blank — <c>Cｼﾊｲﾄｼ.ADV:20</c> for ours (the <c>R</c> before <c>:21</c>, where the run must
    /// start, and the figures after it), <c>Cﾃｷﾄｼ2.ADV:71 + 4 (owner − 2)</c> for the others (the lines <c>Cﾃｷﾄｼ</c>
    /// prints too).
    /// </summary>
    public static ProvinceMenu? For(GameState state, IReadOnlyList<SceneMenuItem> items, IReadOnlyList<string> texts,
        Func<ScriptRef, string?> text)
    {
        if (items.Count == 0 || items.Count != texts.Count)
            return null;
        string file = items[0].At.File;
        int last = items[^1].At.Line;
        if (items.Any(i => i.At.File != file))
            return null;
        Func<int, (ProvinceAction, bool)?> kind = file switch
        {
            OwnFile when last == 502 => OwnItem,
            EnemyFile when last == 81 => EnemyItem,
            DistantFile when last == 55 => line => line == 50 ? (ProvinceAction.Ninja, true) : null,
            _ => _ => null,
        };
        var commands = new List<ProvinceMenuItem>();
        for (int i = 0; i < items.Count - 1; i++)
        {
            if (kind(items[i].At.Line) is not var (action, enabled))
                return null;
            commands.Add(new ProvinceMenuItem(i + 1, Unmarked(texts[i]), action, enabled));
        }
        int p = state.Prov_Cur.Value;
        if (p < 1 || p > state.Prov_Count.Value)
            return null;
        int owner = state.Provs[p].Owner.Value;
        ScriptRef? armyAt = owner == 1 ? new ScriptRef(OwnFile, 20) : owner is >= 2 and <= 11 ? new ScriptRef(DistantFile, 71 + 4 * (owner - 2)) : null;
        string army = armyAt is { } at
            ? ((text(at) ?? "").Split('\n').FirstOrDefault(line => line.Trim('　', ' ').Length > 0) ?? "").Trim('　', ' ', '（', '）', '(', ')')
            : "";
        return new ProvinceMenu(p, MapMarkers.ProvinceName(state, p).Replace('’', '\''), army, state.Provs[p].BgCg.Value,
            MapMarkers.CardOf(state, p), commands, items.Count);
    }

    /// <summary>
    /// <c>Cｼﾊｲﾄｼ</c>'s menu item at <paramref name="line"/> (<c>OwnCityCommand.MenuAsync</c>): the four
    /// commands at 41-59 (× 24 lines below), the places at 91-283 (× 207 below), the Ruins Search's ○
    /// also at 436 in the × block (CMD-09).
    /// </summary>
    private static (ProvinceAction, bool)? OwnItem(int line)
    {
        bool enabled = line == 436 || line is not (65 or 71 or 77 or 83 or 235) && !(line > 294 && line < 500);
        int at = line switch
        {
            436 or 442 => line - 207,
            >= 65 and <= 83 => line - 24,
            > 294 and < 500 => line - 207,
            _ => line,
        };
        return at switch
        {
            41 => (ProvinceAction.Levy, enabled),
            47 => (ProvinceAction.Build, enabled),
            53 or 59 => (ProvinceAction.Plot, enabled),
            229 or 235 => (ProvinceAction.Search, enabled),
            >= 91 and <= 289 => (ProvinceAction.Visit, enabled),
            _ => null,
        };
    }

    /// <summary><c>Cﾃｷﾄｼ</c>'s first menu (<c>EnemyCityCommand.MenuAsync</c>): the AL Church ○ 52 / × 58, Attack 64 or Pillage 70, Ninja Maneuver 76.</summary>
    private static (ProvinceAction, bool)? EnemyItem(int line) => line switch
    {
        52 => (ProvinceAction.Visit, true),
        58 => (ProvinceAction.Visit, false),
        64 or 70 => (ProvinceAction.Battle, true),
        76 => (ProvinceAction.Ninja, true),
        _ => null,
    };

    /// <summary>An item's text without the mark the files put first (○ free, × spent this week).</summary>
    private static string Unmarked(string shown) => shown.TrimStart('○', '×', '△', '◎', ' ', '　').Replace('’', '\'');
}
