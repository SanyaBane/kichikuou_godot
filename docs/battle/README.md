# Battle specification

Read in this order: [01-flow.md](01-flow.md) — entry wrappers, setup, strategy, rounds, turn order, commands, enemy AI, exits, EXP, post-battle (exchange = black box, interface §9); [02-resolution.md](02-resolution.md) — one exchange: setters, specials, flavour text `FIGT_M`, `ComputeExchange`, `ApplyExchange` (interface §1, same procedure names); [03-rng-order.md](03-rng-order.md) — every random draw of a battle in order, mapping 01's R-ids to 02's §8 rows.

Open questions (merged from 01 §14 and 02 §11):

1. ~~Does `IK 0` overwrite `RetVal`?~~ **Resolved (xsystem35 `cmdi.c` `commandIK`):** yes — it
   sets `RetVal` to the pressed key code, or 0 while message skip is on. So the extra flavour
   lines for C83/C84/U3 depend on which key the player pressed; display only, no draws —
   `FIGT_M.ADV:5014-5077`, `8859-8888`; `WAO_SUB.ADV:292-294`, `510-519`.
2. ~~Operator precedence of the Asuka/Mill test?~~ **Resolved (bytecode, RPN in `FIGT_M.SCO` at
   0xa07d):** `*` binds before `+`, so the Mill-state test applies to slot 4 only — quirk BTX-04
   ([../quirks.md](../quirks.md)); `FIGT_M.ADV:8326`.
3. Content of unit record 51 before any kaiju is summoned (drives Goliath/ninja draws) — `FIGT.ADV:1126-1137`, `1972-1979`, `2592`, `2648`.
4. Can the original `system39.exe` `Y 4` return n+1 like xsystem35? — every `Y 4` site, e.g. `FIGT.ADV:2178`.
5. Background overlay test `Bt_EnemyParty1 ≠ 1137` is always true (probably meant `Cmdr_CgFull`) — `FIGT.ADV:1715`.
6. Can `Bt_MsgMode` be outside 1..4 at a Goliath strike (0 at new game), letting `FIGT_M.ADV:5`'s draw pick the side? — `FIGT.ADV:1144`, `FIGT_M.ADV:5-23`, `SET_VER.ADV:515`.
7. Who restores Rance's `Unit_Status` after a prologue defeat? — `FIGTﾐｶDE.ADV:1-21`, `死亡捕獲.ADV:26-39`.
8. ~~Can `Sﾌﾞﾀｲ` return a code other than 501/502/503 to FIGT_B?~~ **Resolved:** no — it returns only
   501, 502 (mode 2) or 503 (`Sﾌﾞﾀｲ.ADV:106-149`); the plains fall-through is unreachable.

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
- The strategic-map helpers (`M.ADV` and the `WAO_MAP.ADV` operations it reaches: ops 0, 1, 3,
  4, 10-13) are ported for their state effects in `ScriptContext.Map.cs` (camera, scratch
  `Tmp0021-0030`/`Tmp0039-0040`, marker list on page 10); `FlowMapAsync` raises the event first.
  Every `Y 3` pause goes through `IScriptInput.PauseAsync()`, which returns the key that cut it
  short; the engine stores it in `RetVal`. Both found by the first golden capture.
- Every `IK 0` goes through `IBattleInput.WaitForKeyAsync()` and stores the key in `RetVal`;
  the `*KeyWaitEvent` records only mark the point for the presentation.
- A jump out of the battle (game over to `OVER.ADV`) throws `ScenarioJumpException`.
- Flow entry points: `FightAsync` (direct `FIGT`), `AttackProvinceAsync` (`FIGT_A`),
  `DefendProvinceAsync` (`FIGT_B`), `DeathCaptureAsync` (`死亡捕獲`).

### Known gaps for the golden-test harness

- The message-window helper `WAO_SUB.ADV` is ported for its state effects in `ScriptContext.Wao.cs`
  (`WaoSubAsync`, called by the flow and resolution window helpers; the flavour part keeps its
  own equivalent open/wait/close): window type and frame, the portraits and centre picture,
  `Tmp0021/0022` left by every picture removal (FIGT_B), and the key waits of ops 1/11, which
  do nothing while no window is open. Its drawing is the event each caller raises. The `ui`
  writes of `FIGT_SEL` (button cursors, pointer) are not ported; the golden comparison ignores
  the `ui` group (except `Ui_CmdAttackAvail`) — reasons in
  [../golden-tests.md](../golden-tests.md), "What the comparison ignores".
- `EndToEndBattleTests` (14 tests) run whole battles through all three parts with hand-derived
  draw sequences; they check internal consistency, not equality with the original game.
  Equality is checked by `Golden/GoldenBattleTests` on captured battles
  ([../golden-tests.md](../golden-tests.md)).
