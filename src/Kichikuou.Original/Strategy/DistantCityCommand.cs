using Kichikuou.Engine.Script;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cﾃｷﾄｼ2.ADV</c>, a province the player cannot attack (not next to their land; in the prologue
/// every province but 33 and 34), ported whole (<c>docs/strategy/survey-commands.md</c> §2): the
/// header with the scouted figures, then Ninja Maneuver (after the prologue) or Return.
/// </summary>
public sealed class DistantCityCommand(OriginalScriptContext script) : CommandFile(script, "Cﾃｷﾄｼ2.ADV")
{
    /// <summary><c>Cﾃｷﾄｼ2.ADV:1-67</c>: the view until Return.</summary>
    public async Task RunAsync()
    {
        State.Ui_CityScouted = 0;                                       // :1
        while (true)
        {
            // L_00025 (:2-15) — Ninja Maneuver comes back here, keeping what the ninja scouted.
            CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);
            State.CallArg = Prov.BgCg;
            await WaoAsync(331);                                        // \L_0038f: the city's picture
            if (State.Ev_CommandMode == 1)                              // :6-15 a forced trip takes over
            {
                await Script.EventAsync("Gｶﾌｪ.ADV", 31);
                return;
            }
            if (State.Ev_CommandMode == 2)
            {
                await Script.EventAsync("Gﾏﾘｱ.ADV", 5);
                return;
            }

            await WaoAsync(113);                                        // \L_002cb (:16-41)
            await TextAsync(18);                                        // "［name］"
            await OwnerAsync();                                         // \L_00179
            if (State.Ui_CityScouted != 1)
                await TextAsync(26);
            if (State.Ui_CityScouted == 1)
                await ValuesAsync(32, Prov.Economy, Prov.Defense, Prov.BattleArea);

            await WaitForReleaseAsync();                                // L_00107 (:42-58)
            await SavePointerAsync();                                   // \L_0027d
            var items = new List<SceneMenuItem>();
            if (State.StoryStage != 0)
                items.Add(MenuItem(50));                                // Ninja Maneuver
            items.Add(MenuItem(55));                                    // Return
            int choice = await SelectAsync(items);
            if (choice == 0)
                await RestorePointerAsync();                            // \L_00288, then @L_00163
            else if (items[choice - 1].At.Line == 50)
            {
                await Script.CallFileAsync("Cﾆﾝｼﾞｬ.ADV");               // L_00170
                continue;
            }
            await WaoAsync(0);                                          // L_00163: \L_003ab
            await WaoAsync(332);                                        // \L_0039d
            return;
        }
    }

    /// <summary><c>L_00179</c> (<c>:68-111</c>): the owner's name after the city's, then a line break.</summary>
    private async Task OwnerAsync()
    {
        int owner = Prov.Owner.Value;
        if (owner is >= 2 and <= 11)
            await TextAsync(71 + (owner - 2) * 4);
        await LineBreakAsync();
    }
}
