using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A command whose use this week the status strip shows, for its tag's colour (ours).</summary>
public enum WeekAction
{
    /// <summary>Construction (<c>Ui_CmdBuildAvail</c>).</summary>
    Build,

    /// <summary>Harem (<c>Ui_CmdHaremAvail</c>).</summary>
    Harem,

    /// <summary>Merchant (<c>Ui_CmdMerchantAvail</c>).</summary>
    Trade,

    /// <summary>Subordinates as a whole (<c>Ui_CmdSubordAvail</c>): the thieves' week.</summary>
    Units,

    /// <summary>Subordinates' Replenish/Add Troops (<c>CmdAvail_SubTroops</c>).</summary>
    Troops,

    /// <summary>Subordinates' Talk (<c>CmdAvail_SubTalk</c>).</summary>
    Talk,

    /// <summary>An own city's action (<c>Ui_CmdOwnCityAvail</c>).</summary>
    City,

    /// <summary>An attack on an enemy city (<c>Ui_CmdAttackAvail</c>).</summary>
    Attack,

    /// <summary>A dungeon search (<c>Ui_CmdDungeonAvail</c>).</summary>
    Dungeon,
}

/// <summary>One command in the strip: its name, and whether it is still free this week.</summary>
public sealed record WeekUse(string Text, WeekAction Action, bool Free);

/// <summary>A group of the week's commands under its caption (Castle, Subordinates, Map).</summary>
public sealed record WeekGroup(string Caption, IReadOnlyList<WeekUse> Uses);

/// <summary>
/// Our status strip along the top of the map (docs/ui/command-screen.md, "The status strip"; the owner's
/// mock-up C with groups d, 2026-10-16): the status window <c>COMMAND.ADV</c> prints at a right click (the
/// nation, its money and troops, as the files print them), the date with LP (computed by the original, not
/// shown), and the week's uses of the commands, grouped. It reads only the state, as the status window would
/// be printed now: the figures the files compute (<c>CommandPhase.LeazasStatusAsync</c>/<c>ThievesStatusAsync</c>,
/// <c>SetDate</c>) and the weekly flags.
/// </summary>
public sealed record StatusStrip(string Nation, bool Thieves, string Date, IReadOnlyList<(string Label, string Value)> Status,
    IReadOnlyList<WeekGroup> Groups)
{
    private const string File = "COMMAND.ADV";

    /// <summary>What the strip shows, as one string: a view draws again when it changes.</summary>
    public string Key => string.Join("|", [Nation, Date, .. Status.Select(s => $"{s.Label}={s.Value}"),
        .. Groups.SelectMany(g => g.Uses.Select(u => $"{u.Text}:{u.Free}"))]);

    /// <summary>
    /// The strip for <paramref name="state"/>; <paramref name="text"/> formats a text run with its numbers — the
    /// status window's, <c>:443</c> for Leazas, <c>:472</c> for the thieves (story stage 0). Null when the
    /// window's text does not have its four lines.
    /// </summary>
    public static StatusStrip? For(GameState state, Func<ScriptRef, IReadOnlyList<Sys3Int>, string> text)
    {
        bool thieves = state.StoryStage == 0;
        var troops = Sys3Int.Zero;                                       // Sys3Int: the script's sum saturates
        if (thieves)
        {
            foreach (int unit in (int[])[1, 3, 4])                     // COMMAND.ADV:462-468
                troops += state.Units[unit].Troops;
        }
        else
        {
            for (int unit = 1; unit <= 50; unit++)                      // COMMAND.ADV:427-437
                if (state.Units[unit].Status != 0)
                    troops += state.Units[unit].Troops;
        }
        var (year, month, week) = DateOf(state.Week.Value);
        string printed = text(new ScriptRef(File, thieves ? 472 : 443),
            [state.Gold, troops, (Sys3Int)month, (Sys3Int)week]);
        string[] lines = [.. printed.Split('\n').Select(l => l.Trim(' ', '　'))];
        if (lines.Length < 4)
            return null;
        var status = lines[1..3].Select(l => l.IndexOfAny([':', '：']) is var at and >= 0 ? (l[..at].Trim(), l[(at + 1)..].Trim()) : ("", l)).ToArray();
        string date = $"LP {year} · {lines[3].Replace(", ", " · ").Replace(",", " ·")}";
        return new StatusStrip(lines[0].Replace('’', '\''), thieves, date, status, GroupsOf(state, thieves));
    }

    /// <summary>
    /// <c>SetDate</c> (<c>COMMAND.ADV:621-637</c>): LP, month and week from <c>Week</c> — 48 weeks a year
    /// from LP 3, 4 weeks a month.
    /// </summary>
    public static (int Year, int Month, int Week) DateOf(int week)
    {
        int years = (week - 1) / 48;
        int w = week - years * 48;
        int months = (w - 1) / 4;
        return (3 + years, months + 1, w - months * 4);
    }

    /// <summary>
    /// The week's commands: the castle's (Construction, Harem, Merchant), Subordinates' two (Troops, Talk: the
    /// menu's ◎/○/△ is their sum while <c>Ui_CmdSubordAvail</c> is set, △ when it is not), the map's (an own
    /// city, an attack, a dungeon). The thieves' week has Harem and Subordinates in its menu, and the map's attack
    /// (Pillage) without a dungeon: the hideout costs nothing and the other dungeons give no search before stage 1
    /// (<c>Cﾒｲｷｭｳ.ADV:168-173</c>, DGN-20).
    /// </summary>
    private static IReadOnlyList<WeekGroup> GroupsOf(GameState s, bool thieves)
    {
        bool subordinates = s.Ui_CmdSubordAvail != 0;
        var attack = new WeekUse("Attack", WeekAction.Attack, s.Ui_CmdAttackAvail != 0);
        if (thieves)
            return [
                new WeekGroup("Hideout", [new WeekUse("Harem", WeekAction.Harem, s.Ui_CmdHaremAvail != 0),
                    new WeekUse("Subordinates", WeekAction.Units, subordinates)]),
                new WeekGroup("Map", [attack]),
            ];
        return [
            new WeekGroup("Castle", [
                new WeekUse("Construction", WeekAction.Build, s.Ui_CmdBuildAvail != 0),
                new WeekUse("Harem", WeekAction.Harem, s.Ui_CmdHaremAvail != 0),
                new WeekUse("Merchant", WeekAction.Trade, s.Ui_CmdMerchantAvail != 0)]),
            new WeekGroup("Subordinates", [
                new WeekUse("Troops", WeekAction.Troops, subordinates && s.CmdAvail_SubTroops != 0),
                new WeekUse("Talk", WeekAction.Talk, subordinates && s.CmdAvail_SubTalk != 0)]),
            new WeekGroup("Map", [
                new WeekUse("Own city", WeekAction.City, s.Ui_CmdOwnCityAvail > 0),
                attack,
                new WeekUse("Dungeon", WeekAction.Dungeon, s.Ui_CmdDungeonAvail != 0)]),
        ];
    }
}
