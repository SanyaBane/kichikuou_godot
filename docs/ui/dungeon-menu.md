# The dungeon's floor menu — rows that stay in place

The owner's request (2026-10-25): in a dungeon search the floor menu shows Descend and Retreat on floor 1, then Ascend,
Descend and Retreat from floor 2, so the rows move between floors. A player who clicks the same spot each time picks
Ascend on floor 2 where Descend stood on floor 1. This file records what the script offers and the first round of
mock-ups (`tools/ui/mock_dungeon_menu.py`).

## 1. What the original offers

`L_016b4` (`Cﾒｲｷｭｳ.ADV:858-893`, the port `DungeonCommand.FloorMenuAsync`) builds the menu from four conditional items
and Retreat, always in this order:

| Item | Cave (`DgnRun_FloorMode` 1) | Tower (`DgnRun_FloorMode` 2) | Offered when |
|---|---|---|---|
| Ascend | one floor back (`L_01c0a`) | one floor deeper (`L_01982`) | cave: floor ≠ 1; tower: floor < `DgnRun_MaxFloor` |
| Descend | one floor deeper (`L_01982`) | one floor back (`L_01c0a`) | cave: floor < `DgnRun_MaxFloor`; tower: floor ≠ 1 |
| Retreat | `L_0181c` | `L_0181c` | always |

So floor 1 shows Descend and Retreat, a middle floor all three, the last floor Ascend and Retreat (the Monsters' Cave
(dungeon 2): 14 floors). The order is Ascend, Descend, Retreat in both modes. The towers are Rising Dragon Mountain
(dungeon 11) and the Hyper Building (dungeon 26) (docs/strategy/survey-dungeon.md §2.1).

A misclick costs something. A floor back (`L_01c0a`) makes an encounter roll like a floor deeper does, so a wrong
Ascend can bring a battle. A wrong Retreat only opens Retreat's own question (`:934-938`).

In our interface the menu is a plain `]` menu (`godot/Strategy/SceneMenuView`) at the original box's place.

## 2. Mock-ups, round 1

Drawn over two of the owner's window screenshots (the Monsters' Cave, floors 1 and 2). The last floor and the tower
are drawn over them, with the place's window rewritten and the cave's picture standing in for the tower's.

**Variant 1: three rows, always.** The menu stays where it is and always has its three rows. A row the script does
not offer is dimmed and takes no click or key. A cancel still opens the menu again, as in the original.
- `1-plain-cave-1F`: the owner's idea as it is.
- `1a-cave-1F`, `1b-cave-2F`, `1c-cave-last`, `1d-tower-2F`: the same, plus each row's destination ("to 2F"), the
  reason a row is dimmed ("the entrance", "the deepest floor", "the top floor") and Retreat set apart by a divider.

**Variant 3: a ladder.** No menu over the picture. A lacquer panel right of the dungeon's picture holds Ascend (▲) and
Descend (▼) around the current floor, with Retreat under them as a red button. The way deeper has the gold arrow and the
caption "deeper · to nF"; the way back is quiet ("back · to nF"). A way not offered is dimmed.
- `3a-cave-1F`, `3b-cave-2F`, `3c-cave-last`, `3d-tower-2F`. In a tower the gold arrow points up.

Both variants answer the script with the item it offered, so no rule changes: the screen's rows are only mapped to
the menu's items. The hints show nothing the original hides. The original's missing row already shows the first and
last floors, and the next floor's number follows from the current one.

## 3. Mock-ups, round 2

The owner (2026-10-25) asked for variant 3 with a way not offered hidden rather than dimmed. Its place stays empty, so
the floor plate, the other button and Retreat do not move.
- `3e-hidden-cave-1F`: no Ascend.
- `3f-hidden-cave-last`: no Descend.
- `3g-hidden-tower-1F`: no Descend, the gold arrow up.

The middle floors are the same as round 1's `3b-cave-2F` and `3d-tower-2F`.

## 4. Built: the ladder with hidden ways

The owner chose round 2's ladder (2026-10-25). It is built in our interface:
- `Original.Content/Strategy/FloorLadder` recognises the floor menu by its items (`Cﾒｲｷｭｳ.ADV` 866/872 up,
  878/884 down, Retreat at 889 last). It returns the floor, each way's choice, its text from the menu, the floor it
  leads to and whether it goes deeper. `GamePresenter.FloorLadder` gives it while the menu is open.
- `godot/Strategy/FloorLadderView` draws the ladder right of the dungeon's picture, at fixed screen coordinates
  (948, 196, 248×340). `GameScreen.SelectAsync` opens it instead of our `]` menu, with `SceneMenuCovered` hiding
  the selection box.
- A click answers the script with the way's item. A right click answers 0, and the script opens the menu again, as
  in the original.
- Unattended play in a window dumps it once as `floor-ladder`, for example from the week-18 save with `--dungeon`
  (CLAUDE.md, "A dungeon battle"; `--until round-1` is enough).

Tests: `FloorLadderTests` (content tests).
