using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cﾀｶﾗ.ADV</c>, the Treasure command (<c>COMMAND.ADV:344-348</c>, always offered;
/// <c>docs/strategy/survey-commands.md</c> §3.9): every item a unit with <c>Unit_Status</c> 0
/// holds is lost — taken off the unit and set to "not owned", not to stock (CMD-18) — then the item
/// screen <c>Sｱｲﾃﾑ</c>. Leaves <c>Item_Cur</c> at 51.
/// </summary>
public sealed class TreasureCommand(OriginalScriptContext script) : CommandFile(script, "Cﾀｶﾗ.ADV")
{
    /// <summary><c>Cﾀｶﾗ.ADV:1-15</c>.</summary>
    public async Task RunAsync()
    {
        State.Item_Cur = 0;
        while (true)                                                    // L_00025
        {
            State.Item_Cur = State.Item_Cur + 1;
            if (State.Item_Cur == 51)
            {
                await Script.CallFileAsync("Sｱｲﾃﾑ.ADV");
                return;
            }
            if (Item.Holder != 0 && Item.Holder != 100)
            {
                State.Unit_Cur = Item.Holder;
                if (Unit.Status == 0)
                {
                    // QUIRK: CMD-18 — the fallen unit's item is lost, not put in stock.
                    Unit.Item = 0;
                    Item.Holder = 0;
                }
            }
        }
    }
}
