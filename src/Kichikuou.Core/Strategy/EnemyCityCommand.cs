using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾃｷﾄｼ.ADV</c>, the enemy city next to the player's land (<c>docs/strategy/survey-commands.md</c>
/// §2-§3.1): the header, the menu (AL Church, Attack/Pillage, Ninja Maneuver, Return) and the
/// prologue's Pillage (<c>Iﾄｳｿﾞｸ</c> 1, then <c>ｵｰﾌﾟﾆﾝｸﾞ</c> if the story stage moved). The attack
/// beyond the prologue (<c>:109-425</c>) and the independent cities come with part P3/P4 and fail
/// as not ported. Called by <c>COMMAND.ADV</c> with the clicked province in <c>Prov_Cur</c>.
/// </summary>
public sealed class EnemyCityCommand(ScriptContext script) : CommandFile(script, "Cﾃｷﾄｼ.ADV")
{
    private enum Choice
    {
        ALChurch,       // L_0021f
        Unavailable,    // L_0010d: the menu again
        Attack,         // L_00230
        Ninja,          // L_009fe
        Return,         // L_00205
    }

    /// <summary><c>Cﾃｷﾄｼ.ADV:1-108</c>: the city's menu until a command leaves the file.</summary>
    public async Task RunAsync()
    {
        State.Ui_CityScouted = 0;                                       // :1
        while (true)
        {
            // L_00025 (:2-16) — Ninja Maneuver comes back here, keeping what the ninja scouted.
            State.Bt_TargetProv = State.Prov_Cur;
            CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);
            State.CallArg = Prov.BgCg;
            await WaoAsync(331);                                        // \L_0539b: the city's picture
            if (State.Ev_CommandMode == 1)                              // :7-16 a forced trip takes over
            {
                await Script.EventAsync("Gｶﾌｪ.ADV", 31);
                return;
            }
            if (State.Ev_CommandMode == 2)
            {
                await Script.EventAsync("Gﾏﾘｱ.ADV", 5);
                return;
            }

            // L_00078 (:17-43) — "［name］ (owner)" and the scouted figures.
            await WaoAsync(113);                                        // \L_052d7
            await TextAsync(20);
            await OwnerAsync();                                         // \L_05185
            if (State.Ui_CityScouted != 1)
                await TextAsync(28);                                    // "Economy: ??  Defense: ??"
            if (State.Ui_CityScouted == 1)
                await ValuesAsync(34, Prov.Economy, Prov.Defense, Prov.BattleArea);

            var choice = await MenuAsync();
            switch (choice)
            {
                case Choice.ALChurch:                                   // L_0021f (:94-98)
                    State.Ui_CmdOwnCityAvail = 0;
                    await Script.EventAsync("GAL.ADV", 7);
                    return;
                case Choice.Attack:
                    await AttackAsync();
                    return;
                case Choice.Ninja:                                      // L_009fe
                    await Script.CallFileAsync("Cﾆﾝｼﾞｬ.ADV");
                    continue;
                default:                                                // L_00205 (:87-93)
                    State.Bt_TargetProv = 0;
                    await WaoAsync(0);                                  // \L_053b7
                    await WaoAsync(332);                                // \L_053a9
                    await Script.MapCallAsync(0);
                    return;
            }
        }
    }

    /// <summary>
    /// <c>L_0010d</c> (<c>:44-86</c>): the release wait, the pointer saved, the menu; a "×" item
    /// starts again from the wait, a cancel puts the pointer back and returns.
    /// </summary>
    private async Task<Choice> MenuAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            await SavePointerAsync();                                   // \L_05289
            var items = new List<(SceneMenuItem Item, Choice Choice)>();
            bool church = State.N_ALChurch_Stage > 3 && State.N_ALChurch_Stage < 20 && State.Prov_Cur == 77;
            if (church && State.Ui_CmdOwnCityAvail != 0)
                items.Add((MenuItem(52), Choice.ALChurch));             // ○Go to AL Church
            if (church && State.Ui_CmdOwnCityAvail == 0)
                items.Add((MenuItem(58), Choice.Unavailable));          // ×Go to AL Church
            if (State.StoryStage != 0)
                items.Add((MenuItem(64), Choice.Attack));               // Attack
            if (State.StoryStage == 0)
                items.Add((MenuItem(70), Choice.Attack));               // Pillage
            if (State.StoryStage != 0)
                items.Add((MenuItem(76), Choice.Ninja));                // Ninja Maneuver
            items.Add((MenuItem(81), Choice.Return));

            int choice = await SelectAsync(items.Select(i => i.Item).ToArray());
            if (choice == 0)
            {
                await RestorePointerAsync();                            // \L_05294, then @L_00205
                return Choice.Return;
            }
            if (items[choice - 1].Choice != Choice.Unavailable)
                return items[choice - 1].Choice;
        }
    }

    /// <summary>
    /// <c>L_00230</c> (<c>:99-108</c>): in the prologue Pillage — <c>Iﾄｳｿﾞｸ</c> 1, and the opening
    /// if the scene moved the story on. The attack of the later stages is part P3.
    /// </summary>
    private async Task AttackAsync()
    {
        await WaoAsync(0);                                              // \L_053b7
        if (State.StoryStage == 0)
        {
            await Script.EventAsync("Iﾄｳｿﾞｸ.ADV", 1);
            if (State.StoryStage != 0)
                await Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");
            return;
        }
        throw NotPorted(109, "the attack beyond the prologue", "P3");
    }

    /// <summary><c>L_05185</c> (<c>:4365-4408</c>): the owner's name after the city's, then a line break.</summary>
    private async Task OwnerAsync()
    {
        int owner = Prov.Owner.Value;
        if (owner is >= 2 and <= 11)
            await TextAsync(4368 + (owner - 2) * 4);
        await LineBreakAsync();
    }
}
