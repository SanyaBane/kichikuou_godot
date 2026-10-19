# The Harem and Treasure screens — our own (survey, 2026-10-19)

The owner asked (2026-10-19) for the next screens of our own interface: the harem picker `Sﾊｰﾚﾑ` and the item screen
`Sｱｲﾃﾑ` in the lacquer look of the Subordinates and deployment screens (subordinates.md, §5; deployment-table.md, "The
lacquer look"), mock-ups over dumps first. Like the other screens they come with Option → Interface → New
(`InterfaceChoice`); with Original nothing changes. The script decides everything, as with the Subordinates screen: our
screens only choose what the original's player would have chosen, through the inputs the script reads.

## 1. What the original does

**The harem picker `Sﾊｰﾚﾑ`** (`Strategy/HaremScreen`; survey-commands.md §3.8; `Cﾊｰﾚﾑ` is `Strategy/HaremCommand`):

- `Cﾊｰﾚﾑ` fills `HaremAvail_*` (in the prologue Sill, Soul and, once captured, Anasel; later Lia and Maris always, the
  others by their unit's presence and story flags, `HaremCommand.OfferGirls`) and calls the picker; one visit a week
  (`Ui_CmdHaremAvail`), given back on a cancel.
- The screen: the background CG 818 (694 in the prologue) — a bed in the picture panel at the top left, the info line
  under it —, a 3×16 grid of name cells where each girl sits at her own number (Sill 1 … Saitou 50; at week 18 Lia (2),
  Maris (3), Kanami (6) and Haurein (9), so most cells are empty), "End" on the bar.
- Hovering a girl shows her picture (CG 700 + n, 304×196: her sprite on the bed, her full name, her description in the
  CG; in the prologue 697-699) and the info line "Sex Count: n" and "Last Summoned: LP y, Month m, Week w" (dashes
  before the first visit; `Hist.Harem_SexCount`, `Hist.Harem_LastWeek`).
- A left click on a girl picks her at once (no confirmation): her scene plays (`Ev_Entry` 10). End, a right click or a
  click on the bar's other places ends the command (503; CMD-22, CMD-23).

**The item screen `Sｱｲﾃﾑ`** (`Strategy/ItemScreen`; survey-commands.md §3.9; `Cﾀｶﾗ` is `Strategy/TreasureCommand`,
which first drops the items of fallen units, CMD-18):

- Two views on one grid, one at a time (`Ui_ItemViewMode`, kept between visits): the characters (units at status 1 or
  2, white) or the items owned (in stock white, held coloured, without effect grey). "Item/Character" on the bar turns
  the view, "End" leaves; the bar's first place has no label but answers 997 (CMD-17).
- Hovering shows the picture panel: a unit's status CG (600 + n, the same as the Subordinates screen's) with its war
  cost, troops, class and item; an item's CG (750 + n, 304×196: the item, whether it is equippable and its effect, the
  description) with "Equippable · X is equipped / Nobody's equipping this", or "Unequippable".
- Equipping is two picks: a character, then an item (the view turns by itself, the first pick's picture shrinks to the
  bottom left with "X is equipping — Which item?"), or an item, then a character. The unit's own item goes back to stock.
- **Refusals** in a narration window (a key wait): a unit holding Chaos (1) or Nikkou (2) ("can't be unequipped"); those
  two items themselves; an item without effect ("This item can't be equipped"); the item the unit already holds.
- **Questions** (Yes/No at the pointer): an item held by another unit — "would you like to remove it and equip X
  instead?"; in the item view with no unit picked, a held item — "would you like to unequip it?" (504: back to stock).
- A right click drops a half-made pick (998), or leaves when there is none.
- At week 18 of the kept save the stock is two items, both held: the L. Holy Sword (item 6) on Rance (unit 1) and By
  Lord (item 7) on Rick (unit 7); the bot's new game at week 26 has the same two.

## 2. Mock-ups (2026-10-19)

`tools/ui/mock_pickers.py` (its header gives the dump commands and the call) reads the kept save `saves-week18` (the
units, the items, the harem's history) and draws over the original's screens: the harem picker dumped from that save
(`--harem 6`, headless) and the item screen dumped from an unattended new game at week 26 (headless). The images hold
the player's CGs and stay outside the repository. Where the save is too thin to show a layout (four girls, two items),
a variant marked "made up" adds a later game's offer or stock — invented for the layout, not from a save.

- **H0 / I0** — the original's screens, for comparison.

**The harem:**

- **H1 — the ledger** (the Subordinates screen's layout): the girls on offer as a paper ledger across the top (No.,
  Name, Visits, Last summoned — "Never" in vermilion —, Leads: her unit if she leads one), her picture below left as the
  original's panel, a Summon panel right: her name, visits, last summoned, the unit she leads, "Summon Kanami" in red
  lacquer, End. A click on a row chooses her; Summon plays her scene.
- **H2 — name tablets, her picture large**: the girls as lacquer tablets (a plum heart, her name in the brush face, her
  visits or "new", the last date short) three to a row on the right; the hovered one's picture (666×416) and figures on
  the left with "Summon Kanami". A click on a tablet summons her at once, as the original's pick.
- **H2b — H2 with a later game's offer (made up)**: 22 girls, to show how the tablets scale (33 before a scroll).
- **H3 — her picture as the stage**: the picture at nearly 3× over the top of the screen; the girls a strip of tablets
  below that scrolls when they outgrow it; Summon and End at its right.

**The treasure:**

- **I1 — two ledgers side by side**: the characters (No., Name, Type, Item) and the items owned (No., Name, Kind —
  Equip, Fixed, Can't equip —, Held by) at once, no view to turn; a character and an item chosen (both vermilion); below,
  the character's status CG and the item's CG side by side, and an Orders panel: the two names, what moves where
  ("from Rance", "Magic Barrier back to stock"), Equip in red lacquer, Unequip, End. Equip moves the item at once (the
  original's Yes/No is the click). Chaos and Nikkou, and the unequippable items, in grey ink.
- **I1b — I1 with a later game's stock (made up)**: twelve items, the ledger's scroll bar.
- **I2 — characters with item slots** (made-up stock): the units as lacquer cards (face, name, type, a slot with the
  item, a padlock on Chaos), the items as tablets right under the hovered item's picture; an item dragged onto a card
  (the slot glowing gold, "Equip here"), or clicked and then a card; a slot's item dragged out goes back to stock.
- **I3 — the original's two views as tabs** (made-up stock): one ledger at a time, "Characters | Items" in the title
  band; a character picked turns to Items with "Equipping Cordoba — which item?" and his face, as the original's prompt;
  "Equip Crystal Ring", Back (drops the pick), End.

## 3. Open for the owner

1. The harem: H1, H2 or H3 (or a mix)?
2. The harem: does a click on a girl summon her at once (the original, H2/H3) or choose her, with Summon to confirm (H1)?
3. The treasure: I1, I2 or I3?
4. The treasure: Equip at once, the original's Yes/No ("remove it and equip X instead?") taken as the click — as the
   Subordinates screen dropped Do/Don't?
5. The refusals (Chaos, Nikkou, the unequippable items, the item already held): greyed with their reason as a tooltip,
   never clicked, instead of the original's message window?

**The owner's choices (2026-10-19):** the harem is **H2** (the tablets, her picture large), and a click on a tablet
only chooses her — **Summon** summons (as H1's button, so question 2 is answered against the original's one click);
the treasure was I2 at first. The owner then changed it (the same day): the treasure **in Sengoku Rance's manner** —
every character in a list on the right; the chosen one's sprite shown, and beside it a window an item is dragged onto.
Questions 4 and 5 wait for that round.

**Round S (2026-10-19)** — `tools/ui/mock_pickers.py`, round `s`. The sprite is the unit's own battle sprite
(`Unit_CgAlone`, 320×256, palette index 40 transparent, the one its battle shows when it has no troops), cut to its
pixels and scaled up whole pixels (×3 at first, ×2 since the owner's note below); the faces as the deployment screen's (`UnitFaces`). S1 is the save's own state, the
others the made-up stock of I1b:

- **S1 — the stage** (Barres (unit 6) chosen, week 18): the characters right, a lacquer row each (face, name, type, the
  item held; the chosen row vermilion with a gold arrowhead); left a stage — his name and type in the brush face, a
  strip of figures (troops, cost, level, HP, Atk, Def, Act, Mag), his sprite large on a lit floor, and beside him the
  **Equipment** window ("No item — Drag an item here", dashed); under the stage the items owned as tablets, three to a
  row, with where each one is.
- **S2 — a character holding an item**: Cordoba (unit 8) with Magic Barrier — the item's picture (the original's item CG
  with its description) in the window, its name, × to take it off (or dragged out).
- **S3 — a drag**: Crystal Ring onto Cordoba — the window glows gold over the dragged item's picture, "Equip here", "Magic
  Barrier goes back to stock".
- **S4 — Rance (unit 1) with Chaos**: the window shows it with a padlock ("Never leaves him") and takes no drop; Chaos
  and the unequippable items greyed on the shelf.
- **S5 — three columns**: the items a column at the left, the stage in the middle with the Equipment window under the
  sprite, the characters right (here during S3's drag).

The owner (2026-10-19): the sprite smaller, the item's picture with its description larger. Redrawn: the sprite ×2,
the Equipment window 470 wide beside it (the item's picture 432×270, ×1.4 the original's), in S5 the picture 496×310
under the sprite.

**The harem, built (2026-10-19)** — H2 with Summon: the model `Original.Content/Strategy/HaremRoster` (the girls
on offer from `HaremAvail_*` with their visits and last week from the history records, their pictures as the picker
chooses them, the first choice — the girl chosen before, else the one summoned last, `Harem_LastPick`, else the first —,
the gestures: Summon a click on her cell, End the bar's End, the script's pointer at rest on the empty 51st cell),
`GamePresenter.HaremGirls` (named from the picker's string table); `HaremRosterTests` (the model on hand-made states,
and `Cﾊｰﾚﾑ` from the player's files: a summon of Maris (3) recorded, End giving the visit back);
`godot/Strategy/HaremView` (drawn by hand as H2, but its grid as the original's — the owner, 2026-10-19: each girl at
her own number's cell of the 3×17 grid, the cells of the girls not on offer empty, a tablet one line (heart, name, visits);
a tablet's click only chooses; a right press is End) and `GameScreen.Harem.cs`; `FacePictures.PicturePanel` takes the
harem's screen and empty picture (818/700, the prologue's 694/696). Looked at in a window from `saves-week18`
(`--harem 9 --until deployment --interface new`: Lia (2), Maris (3), Kanami (6), Haurein (9) on offer, Haurein summoned
by unattended play). The mock-up's "Leads" (the girl's unit) is left out: nothing in `Sﾊｰﾚﾑ` ties a girl to a unit.

## 4. How the screens would answer the script (a sketch, before the choice)

- **Harem:** while `Sﾊｰﾚﾑ` is the open picker (`PickerKind.Harem`) our screen covers it; a summon is a click on her cell
  (`GesturePoints.PickerCell(n)`), End the bar's End. The pointer stays ours over the screen, so the script hovers
  nothing; her picture and figures come from the state (`HaremAvail_*`, `Hist`) and the CGs, as the deployment screen's
  picture panel (`FacePictures.PicturePanel`). The prologue's pictures (697-699) and grown-up Mill's (695) as the script
  chooses them (`HaremScreen.ShowDetailsAsync`).
- **Treasure:** while `Sｱｲﾃﾑ` is open (`PickerKind.Items`): an Equip is a short plan of inputs, answered while it runs as
  the Subordinates screen's orders — the view turned to Characters if needed (the bar's Item/Character), the unit's
  cell, the item's cell, Yes when the script asks about a holder; an Unequip is the item view, the item's cell with no
  unit picked, Yes (504). The refusals are never reached when the screen greys what they refuse. End the bar's End.
- No rule moves into the UI: what can be equipped and what the script would refuse are read from the state the way
  `ItemScreen.PickAsync` decides them, into a model in `Original.Content/Strategy`, with tests.
