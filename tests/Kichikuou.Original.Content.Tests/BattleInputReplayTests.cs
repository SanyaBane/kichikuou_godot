using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The captured battles replayed through our battle screen's input (docs/ui/battle-screen.md, step 7): the same choices
/// given by our controls give the same memory as the original.
/// </summary>
public class BattleInputReplayTests(ITestOutputHelper output)
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    /// <summary>xsystem35's <c>SYS35KEY_RET</c>: the key a click gives, which our screen gives where it answers itself.</summary>
    private const ushort KeyReturn = 16;

    /// <summary>
    /// Each <c>FIGT_SEL</c> answer comes from our command model — the banner or the card menu's item that gives the
    /// captured command (Auto-Atk / Auto-Def on the acting unit's own slot as its menu's 8 / 9; the commands without a
    /// target with ours, 99, whatever button the original's pointer was on); the retreat confirmation from our dialog; the
    /// key waits our screen answers itself (the "vs" window, an order's message, a later round's header, the scout
    /// report's lines, an exchange's result without a wiped-out mark) with a click's key, as <c>GameScreen</c> does — the
    /// rest keep the capture's. Answers our screen cannot give (Info, Manual on the acting unit, a command without a
    /// target given with the pointer on an enemy's panel, BFL-19) keep the capture's and are listed. The replay must
    /// match the original.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(BattleSceneTests.Captures), MemberType = typeof(BattleSceneTests))]
    public async Task The_captures_replayed_through_our_input_match_the_original(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        var order = new RoundOrder();
        GameState? state = null;
        int answers = 0, ours = 0, keys = 0, otherKeys = 0, dialogs = 0;
        var kept = new List<string>();
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            if (state is { } s)
                order.Observe(e, s);
        });

        FigtSelChoice Command(FigtSelMenu menu, FigtSelChoice captured)
        {
            answers++;
            int code = captured.Command.Value;
            if (code is 53 or 54 || code == 50 && captured.Argument == menu.ActingSlot)
            {
                kept.Add($"({code}, {captured.Argument.Value})");                  // Info; Manual on the acting unit
                return captured;
            }
            var wanted = code is 51 or 52 && captured.Argument == menu.ActingSlot
                ? new FigtSelChoice((ushort)(code - 43), BattleCommands.NoButton)  // its own menu's Auto-Atk / Auto-Def
                : captured;
            int c = wanted.Command.Value;
            bool targeted = c is (>= 1 and <= 3) or (>= 10 and <= 21) or (>= 50 and <= 52);
            var commands = new BattleCommands(menu);
            var offered = Enumerable.Range(1, 4).SelectMany(slot => commands.Banners(new BattlePlace(BattleSide.Enemy, slot))
                .Concat(commands.ContextMenu(new BattlePlace(BattleSide.Player, slot), StandingOrder.Manual)));
            if (offered.FirstOrDefault(o => o.Choice.Command == wanted.Command && (!targeted || o.Choice.Argument == wanted.Argument)) is not { } item)
            {
                kept.Add($"({code}, {captured.Argument.Value}) not offered");
                return captured;
            }
            if (!targeted && captured.Argument.Value - 3 is >= 1 and <= 4 && item.Choice.Argument != captured.Argument)
            {
                // QUIRK: BFL-19 — the original's pointer was on an enemy's panel, so even this command rebinds the enemy
                // slot and Cmdr_Cur; ours carries 99, as the original's with the pointer elsewhere: another input.
                kept.Add($"({code}, {captured.Argument.Value}) over an enemy");
                return captured;
            }
            if (item.Choice != captured)
                ours++;
            return item.Choice;
        }

        Sys3Int? Menu(BattleMenu menu, Sys3Int? captured)
        {
            if (BattleDialog.For(menu) is not { } dialog)
                return captured;                                                // the original's menu stays
            dialogs++;
            Assert.True(captured is null || dialog.Items.Any(i => i.Code == captured), $"{captured} is not in our dialog");
            return captured;                                                    // our dialog's item gives its code; a cancel, null
        }

        Sys3Int Key(Sys3Int captured)
        {
            bool answered = presenter.ActionLabelsOpen || presenter.OrderedUnit is not null
                || presenter.RoundHeader is not null && order.Round > 1 || presenter.ScoutReported
                || presenter.ExchangeResultWaits && !presenter.ExchangeResultWipedOut;
            presenter.AfterKey();
            if (!answered || captured == 0)                                     // under the message skip our screen gives 0 too
                return captured;
            keys++;
            if (captured != KeyReturn)
                otherKeys++;
            return KeyReturn;
        }

        var report = await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            presenter.Start(s);
            order.Reset();
        }, answers: new ReplayAnswers(Command, Menu, Key));
        output.WriteLine($"{file}: {answers} FIGT_SEL answers, {ours} given otherwise by ours, {kept.Count} kept "
            + $"({string.Join(", ", kept.Distinct())}); {keys} key waits ours ({otherKeys} where the original's key was another); "
            + $"{dialogs} dialogs; replay {(report.Passed ? "matches" : "diverges")}");
        if (!report.Passed)
            output.WriteLine(report.Describe(15));
        Assert.True(report.Passed);
    }

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
