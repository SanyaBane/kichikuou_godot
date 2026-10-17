using System.Text.RegularExpressions;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A figure of the week report: its label as the files print it, its amount (K GOLD, or troops).</summary>
public sealed record ReportLine(string Label, int Amount);

/// <summary>
/// Our week report (docs/ui/turn-start.md, "The week report"; the owner's mock-up C, 2026-10-18): the windows
/// <c>MAIN.ADV</c> opens at a week's start — the banner ("Leazas Army Phase" and the date), the revenue and the
/// troops replenished, the special expenses — gathered into one screen, with the budget before and after (ours:
/// the state holds it, the windows never showed it). The thieves' week has the banner alone. It is read from the
/// events the windows print (<see cref="WeekLoop"/>'s runs): the texts from the player's files, the figures from
/// the values printed, the budget from <c>Gold</c> at the banner and when the window closes (<c>WAO_SUB</c> 0),
/// which completes it.
/// </summary>
public sealed partial class WeekReport
{
    private const string File = "MAIN.ADV";

    // The runs WeekLoop prints: the banners' titles and dates, the revenue window, the expenses' caption and lines.
    private const int ThievesTitle = 35, ThievesDate = 40, LeazasTitle = 98, LeazasDate = 103, RevenueRun = 173, ExpensesTitle = 188;
    private static readonly int[] ExpenseLines = [194, 201, 208];

    private readonly List<ReportLine> _expenses = [];

    private WeekReport(string title, bool thieves, int budget)
    {
        Title = title;
        Thieves = thieves;
        BudgetBefore = budget;
    }

    /// <summary>The banner's title, "Leazas Army Phase" or "Rance's Thieves Phase".</summary>
    public string Title { get; }

    /// <summary>The thieves' week: the banner alone.</summary>
    public bool Thieves { get; }

    /// <summary>The banner's date as "LP 3 · Month 5 · Week 2".</summary>
    public string Date { get; private set; } = "";

    /// <summary>The budget in K GOLD at the banner, before the revenue (ours).</summary>
    public int BudgetBefore { get; }

    /// <summary>The budget in K GOLD when the window closed, after the expenses (ours); null until then.</summary>
    public int? BudgetAfter { get; private set; }

    /// <summary>The revenue window's caption ("Revenue &amp; Troop Replenishment"), or null.</summary>
    public string? IncomeCaption { get; private set; }

    /// <summary>The revenue in K GOLD.</summary>
    public ReportLine? Revenue { get; private set; }

    /// <summary>The troops the units gained.</summary>
    public ReportLine? Troops { get; private set; }

    /// <summary>The expenses' caption ("Special Expenses (Donations)"), or null when none applies.</summary>
    public string? ExpensesCaption { get; private set; }

    /// <summary>Each expense with its amount in K GOLD (negative), as the files print it ("AL Church: -6500K GOLD").</summary>
    public IReadOnlyList<ReportLine> Expenses => _expenses;

    /// <summary>The window closed: everything the week's start prints is in.</summary>
    public bool Complete { get; private set; }

    /// <summary>
    /// The report a banner's title starts (<c>MAIN.ADV:35</c> for the thieves, <c>:98</c> for Leazas), or null;
    /// <paramref name="text"/> formats a text run with its numbers.
    /// </summary>
    public static WeekReport? Start(ScriptEvent scriptEvent, GameState state, Func<ScriptRef, IReadOnlyList<Sys3Int>, string> text)
    {
        if (Printed(scriptEvent) is not var (at, values) || at.File != File || at.Line is not (ThievesTitle or LeazasTitle))
            return null;
        string title = Lines(text(at, values)).FirstOrDefault() ?? "";
        return new WeekReport(title.Replace('’', '\''), at.Line == ThievesTitle, state.Gold.Value * 100);
    }

    /// <summary>Takes in what the week's start prints after the title; the window's close completes the report.</summary>
    public void Observe(ScriptEvent scriptEvent, GameState state, Func<ScriptRef, IReadOnlyList<Sys3Int>, string> text)
    {
        if (Complete)
            return;
        if (scriptEvent is WindowEvent { Op.Value: 0 })
        {
            Complete = true;
            BudgetAfter = state.Gold.Value * 100;
            return;
        }
        if (Printed(scriptEvent) is not var (at, values) || at.File != File)
            return;
        string[] lines = Lines(text(at, values));
        switch (at.Line)
        {
            case ThievesDate or LeazasDate when lines.Length > 0:
                Date = lines[0].Replace(", ", " · ").Replace(",", " ·");
                break;
            case RevenueRun when lines.Length >= 3 && values.Count >= 2:
                IncomeCaption = lines[0];
                Revenue = new ReportLine(LabelOf(lines[1]), values[0].Value * 100);
                Troops = new ReportLine(LabelOf(lines[2]), values[1].Value);
                break;
            case ExpensesTitle when lines.Length > 0:
                ExpensesCaption = lines[0];
                break;
            case var line when ExpenseLines.Contains(line) && lines.Length > 0:
                _expenses.Add(new ReportLine(LabelOf(lines[0]).Replace('’', '\''), -AmountOf(lines[0])));
                break;
        }
    }

    /// <summary>A run printed: its location and values.</summary>
    private static (ScriptRef At, IReadOnlyList<Sys3Int> Values)? Printed(ScriptEvent scriptEvent) => scriptEvent switch
    {
        MessageEvent e => (e.Text, []),
        ValueMessageEvent e => (e.Text, e.Values),
        _ => null,
    };

    private static string[] Lines(string printed) =>
        [.. printed.Split('\n').Select(l => l.Trim(' ', '　')).Where(l => l.Length > 0)];

    /// <summary>"Revenue: 20200K GOLD" → "Revenue".</summary>
    private static string LabelOf(string line) => line.IndexOfAny([':', '：']) is var at and >= 0 ? line[..at].Trim() : line;

    /// <summary>"AL Church: -6500K GOLD" → 6500.</summary>
    private static int AmountOf(string line) =>
        Digits().Match(line[(line.IndexOfAny([':', '：']) + 1)..]) is { Success: true } m ? int.Parse(m.Value) : 0;

    [GeneratedRegex("[0-9]+")]
    private static partial Regex Digits();
}
