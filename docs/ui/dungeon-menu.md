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
  (CLAUDE.md, "A dungeon battle"; `--until round-1` is enough). In the touch mode it stands in the lower right corner
  (§5).

Tests: `FloorLadderTests` (content tests).

## 5. Touch: the ladder within the thumb's reach (owner, 2026-10-26)

The owner's request, from the screen gallery's touch pass (`floor-ladder`): the ladder's ways at the screen's lower
right corner, in the right thumb's reach, as the map's rails (docs/ui/touch.md, "The command panel within the thumbs'
reach"). Built, the ladder stands right of the dungeon's picture at the screen's middle height (y 196-536), the same on
PC and in the touch mode; its Ascend and Retreat lie at the reach's edge.

On the real screen (a dungeon look with `--touch`, the dump `floor-ladder`) the picture stands over black, not over the
map, and the place's window 113 (the dungeon, the floor, the unit, its troops) lies under it from x 248 to 1032, y 536 to
the foot. The gallery's entry shows the map and no window 113, so it is not what the game shows here. The corners right
of the window (x 1040-1276) and left of it (x 4-240) are free down to the foot.

The mock-ups are `tools/ui/mock_touch_floor.py` (its parts `mock_touch_panel.py`'s), over that dump: the Monsters' Cave
(dungeon 2) on 7F (both ways; window 113's floor rewritten) and on 1F (no Ascend, its place empty as round 2's); the
command is in its docstring, the images stay outside the repository. Round T1:

- **A — the ladder as built, moved into the corner**: the rail at x 1040-1276, y 424-796; Ascend, the floor, Descend,
  Retreat in the PC's order, the ways 212×78 (43 × 16 mm). Descend, the usual way in a cave, at the grip's height
  (y 594-672, the thumb's resting place); Ascend, a floor back with an encounter roll, the furthest; Retreat at the foot,
  where the map's Close and the province menu's Return stand.
- **B — Retreat at the rail's top, the ways at the thumb**: Retreat furthest from the resting thumb, as Turn END on the
  command panel's rail; Ascend over the floor, Descend 212×104 in the corner. In a tower the way deeper (Ascend) is the
  rail's middle.
- **C — the ways at the right thumb, Retreat at the left**: the ways 212×100 with the floor between them in the right
  corner (y 470-796), Retreat alone in the lower left corner, left of window 113: leaving takes the other hand, so a
  thumb stepping down floor after floor never lands on it.

In all three a way not offered leaves its place empty, the gold arrow marks the way deeper, and the script is answered
with the menu's item as now; the PC keeps today's place.

**Recommendation: A** — the same ladder as the PC's, only lower, the rails' language (the exit at the foot, as Close
and Return), and the cave's usual way exactly where the thumb rests. A wrong Retreat only opens its question
(`Cﾒｲｷｭｳ.ADV:934-938`), so it needs no distance; the costly mistake is Ascend, which A puts furthest. B gives Descend the
largest button but moves Retreat away from where the other rails have their exit; C is the safest against a stray
Retreat but splits the screen between the hands for one menu.

**The owner's choice (2026-10-26): B.** Built, in the touch mode only (the PC's ladder as before):
- `godot/Strategy/FloorLadderView` lays its rail at x 1040-1276, y 424 to the foot when `TouchMode.On`: Retreat 212×60
  at the top (Shojumaru 22), a divider, Ascend 212×78, the floor, Descend 212×104 in the corner (its arrow and name a
  size larger). A way not offered leaves its place empty, as on the PC; the taps and the right click (Back) answer the
  script as before.
- The screen gallery's `floor-ladder` entries now show the screen the game shows: cleared to black (`EC 0`), the
  picture, window 113 with the header (the gallery's own lines; `GalleryEntries.Map.cs`, `Ladder`).
- Looked at by the agent (2026-10-26) in the gallery's both passes and in the dungeon look with `--touch` (the week-18
  save, `--dungeon --until round-1`). Not yet on the tablet.

## 6. Touch: the menu before a search (owner, 2026-10-26)

The owner's request, after the gallery's `dungeon-entry`: the dungeon's own menu before a search within the thumb's
reach. `L_0076a` (`Cﾒｲｷｭｳ.ADV:238-305`, `DungeonCommand.MainMenuAsync`) opens it over the dungeon's picture, window 113
under it with the dungeon's name and state; its items are Start Search (`:267`), Bring Merim (`:272`) or Leave Merim
(`:278`), Bring Pink Cow (`:284`, the Uncurse Dungeon (dungeon 10) only), God's Door (`:290`, the Ancient Ruins
(dungeon 30) only) and Return (`:295`), last — two to four items. A cancel is Return. Today it is our plain `]` menu
(`SceneMenuView`) at the boot's selection box, at the screen's top. Its items tell it from every other menu, as the
ladder's do, so a view of its own changes this menu alone.

The mock-ups are `tools/ui/mock_touch_dungeon_entry.py` (its parts `mock_touch_panel.py`'s), over the gallery's touch
dump `dungeon-entry` with the menu taken away: the Monsters' Cave (dungeon 2) unsearched (two items) and the Ancient
Ruins (dungeon 30) in progress with Merim and God's Door (four; the cave's picture standing in); the command is in its
docstring, the images stay outside the repository. All three use the ladder's rail, right of window 113. Round E1:

- **A — the province menu's rail**: rows 212×46 from the foot up, Start Search lowest with a small gold arrow, Return
  212×60 at the foot, as the map's Close and the province menu's Return.
- **B — the floor ladder's rail**: Start Search 212×104 in the corner, exactly where Descend stands once the search
  begins, a gold arrow down; Return on top, as the ladder's Retreat; Merim, the cow and God's Door between them, the rail
  growing upwards.
- **C — Start Search at the right thumb, Return at the left**: Start Search in Descend's place, the other items above it;
  Return alone in the lower left corner.

**Recommendation: B** — the ladder's language the owner chose (the way out on top, the way down in the corner), and one
place for the thumb from entering to the deepest floor: Start Search, then Descend, at the same spot. A puts Return
where the thumb rests, the menu's least wanted item; C splits the hands for a menu that is mostly entered.
