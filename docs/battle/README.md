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
