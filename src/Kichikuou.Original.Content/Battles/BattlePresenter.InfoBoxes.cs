using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Battles;

// The battle's own info boxes (WAO_SUB 143/153, frame 894) as our battle screen shows them: docs/ui/messages.md, §5 step 5.
public sealed partial class BattlePresenter
{
    /// <summary>The defence's box (<c>L_02078</c>, <c>FIGT.ADV:1399</c>): the unit's name, "prepared for the enemy's attack", the increase.</summary>
    private static readonly ScriptRef DefenceText = new(Figt, 1399);

    /// <summary>The files of the falls' boxes: the player's units' (<c>FIGTﾐｶDE</c>) and the enemy commanders' (<c>FIGTﾃｷDE</c>).</summary>
    private const string PlayerDownFile = "FIGTﾐｶDE.ADV", EnemyDownFile = "FIGTﾃｷDE.ADV";

    /// <summary>The stamp of each fall's box, by its text's line (the 12 boxes of <c>Battle.Resolution.DefeatScenes</c>).</summary>
    private static readonly Dictionary<ScriptRef, string> Stamps = new()
    {
        [new(PlayerDownFile, 13)] = "FAINTED",          // Rance, leader of the thieves
        [new(PlayerDownFile, 35)] = "COLLAPSED",        // Rance
        [new(PlayerDownFile, 62)] = "DEFEATED",         // Sill
        [new(PlayerDownFile, 80)] = "RETREATED",        // Sill
        [new(PlayerDownFile, 98)] = "DIED",             // Sill
        [new(PlayerDownFile, 724)] = "FELL IN BATTLE",
        [new(PlayerDownFile, 733)] = "WOUNDED",
        [new(EnemyDownFile, 13)] = "RETREATED",         // Ex
        [new(EnemyDownFile, 1807)] = "FELL IN BATTLE",
        [new(EnemyDownFile, 1816)] = "CAN'T MOVE",
        [new(EnemyDownFile, 1827)] = "DIED",
        [new(EnemyDownFile, 1836)] = "RAN AWAY",
    };

    /// <summary>
    /// The end's reasons, by the line of their text, with the results plate's title: the time limit (<c>FIGT.ADV:453</c>, then
    /// <c>:458</c> "it became night" or <c>:465</c> "time ran out"), the annihilations and the retreats (<c>:496-591</c>).
    /// </summary>
    private static readonly Dictionary<int, string> Reasons = new()
    {
        [453] = "",
        [458] = "Night Falls",
        [465] = "Time Runs Out",
        [498] = "Defeat",
        [518] = "Victory",
        [542] = "Victory",
        [585] = "Retreat",
    };

    /// <summary>The tally (<c>L_04a2c</c>): its heading, Leazas's or the thieves' (<c>:2994</c> / <c>:2999</c>), then the numbers (<c>:3003</c>).</summary>
    private const int TallyLeazas = 2994, TallyThieves = 2999, TallyNumbers = 3003;

    /// <summary>A level-up's pages (<c>L_04bb7</c>): "X leveled up." (<c>:3065</c>), then "Leader Attacks +1" (<c>:3071</c>) and the gains after it.</summary>
    private const int LevelUpFirst = 3065, LevelUpGains = 3071;

    /// <summary>The script locations (and numbers) of the text printed into the open box since it opened, every page's.</summary>
    private readonly List<(ScriptRef Text, IReadOnlyList<Sys3Int> Values)> _boxTexts = [];

    private (string Title, string Sentence)? _endReason;    // the end's reason, once its box closed
    private BattleResults? _results;                        // the results plate, from the tally's close

    /// <summary>
    /// Whether one of the battle's info boxes is open that our battle screen shows its own way (<see cref="InfoBox"/>): window
    /// 143 or 153 with frame 894, but not those it shows otherwise already — the "vs" window, the order messages, the units'
    /// info, the strategy's and the scout's. The windows' layer leaves its frame out (<see cref="Windows"/>).
    /// </summary>
    private bool InfoBoxOpen => _winType == 43 && _winFrame == 894 && !ActionLabelsOpen && OrderedUnit is null && !_infoBox
        && Strategy is null && Scout is null && !ScoutCalling && !ScoutReplied;

    /// <summary>
    /// The open info box at its key wait as our battle screen shows it (docs/ui/messages.md, §5 step 5), else null: a
    /// defence's badge, a fall's stamp, the results plate, our panel for the rest, or a box shown elsewhere.
    /// </summary>
    public BattleInfoBox? InfoBox => InfoBoxOpen && WaitsForKey && State is not null ? InfoBoxNow() : null;

    private BattleInfoBox InfoBoxNow()
    {
        var s = State!;
        var page = BattleInfoBox.SentencesOf(PageLines());
        var panel = new BattleInfoBox(InfoBoxKind.Panel, page);
        foreach (var (text, values) in _boxTexts)
        {
            if (text == DefenceText)
            {
                int slot = s.Bt_PlayerActSlot.Value;
                if (slot is < 1 or > 4 || values.Count == 0)
                    return panel;
                return new BattleInfoBox(InfoBoxKind.Defence, [DefenceSentence(page)], new BattlePlace(BattleSide.Player, slot),
                    DefenceMark(values[0].Value));
            }
            if (text.File is PlayerDownFile or EnemyDownFile)
            {
                var side = text.File == PlayerDownFile ? BattleSide.Player : BattleSide.Enemy;
                int record = side == BattleSide.Player ? s.Unit_Cur.Value : s.Cmdr_Cur.Value;
                var place = SlotSnapshot.Places.FirstOrDefault(p => p.Side == side && BattleBoard.Record(s, p) == record);
                return Stamps.TryGetValue(text, out var stamp) && record != 0 && place.Slot != 0
                    ? new BattleInfoBox(InfoBoxKind.Fall, page, place, stamp)
                    : panel;
            }
            if (text.File != Figt)
                continue;
            if (Reasons.ContainsKey(text.Line))
                return new BattleInfoBox(InfoBoxKind.Passed, page);
            if (text.Line is TallyLeazas or TallyThieves)
                return Tally() is { } results ? new BattleInfoBox(InfoBoxKind.Results, page, Results: results) : panel;
            if (text.Line == LevelUpFirst)
            {
                if (_results is null)
                    return panel;
                return LevelUp() is { } up
                    ? new BattleInfoBox(InfoBoxKind.Results, page, Results: _results with { LevelUps = [.. _results.LevelUps, up] })
                    : new BattleInfoBox(InfoBoxKind.Passed, page);
            }
        }
        return panel;
    }

    /// <summary>
    /// The open box's page as lines, top to bottom (an empty line for each line's height left empty between them: the
    /// tally's parted paragraph), the half-width spaces between its characters put back.
    /// </summary>
    private IReadOnlyList<string> PageLines()
    {
        var lines = new List<string>();
        int? lastY = null;
        foreach (var row in _windowText.Chars.GroupBy(c => c.Y).OrderBy(row => row.Key))
        {
            int pitch = row.Max(c => c.Size) + 2;
            if (lastY is int y)
                for (int gap = (row.Key - y) / pitch - 1; gap > 0; gap--)
                    lines.Add("");
            lines.Add(MessageWindow.Line(row, _windowText.Rect.X));
            lastY = row.Key;
        }
        return lines;
    }

    /// <summary>
    /// The defence's ribbon text for the printed increase (<c>FIGT.ADV:1399</c>, the slot's new state, 2-21): what it does —
    /// that many points off the enemy's hit chance against the unit till the round's end (docs/battle/02-resolution.md §6.10),
    /// not the Defense stat (owner, 2026-10-21, mock-up N5).
    /// </summary>
    public static string DefenceMark(int increase) => $"ENEMY HIT −{increase}%";

    /// <summary>
    /// The defence's caption: its sentence without the unit's name (the first line, which the caption has apart) and the
    /// increase (the last, the ribbon's), "Prepared for the enemy's attack"; the whole page when it is not so.
    /// </summary>
    private string DefenceSentence(IReadOnlyList<string> page)
    {
        var lines = PageLines();
        string text = lines.Count > 2 ? BattleSpeech.Flow(string.Join('\n', lines.Skip(1).Take(lines.Count - 2))).TrimEnd('.') : "";
        return text.Length == 0 ? string.Join(' ', page) : char.ToUpperInvariant(text[0]) + text[1..];
    }

    /// <summary>
    /// The tally's plate (<c>FIGT.ADV:2977-3044</c>): the reason kept from its box (or the result, <c>Bt_Result</c>, as the
    /// title when the script gave none: a duel), each army's name as the tally prints it and its damages, the EXP.
    /// </summary>
    private BattleResults? Tally()
    {
        int at = _boxTexts.FindIndex(t => t.Text.File == Figt && t.Text.Line is TallyLeazas or TallyThieves);
        int numbersAt = _boxTexts.FindIndex(t => t.Text == new ScriptRef(Figt, TallyNumbers));
        if (at < 0 || numbersAt < 0 || _boxTexts[numbersAt] is not { Values.Count: 3 } numbers)
            return null;
        string player = Read(_boxTexts[at].Text, []).Split(':')[0].Trim();
        // The enemy's name is string 10 (MP 10, 10) on the run's second line, before "： Damages".
        string enemy = Read(numbers.Text, numbers.Values).Split('\n').FirstOrDefault(l => l.Contains("Damages")) is { } line
            ? line.Split(':', '：')[0].Trim() : "";
        string title = _endReason?.Title ?? State!.Bt_Result.Value switch { 2 => "Victory", 1 => "Defeat", _ => "Battle Over" };
        return new BattleResults(title, _endReason?.Sentence ?? "", player, numbers.Values[0].Value, enemy, numbers.Values[1].Value,
            numbers.Values[2].Value, []);
    }

    /// <summary>
    /// A level-up's second page (<c>FIGT.ADV:3071-3160</c>), null on its first: the unit (<c>Unit_Cur</c>, its name as the
    /// battle's cards show it) and the gains — "Leader Attacks +1", then what the rest says as one sentence (a modifier's
    /// "… UP", Mill's phantom beasts, the Popularity Staff's troops; " UP" alone when nothing rose, BTX-09).
    /// </summary>
    private BattleLevelUp? LevelUp()
    {
        int at = _boxTexts.FindIndex(t => t.Text == new ScriptRef(Figt, LevelUpGains));
        if (at < 0)
            return null;
        var s = State!;
        string name = DeploymentTable.StringAt(s, (s.Unit_StrBase + (Sys3Int)((s.Unit_Cur.Value - 1) * 3)).Value).Trim();
        var gains = new List<string> { BattleSpeech.Flow(Read(_boxTexts[at].Text, _boxTexts[at].Values)) };
        string rest = BattleSpeech.Flow(string.Concat(_boxTexts.Skip(at + 1).Select(t => Read(t.Text, t.Values))));
        if (rest.Length > 0)
            gains.Add(rest);
        return new BattleLevelUp(name, gains);
    }

    /// <summary>A text printed into the open window: kept while it is one of the battle's info boxes.</summary>
    private void BoxText(ScriptRef text, IReadOnlyList<Sys3Int> values)
    {
        if (_winType == 43)
            _boxTexts.Add((text, values));
    }

    /// <summary>
    /// An info box closes (op 10): the end's reason is kept for the results plate (a second one — the enemy annihilated
    /// after the night — joins the first and gives the title), the tally starts the plate, a level-up adds its row.
    /// </summary>
    private void CloseInfoBox()
    {
        if (!InfoBoxOpen || State is null)
            return;
        var reasons = _boxTexts.Where(t => t.Text.File == Figt && Reasons.ContainsKey(t.Text.Line)).ToArray();
        if (reasons.Length > 0)
        {
            string sentence = BattleSpeech.Flow(string.Concat(reasons.Select(t => Read(t.Text, t.Values))));
            string title = Reasons[reasons[^1].Text.Line] is { Length: > 0 } t ? t : "Battle Over";
            _endReason = (title, _endReason is { } before ? before.Sentence + " " + sentence : sentence);
        }
        if (InfoBoxNow() is { Kind: InfoBoxKind.Results, Results: { } results })
            _results = results;
    }

    /// <summary>A battle starts or a round begins: the end's reason and plate are gone.</summary>
    private void ForgetResults()
    {
        _endReason = null;
        _results = null;
    }
}
