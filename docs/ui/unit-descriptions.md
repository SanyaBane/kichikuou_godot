# The units' descriptions as text

Owner, 2026-11-04: the description beside a unit's sprite in its status CG (600 + n; the deployment screen, Subordinates,
Treasure's hover and the others that show it) is hard to read in the patch's pixel font. Keep the CG, read its text and
draw it over the CG's panel in a font of ours. This part is the reading; the drawing comes after its mock-ups.

## What the CG holds

A status CG is one picture: the gold frame, the sprite on its floor, the name at the lower left and, at the right, a
panel whose text the English patch drew into the pixels. The text is nowhere else — not in the scenario, not in any
other file — so it can only be read from the picture, and since it is the player's (the patch's), it is read from the
player's CG at run time and never kept in the repository.

- **Sizes.** 601-612 are 304×186, the panel's inside rows 1-184; 613 on are 304×196, rows 6-189. The text's columns are
  207-301.
- **Which CGs.** 601-653 and 695-699 have English text (57); 600, 692 and 696 have an empty panel; 654 is still in
  Japanese (its own font, lines 12 rows apart), which the reading leaves alone. 693 and 694 are other screens.
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
   pixels (`UnitDescriptionGlyphs`, 139 pieces). One unknown piece makes the whole panel unread (`null`): the screen
   then keeps the CG's own panel.
4. Spaces: two empty columns or more part words — the patch kerned its spaces, so "A talented" may stand only a column
   wider than letters do — but not before `.` `,` `!` `?` nor after a hyphen. Two apostrophes side by side are `"`.
5. Lines join into their paragraph; a line ending in a hyphen is a word broken in two and joins the next without it.
6. The font's capital I is its small l: a word starting with l where no English word can (alone, before an apostrophe
   or a consonant but l and y) starts with I.

**What the pixels cannot tell**, set by position (CG, line, piece) in `UnitDescription`:

- The touching r and n are the font's m to the pixel: "turn" (634) and "returned" (636).
- A capital A as close to the next word as to a letter: "A talented" (603, 624).
- A line-end hyphen that is the word's own: "pseudo-magic" (620).

## How the table was made

Once, from the extract's `cg_png`: every piece of every English panel was collected (139 distinct), the single letters
named by eye from enlarged sheets, the touching ones split into named letters by matching their columns, the rest named
from their words in context; the 57 texts were then read through whole and the exceptions above found. The test
`UnitDescriptionTests` holds the SHA-256 of all 57 texts as checked, so a change of the reading cannot go unseen; the
archive's CGs are the PNGs pixel for pixel (`GameArchiveTests`), so the import reads the same.

## Next

The drawing: the CG's panel covered and the text set in our font, on every screen that shows a status CG — mock-ups
first.
