# Battle specification

Read in this order: [01-flow.md](01-flow.md) — entry wrappers, setup, strategy, rounds, turn order, commands, enemy AI, exits, EXP, post-battle (exchange = black box, interface §9); [02-resolution.md](02-resolution.md) — one exchange: setters, specials, flavour text `FIGT_M`, `ComputeExchange`, `ApplyExchange` (interface §1, same procedure names); [03-rng-order.md](03-rng-order.md) — every random draw of a battle in order, mapping 01's R-ids to 02's §8 rows.

Open questions (merged from 01 §14 and 02 §11):

1. ~~Does `IK 0` overwrite `RetVal`?~~ **Resolved (xsystem35 `cmdi.c` `commandIK`):** yes — it
   sets `RetVal` to the pressed key code, or 0 while message skip is on. So the extra flavour
   lines for C83/C84/U3 depend on which key the player pressed; display only, no draws —
   `FIGT_M.ADV:5014-5077`, `8859-8888`; `WAO_SUB.ADV:292-294`, `510-519`.
2. Operator precedence of the Asuka/Mill test (both specs read `*` before `+`) — confirm in the bytecode: `FIGT_M.ADV:8326`.
3. Content of unit record 51 before any kaiju is summoned (drives Goliath/ninja draws) — `FIGT.ADV:1126-1137`, `1972-1979`, `2592`, `2648`.
4. Can the original `system39.exe` `Y 4` return n+1 like xsystem35? — every `Y 4` site, e.g. `FIGT.ADV:2178`.
5. Background overlay test `Bt_EnemyParty1 ≠ 1137` is always true (probably meant `Cmdr_CgFull`) — `FIGT.ADV:1715`.
6. Can `Bt_MsgMode` be outside 1..4 at a Goliath strike (0 at new game), letting `FIGT_M.ADV:5`'s draw pick the side? — `FIGT.ADV:1144`, `FIGT_M.ADV:5-23`, `SET_VER.ADV:515`.
7. Who restores Rance's `Unit_Status` after a prologue defeat? — `FIGTﾐｶDE.ADV:1-21`, `死亡捕獲.ADV:26-39`.
8. Can `Sﾌﾞﾀｲ` return a code other than 501/502/503 to FIGT_B (falls through to plains)? — `FIGT_B.ADV:160-163`.

## Port: how the C# code is split (`src/Kichikuou.Core/Combat/`)

`Battle` is one partial class written by three parts. Follow
[../porting-conventions.md](../porting-conventions.md). Seam methods are `internal`,
parameterless and read their inputs from `GameState` exactly as the script does (callers set
the same variables the script sets before the call).

| Part | Spec | Files it owns | Seam methods it implements |
|---|---|---|---|
| **R1** resolution | 02 §1-4, §6-7, §9, plus `L_03058`/`L_0320f` state effects (01 §9.4) | `Battle.Resolution.cs`, `Battle.Display.cs`, `BattleEvents.Resolution.cs`, tests `Combat/Resolution*Tests.cs` | `PlayerAttackAsync()` (RetVal = attack command 1-3, 10-21: setter → `ManualAttackPrelude` → `Resolve`/`ResolveMagic`), `ResolveAsync()` (`L_025d7`), `ResolveMagicAsync()` (`L_025a0`), `GoliathStrikeAsync()` (`L_01abd`), `RedrawAsync()` (`L_03058`), `HighlightAsync()` (`L_0320f`), `SpendFocus()` (`L_01dd3`), `CompactPlayer()` / `CompactEnemy()` (`L_04762` / `L_04864`, slot argument in the variable the script uses) |
| **R2** flavour text | 02 §5 | `Battle.Flavour.cs`, `BattleEvents.Flavour.cs`, tests `Combat/Flavour*Tests.cs` | `FlavourTextAsync()` (call of `FIGT_M.ADV`) |
| **F** flow | 01 (all but §9 internals) | `Battle.Flow.cs` (may split into `Battle.Flow.*.cs`), `BattleInput.cs`, `BattleEvents.Flow.cs`, tests `Combat/Flow*Tests.cs` | public entry points (`FIGT_A`, `FIGT_B`, direct `FIGT`), everything in 01 |

The three parts are merged (2026-10-08); there are no stubs left. Notes on the merged code:

- `Battle.Flavour.g.cs` is **generated** from the user's raw decompile by
  `python tools/codegen/gen_figt_m.py <_extract\scenario>` (`--check` verifies it is current);
  never edit it by hand. Re-run it after renaming a variable in the dictionary.
- Every `IK 0` goes through `IBattleInput.WaitForKeyAsync()` and stores the key in `RetVal`;
  the `*KeyWaitEvent` records only mark the point for the presentation.
- A jump out of the battle (game over to `OVER.ADV`) throws `ScenarioJumpException`.
- Flow entry points: `FightAsync` (direct `FIGT`), `AttackProvinceAsync` (`FIGT_A`),
  `DefendProvinceAsync` (`FIGT_B`), `DeathCaptureAsync` (`死亡捕獲`).

### Known gaps for the golden-test harness

- The message-window and map internals (`WAO_SUB.ADV`, `M.ADV`, `WAO_MAP.ADV`) are not ported:
  the flow and resolution parts emit events and set only `Ui_WaoOp` / `CallArg`, while the
  flavour part also tracks `Ui_WinType` / `Ui_WinFrameCg`. These UI variables match the
  original after each window closes but not while one is open, and other `Ui_*` writes of
  those files are missing. Decided (2026-10-08): the golden comparison ignores the `ui` group
  (except `Ui_CmdAttackAvail`) instead of porting them — reasons in
  [../golden-tests.md](../golden-tests.md), "What the comparison ignores".
- `EndToEndBattleTests` (14 tests) run whole battles through all three parts with hand-derived
  draw sequences; they check internal consistency, not equality with the original game.
  Equality is checked by `Golden/GoldenBattleTests` on captured battles
  ([../golden-tests.md](../golden-tests.md)).
