# The already-read text's colour

The owner's request (2026-10-23): text the player has read before is drawn in another colour, chosen in the options, so
that a player who sees it knows they have read it already — after Rance X's option "Already-read text" (a row: "Don't
change" and nine colours).

## 1. What is decided

The owner, 2026-10-23:

- **Only another colour**, chosen from a row of swatches as in Rance X (no dimming, no mark on the window, no change in
  the message log).
- **Not done:** read text shown at once without typing; already-chosen items of a `]` menu marked.
- **System text keeps its colour** (the battle's lines, the menus, the week's report and the like).
- **Our interface only**: `--interface original` shows the original's windows as they are.

## 2. What is read

Two records of read lines exist:

| Record | Where | Read means |
|---|---|---|
| The save's read lines (decision 30) | `ScriptState.Extras`, `read:<file>` (`Engine/Script/ScriptState.ReadLines.cs`) | read in this save's line of play; a load of an older save forgets what came after it |
| The message skip's seen messages | the seen file of `Engine/Script/MessageSkip` (`EngineState`), every launch | seen in any game, any save, since the last "Forget them…" |

The colour follows **the message skip's record** (the agent's proposal, not contested by the owner): it is what "read
before" means to a player, as in Rance X, Kirikiri's 既読 and Ren'Py's seen text, and it is the record the skip stops at
with "Skip unseen text" off — the first line in the window's own colour is where a skip stops. Option → Messages →
"Forget them…" makes every line unread again for the colour too.

The record's unit is a message (`MessageEvent`, `ValueMessageEvent`), and a message is one line of the decompile: a
phrase of three lines on the screen is three messages. Inside one window they are read or not together almost always,
but not quite: a window may print its first lines and then choose the next ones by the state — `Gﾊﾞﾎﾞﾗ.ADV`'s "The
other day, General" and then the general's name, `Gﾏｹﾝ.ADV`'s "We are currently researching" and then what, by a
variable; `Gｱﾋﾞｧﾄｰﾙ`, `Gｶﾞﾝｼﾞｰ`, `Gｹｲﾌﾞﾘｽ`, `Gﾄｳｹﾞﾝ`, `Iｾﾞｽ` and the battle files have such windows too. Coloured
line by line, one phrase would show half read (the first round's R2 and R5). So **the colour's unit is the window**
(the agent's answer to the owner's question, 2026-10-23): its text takes the colour only when every message on it was read before; one new line keeps
the whole window in its own colour, which also tells the player the window holds something new. A message is noted as
seen before it is drawn (`GameScreen`'s `_skip.OnMessage`), so whether it was read **before** is taken before that note.

## 3. Where the colour shows

Our message windows (`godot/Strategy/MessageWindowView`, `Original.Content/Strategy/MessageWindow`) — 111, 112, 121,
122, 113, 123 and 131 — wherever they open: the scenes, the command files' lines, the battle's windows off the field.
A character the script prints in a colour of its own (not the window's) keeps it; a place's title keeps its gold.
A message with numbers (`ValueMessageEvent`: troops, gold, damages) never counts as read, since its numbers change, so a
window holding one keeps its own colour; a string variable printed on a window (a name) counts neither way.

Never: the battle's speech balloons and info boxes, the week report, the status strip, the `]` menus (`SceneMenuView`),
the message log, our screens and the port's own text (`PortTextEvent`) — they are drawn by their own views, which the
colour does not reach. Options' preview (121) counts as read and shows the choice.

## 4. Mock-ups (round R)

Round 1 coloured line by line (R2 two lines read and two new, R5 three and one), which the owner found odd: a phrase
is read or not as a whole. Round 2, below, colours whole windows, with Salmon, the owner's default.

`tools/ui/mock_read_text.py` over a dump of the options window and the week-26 save's opening scenes (the commands in
its header; the images stay outside the repository):

- **R1** — Option → Messages, a section "Already-read text" under the text speed: "Don't change" and nine colours on
  lacquer tiles (the colour as it shows on the windows' lacquer), the chosen one framed in vermilion as the palette's
  swatches are; its name and a note under the row. The preview in the chosen colour. **R1b** — "Don't change" chosen.
- **R2**, **R3** — speeches (121, 111) read before.
- **R4** — a thought (112) read: the colour dimmed as a thought's text is against a speech's (`Lacquer.Quiet`).
- **R5** — narration (113) read.
- **R6** — the nine colours on one page, "Don't change" first.

The colours, after Rance X's row: Salmon (240, 120, 120), Rose (240, 128, 184), Magenta (255, 128, 255), Violet
(192, 128, 255), Periwinkle (128, 128, 255), Sky (128, 184, 255), Cyan (128, 255, 255), Mint (128, 255, 192), Green
(128, 255, 128). The default is the first, Salmon (the owner, 2026-10-23).

## 5. The build

The owner chose round 2 (2026-10-23: whole windows, Salmon by default). Built:

1. `Engine/Script/MessageSkip.HasSeen` tells whether a message was seen; `GameScreen` gives it to the presenters as
   `GamePresenter.ReadBefore` (the battle's `BattlePresenter.ReadBefore` the same), asked as a message is printed, before
   `OnMessage` notes it. `Content/Screen/TextBox` keeps each character's flag (`Print`/`Put`'s `read`, gone with the
   character in `Clear` and `Wipe`) and answers `ReadBefore` for the whole box; `MessageWindow.Read` carries it.
2. `godot/Strategy/MessageWindowView` draws a read window's own-coloured text in the chosen colour (a thought's dimmed,
   `ReadTextChoice.Of`); the choice is `godot/Strategy/ReadTextChoice`, `[display] read_text_colour` in
   `user://settings.cfg` (-1 "Don't change", else the colour's index; Salmon, 0, by default).
3. Option → Messages: the section of R1 (`OptionsView.ReadTile`), Defaults back to Salmon; the preview counts as read
   (`MessageWindow.Sample(…, read: true)`).
4. Tests: `TextBoxTests` (Content), `MessageWindowTests` (the presenter's and the battle's windows, the sample),
   `MessageSkipTests.A_message_is_seen_once_noted_and_until_forgotten`.

The seen record is kept per save folder (`EngineState.Skip`), as the message skip's always was: the player's own folder
holds one, the agent's look folders (`user://saves-look-*`) their own.
