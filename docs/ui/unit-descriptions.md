# The descriptions as text

Owner, 2026-11-04: the description beside a unit's sprite in its status CG (600 + n; the deployment screen, Subordinates,
Treasure's hover and the others that show it) is hard to read in the patch's pixel font. The harem's pictures (700 + n)
and the items' (750 + n, Treasure's items) have the same panel and take the same treatment. Keep the CG, read its text and
draw it over the CG's panel in a font of ours. This part is the reading; the drawing comes after its mock-ups.

## What the CG holds

A status CG is one picture: the gold frame, the sprite on its floor, the name at the lower left and, at the right, a
panel whose text the English patch drew into the pixels. The text is nowhere else — not in the scenario, not in any
other file — so it can only be read from the picture, and since it is the player's (the patch's), it is read from the
player's CG at run time and never kept in the repository.

- **Sizes.** 601-612 are 304×186, the panel's inside rows 1-184; 613 on are 304×196, rows 6-189. The text's columns are
  207-301.
- **Which CGs.** The units' 601-653 and 695-699 (grey panel), the harem's 701-750 (pink) and the items' 751-800 (blue)
  have English text, 157 in all; 600, 692, 696 and 700 have an empty panel; 654 is still in Japanese (its own font,
  lines 12 rows apart), which the reading leaves alone. 693 and 694 are other screens.
- **The font.** Pure white `(255,255,255)` letters with a black outline over the panel's two greys, no anti-aliasing:
  a letter's pixels are always the same, so the reading is exact, not a guess. Proportional, 9 rows a line (two above
  the small letters, five for them, two for descenders), the letters one empty column apart, many touching with none
  ("rn", "tt", "rri").

## How it is read (`Original.Content/Strategy/UnitDescription`)

1. The rows with white in them, in runs; runs two rows apart or less are one (a line of small letters only leaves
   rows empty within it or between two lines).
2. Each run is fitted with lines 9 rows apart, starting up to two rows above it and ending up to two below, the fit
   whose small letters' rows hold the most white chosen. A run starting where the one before ended continues its
   paragraph; a gap (a blank line, sometimes half of one) starts a new paragraph.
3. Each line is cut at its empty columns into pieces, and each piece is looked up whole by the FNV-1a 64 hash of its
   pixels (`UnitDescriptionGlyphs`, 175 pieces). One unknown piece makes the whole panel unread (`null`): the screen
   then keeps the CG's own panel.
4. Spaces: two empty columns or more part words, but not before `.` `,` `!` `?` `%`, after a hyphen or between digits
   ("+10", "100"). The patch kerned the pair A-t: a capital A alone one column from a t is a word of its own ("A
   talented", "A tea lady"; no word of the panels starts with "At" but on a piece of its own, "Attack"). Two
   apostrophes side by side are `"`.
5. Lines join into their paragraph; a line ending in a hyphen is a word broken in two and joins the next without it.
   An item's "Equippable" is a paragraph of its own (its figures follow it, often with no blank line between).
6. The font's capital I is its small l: a word starting with l where no English word can (alone, before an apostrophe
   or a consonant but l and y) starts with I.

**What the pixels cannot tell**, set by position (CG, line, piece) in `UnitDescription`:

- The touching r and n are the font's m to the pixel: "turn" (634, 739), "returned" (636), "taciturn" (704),
  "modern" (790), "horn" (799).
- A line-end hyphen that is the word's own: "pseudo-magic" (620).

## How the table was made

Once, from the extract's `cg_png`: every piece of every English panel was collected (139 distinct), the single letters
named by eye from enlarged sheets, the touching ones split into named letters by matching their columns, the rest named
from their words in context; the 57 texts were then read through whole and the exceptions above found. The harem's
and the items' panels followed the same way (36 more pieces, 100 more texts read through). The test
`UnitDescriptionTests` holds the SHA-256 of all 157 texts as checked, so a change of the reading cannot go unseen; the
archive's CGs are the PNGs pixel for pixel (`GameArchiveTests`), so the import reads the same.

## Mock-ups, round 1 (2026-11-04)

`tools/ui/mock_unit_descriptions.py` over the gallery's `deployment` and `treasure-hover` (pc) and `treasure-hover`
(phone-portrait); the texts from `UnitDescriptionTests` with `KICHIKUOU_DESCRIPTIONS_OUT` set. The panel's inside
(columns 207-301) is covered and the text set again in **Oswald** (bundled, condensed, proportional): BIZ UDGothic was
tried first and dropped — its Latin is set wide (`that' s`), so the narrow panel held few words and the long texts
fell to 12 px. One size per screen, the largest the longest text of all (CG 622, 384 letters) fits in — 15 px on the
PC's deployment screen, 18 px on Treasure — so every unit on a screen reads alike; the special attack's quoted name
takes the accent colour where the variant has one.

- **A** — the panel as it is: its grey pattern (CG 600's empty panel), the text white with a dark outline.
- **B** — our lacquer, a dim gold line inside the panel's own border, the text cream, the attack's name gold.
- **C** — our paper (the options window's), the text in ink, the attack's name vermilion.

The owner chose **B** (2026-11-04).

## The drawing (built, 2026-11-04)

`godot/Strategy/PanelDescriptions`: `FacePictures.PicturePanel` registers the CG each picture-panel texture shows, and the
screens draw the texture through `PanelDescriptions.Draw` — the deployment screen, Subordinates, Treasure (a unit or an
item), Harem and the phone's long-press picture (`UnitPictureView`), on every device. Over the panel's inside (column
207 to the CG's right edge, the panel's rows) it lays our lacquer (`Ui.Panel`, opaque) with a dim gold line two CG pixels
in, and sets the text in Oswald 400, cream (`Ui.Text`), a quoted name gold (`Ui.Name`), word-wrapped, a paragraph's gap
half a line. The size is one per panel size: the largest from ten CG pixels down to five that the longest text of all
(CG 622) fits in, so every picture on a screen reads alike. A panel the reading does not know stays the original's.
Nothing changes under `--interface original` (the script draws its own screens). Looked at and accepted by the owner in a window (2026-11-04).
