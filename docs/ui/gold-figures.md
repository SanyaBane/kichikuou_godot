# The scenario's gold figures

2026-10-24. The owner's request: our message windows add a thousands separator to the sums of gold the scenario
shows ("90,000K GOLD" for "90000K GOLD"), after the standard of our own screens (docs/ui/options.md, "Thousands") —
but **only at the places verified to be money**, selected by file:line. This document is the survey: every amount in
K / KG / K GOLD the decompiled scenario shows the player, each classified by its context. Nothing is built yet.

Line numbers are the decompile's (`_extract\scenario`, the same in `scenario_named`). Records are named as everywhere
in the repository ("Portugal (28)", "Louis (unit 49)", "the Fog Card (item 28)").

## 1. Method

- **Literal figures.** Every text line (`'…'`, including the items of `$label$ … $` menus) was searched, after folding
  full-width digits, letters and blanks to ASCII, for a digit run (with `,` or `.` inside) followed by an optional blank
  and `K` not followed by a lower-case letter — this catches `K`, `KG`, `K GOLD`, `K G`, `KGOLD`, `00K…`. A second search
  took `K` / `Ｋ` standing alone (not after a digit) and lower-case `k` after a digit; a third, digit groups with commas,
  "thousand", "million" and `GOLD` / `G` after a number.
- **Printed figures.** The scenario prints numbers with `H width, expr:` only: there are 337 `H` and no `HH`; `MH`
  (a number into a string variable) occurs six times, all in `Gﾜｵ`'s clock (`:564-569`), and no string variable is
  printed before a `K`. For each `H` the next text line (skipping `ZH 1:`, which the scenario sets before every text
  line) was taken as its suffix, the expression read in `scenario_named` / docs/variables.tsv, and the surrounding lines
  read to decide what the sum is. Every line that begins with `00K` follows an `H` (34 of 34).
- **The window.** For message text, the window is the `WAO_SUB` operation (`Ui_WaoOp`) of the last window call before
  the line: 113/123 the cream box (narration, status windows), 111 the left portrait's speech (Rance), 121 the right
  portrait's (Maris, a merchant, the other party). docs/ui/messages.md, §1, describes them.
- **Checked against the rules.** Where the scenario changes `Gold` next to the figure (`Gold` is the treasury in units
  of 100K GOLD), the change was compared with the figure; all of them agree (the "Check" column).

Scratch scripts and their raw output are outside the repository; the search is repeatable with the patterns above.

## 2. Summary

| Kind | Money | Not money | Unclear |
|---|---|---|---|
| Literal (digits + K in the text) | 39 (7 of them menu items) | 0 | 1 (a placeholder without a figure, §3.3) |
| Printed (`H` + a `00K…` suffix) | 34 | 0 | 0 |
| Look-alikes (a `K` that is no amount; a printed figure with a digit appended) | — | 9 | — |

Every digit run followed by K in the scenario is money; the only K that are not are letters (§4). The 74 money
places are in 14 files:

| File | What | Literal | Printed |
|---|---|---|---|
| `COMMAND.ADV` | the command menu's status window | — | 2 |
| `Cｼｮｳﾆﾝ.ADV` | the merchant: mercenaries, items | 15 | 4 |
| `Cｼﾊｲﾄｼ.ADV` | own city: Temporary Levy, reinforcing the defences | — | 5 |
| `Cﾃｲﾄ.ADV` | construction | 6 | 6 |
| `Cﾃｷﾄｼ.ADV` | the independent cities' demands | 2 | 2 |
| `Cﾌﾞｶ.ADV` | Add New Troops, Replenish | — | 4 |
| `Gｶﾊﾟｰﾗ.ADV` | Kapala's advertising | 6 | 1 |
| `Gﾄｳｹﾞﾝ.ADV` | the Peach Blossom Spring's entrance fee | 3 | 1 |
| `Gﾌﾚｲｱ.ADV` | Freya's emergency food expenses | — | 1 |
| `Gﾚﾍﾞｯｶ.ADV` | Rebecca's services (Pluepet) | 2 | 1 |
| `Iﾄｳｿﾞｸ.ADV` | the thieves' money (prologue) | 2 | 2 |
| `MAIN.ADV` | the week's revenue and special expenses | 3 | 1 |
| `Sｱｲﾃﾑ.ADV` | the Treasure picker's unit line | — | 1 |
| `Sﾌﾞﾀｲ.ADV` | the deployment picker's war expenses | — | 3 (+1 placeholder) |

Three literal sums are below 1000K (`Cｼｮｳﾆﾝ.ADV:1303`, `Iﾄｳｿﾞｸ.ADV:688`, `:702`): money, but a separator changes
nothing in them.

## 3. Money

### 3.1 Literal figures

| File:line | Shown | Where | Context | Check |
|---|---|---|---|---|
| `Cｼｮｳﾆﾝ.ADV:252` | `200 Troops:　　4000KG` | 113 | the merchant's mercenary offer, Louis (unit 49): price for 200 troops | `Gold - 40` (`:284`) |
| `Cｼｮｳﾆﾝ.ADV:254` | `1000 Troops:　8000KG` | 113 | same, 1000 troops | `Gold - 80` (`:296`) |
| `Cｼｮｳﾆﾝ.ADV:267` | `4000K Contract` | menu item | the choice of that offer | as `:252` |
| `Cｼｮｳﾆﾝ.ADV:271` | `8000K Contract` | menu item | same | as `:254` |
| `Cｼｮｳﾆﾝ.ADV:589` | `1000 Troops:　　3000KG` | 113 | mercenary offer, Cecil (unit 50) | `Gold - 30` (`:621`) |
| `Cｼｮｳﾆﾝ.ADV:591` | `5000 Troops:　11000KG` | 113 | same | `Gold - 110` (`:633`) |
| `Cｼｮｳﾆﾝ.ADV:604` | `3000K Contract` | menu item | the choice | as `:589` |
| `Cｼｮｳﾆﾝ.ADV:608` | `11000K Contract` | menu item | the choice | as `:591` |
| `Cｼｮｳﾆﾝ.ADV:1303` | `Price:　100K GOLD` | 123 | item card: the Clothes (item 19) | `Tmp0003 : 1` |
| `Cｼｮｳﾆﾝ.ADV:1315` | `Price: 50000K GOLD` | 123 | the Hirami Lemon (item 43) | `Tmp0003 : 500` |
| `Cｼｮｳﾆﾝ.ADV:1344` | `Price: 50000K GOLD` | 123 | the Popularity Staff (item 3) | `Tmp0003 : 500` |
| `Cｼｮｳﾆﾝ.ADV:1358` | `Price: 100000K GOLD` | 123 | the Substitute Ninja (item 8) | `Tmp0003 : 1000` |
| `Cｼｮｳﾆﾝ.ADV:1372` | `Price: 80000K GOLD` | 123 | Napoleon's Writings (item 4) | `Tmp0003 : 800` |
| `Cｼｮｳﾆﾝ.ADV:1386` | `Price: 80000K GOLD` | 123 | Shingen's Writings (item 5) | `Tmp0003 : 800` |
| `Cｼｮｳﾆﾝ.ADV:1399` | `Price: 3000K GOLD` | 123 | the Ninja Birdlime (item 44) | `Tmp0003 : 30` |
| `Cﾃｲﾄ.ADV:343` | `Cost: 60000K GOLD` | 113 | building card: Girls' Military Academy | `Tmp0001 : 600` (`:365`) |
| `Cﾃｲﾄ.ADV:430` | `Cost: 40000K GOLD` | 113 | Library | `400` (`:452`) |
| `Cﾃｲﾄ.ADV:532` | `Cost: 40000K GOLD` | 113 | Hospital | `400` (`:554`) |
| `Cﾃｲﾄ.ADV:713` | `Cost: 80000K GOLD` | 113 | S&M Tower | `800` (`:735`) |
| `Cﾃｲﾄ.ADV:818` | `Cost: 98000K GOLD` | 113 | Maria Research Institute | `980` (`:840`) |
| `Cﾃｲﾄ.ADV:908` | `Cost: 90000K GOLD` | 113 | Magic Research Building | `900` (`:934`) |
| `Cﾃｷﾄｼ.ADV:2306` | `Demand:　9000K GOLD` | 113 | Portugal (28)'s demand before it joins | `Gold - 90` (`:2382`) |
| `Cﾃｷﾄｼ.ADV:3491` | `Demand:　3000K GOLD` | 113 | Hanna (26)'s demand | `Gold - 30` (`:3523`) |
| `Gｶﾊﾟｰﾗ.ADV:2039` | `10000K` | menu item | how much to spend on Kapala's advertising (offered while `Gold > 100`) | `Gold - 100` (`:2163`) |
| `Gｶﾊﾟｰﾗ.ADV:2045` | `5000K` | menu item | same (`Gold > 50`) | `Gold - 50` (`:2140`) |
| `Gｶﾊﾟｰﾗ.ADV:2051` | `1000K` | menu item | same (`Gold > 10`) | `Gold - 10` (`:2117`) |
| `Gｶﾊﾟｰﾗ.ADV:2123` | `No more than 1000K GOLD.` | 111 | Rance's answer | as `:2051` |
| `Gｶﾊﾟｰﾗ.ADV:2146` | `No more than 5000K GOLD.` | 111 | Rance's answer | as `:2045` |
| `Gｶﾊﾟｰﾗ.ADV:2169` | `A serious 10000K.` | 111 | Rance's answer | as `:2039` |
| `Gﾄｳｹﾞﾝ.ADV:111` | `10000K GOLD.` | 121 | the Peach Blossom Spring's entrance fee, told | `Gold - 100` (`:207`) |
| `Gﾄｳｹﾞﾝ.ADV:139` | `The entrance fee is 10000K` (`GOLD. Will you pay?` on `:141`) | 121 | the question before paying | same |
| `Gﾄｳｹﾞﾝ.ADV:224` | `A subordinate brought 10000K` (`GOLD at Maris's order.` on `:226`) | 113 | narration of the payment | same |
| `Gﾚﾍﾞｯｶ.ADV:34` | `2000K GOLD, please.` | 121 | Pluepet asks for Rebecca's services in advance | `Gold - 20` (`:131`) |
| `Gﾚﾍﾞｯｶ.ADV:46` | `Cost:　2000K GOLD` | 123 | the cost over the Pay / Don't menu | same |
| `Iﾄｳｿﾞｸ.ADV:688` | `We stole about 100K GOLD,` | 121 | a thief reports the take (prologue) | `Gold + 1` (`:705`) |
| `Iﾄｳｿﾞｸ.ADV:702` | `（+100K）` | 113 | the gain under the money line | same |
| `MAIN.ADV:194` | `M Land: -2000K GOLD` | 113 | the week's special expenses: M Land (25)'s funding | `Gold - 20` (`:191`) |
| `MAIN.ADV:201` | `AL Church: -6500K GOLD` | 113 | the AL Church's donation | `Gold - 65` (`:198`) |
| `MAIN.ADV:208` | `Galtia’s Food: -4500K GOLD` | 113 | Galtia (unit 41)'s food | `Gold - 45` (`:205`) |

### 3.2 Printed figures

The figure is printed by `H` and continued by a literal `00K…` on the next text line (the variable counts 100K GOLD),
so the screen's sum is the two together: `H 0, Gold` with Gold 777 and `'00KG）'` show `77700KG）`. Every row is money.

| `H` line / suffix line | Shown (n = the figure) | Expr (`H` width) | Where | Context | Range |
|---|---|---|---|---|---|
| `COMMAND.ADV:446` / `:448` | `Budget： n00K GOLD` | `Gold` (0) | 113 | status window of the command menu, Leazas | 0-65535 |
| `COMMAND.ADV:475` / `:477` | `Money: n00K GOLD` | `Gold` (0) | 113 | same, Rance's Thieves | 0-65535 |
| `Cｼｮｳﾆﾝ.ADV:257` / `:259` | `（Budget: n00KG）` | `Gold` (0) | 113 | under the Louis offer | 0-65535 |
| `Cｼｮｳﾆﾝ.ADV:594` / `:596` | `（Budget: n00KG）` | `Gold` (0) | 113 | under the Cecil offer | 0-65535 |
| `Cｼｮｳﾆﾝ.ADV:1077` / `:1079` | `National Budget` / `n00K GOLD` | `Gold` (0) | 113 | over the merchant's item menu | 0-65535 |
| `Cｼｮｳﾆﾝ.ADV:1328` / `:1330` | `Price: n00K GOLD` | `FogCard_Price` (0) | 123 | the Fog Card (item 28)'s price | 120 at first (`Cﾒｲｷｭｳ.ADV:1877`), +10% each use (`FIGT.ADV:1238`) |
| `Cｼﾊｲﾄｼ.ADV:890` / `:892` | `of n00K GOLD was collected.` | `RetVal` (0) | 113 | Temporary Levy's take | 1 to `Prov_Economy`×10 |
| `Cｼﾊｲﾄｼ.ADV:1532` / `:1534` | `n00K GOLD in total.` | `Tmp0001` (0) | 121 | Maris: the cost of reinforcing a city's defences | `Prov_Defense`×20, at least 5 |
| `Cｼﾊｲﾄｼ.ADV:1548` / `:1550` | `n00K GOLD!?` | `Tmp0001` (0) | 111 | Rance repeats it | same |
| `Cｼﾊｲﾄｼ.ADV:1566` / `:1568` | `Cost: n00K GOLD` | `Tmp0001` (0) | 113 | over the Reinforce / Don't menu | same |
| `Cｼﾊｲﾄｼ.ADV:1574` / `:1576` | `National Budget:` / ` n00K GOLD` | `Gold` (0) | 113 | same window | 0-65535 |
| `Cﾃｲﾄ.ADV:346` / `:348`, `:433` / `:435`, `:535` / `:537`, `:716` / `:718`, `:821` / `:823`, `:911` / `:913` | `（Budget: n00KG）` | `Gold` (0) | 113 | under each building's cost (§3.1) | 0-65535 |
| `Cﾃｷﾄｼ.ADV:2310` / `:2312` | `National Budget: n00K GOLD` | `Gold` (0) | 113 | under Portugal (28)'s demand | 0-65535 |
| `Cﾃｷﾄｼ.ADV:3495` / `:3497` | `National Budget: n00K GOLD` | `Gold` (0) | 113 | under Hanna (26)'s demand | 0-65535 |
| `Cﾌﾞｶ.ADV:876` / `:878` | `will cost n00K GOLD.` | `Tmp0001` (0) | 121 | Add New Troops' offer, Maris | 0-320 by the offer, + `Week`/4 (`:757-798`), ×3 per Add More (`:865`) |
| `Cﾌﾞｶ.ADV:884` / `:886` | `Costn00K GOLD` | `Tmp0001` (**5**) | 123 | the offer's figures window | same |
| `Cﾌﾞｶ.ADV:895` / `:897` | `National Budget` / `n00K GOLD` | `Gold` (**5**) | 123 | same window | 0-65535 |
| `Cﾌﾞｶ.ADV:1022` / `:1024` | `Cost: n00K GOLD` | `Tmp0226` (**3**) | 121 | Replenish's question | deficiency / 50, at least 1 |
| `Gｶﾊﾟｰﾗ.ADV:2027` / `:2029` | `National Budget` / ` n00K GOLD` | `Gold` (0) | 123 | over the advertising menu | 0-65535 |
| `Gﾄｳｹﾞﾝ.ADV:148` / `:150` | `National Budget` / `n00K GOLD` | `Gold` (0) | 113 | over the Pay / Don't menu | 0-65535 |
| `Gﾌﾚｲｱ.ADV:3073` / `:3075` | `n00K GOLD was used as` | `Tmp0001` (0) | 113 | the whole treasury but 200K spent on emergency food (`Tmp0001 : Gold - 2`, `Gold : 2`) | 0-65533 |
| `Gﾚﾍﾞｯｶ.ADV:52` / `:54` | `National Budget` / ` n00K GOLD` | `Gold` (0) | 123 | over the Pay / Don't menu | 0-65535 |
| `Iﾄｳｿﾞｸ.ADV:698` / `:700` | `Money: n00K GOLD` | `Gold` (0) | 113 | the thieves' money before the take | 0-65535 |
| `Iﾄｳｿﾞｸ.ADV:711` / `:713` | `Money: n00K GOLD` | `Gold` (0) | 113 | and after it | 0-65535 |
| `MAIN.ADV:177` / `:179` | `Revenue: n00K GOLD` | `Tmp0022` (0) | 113 | the week's revenue (the sum of the Leazas provinces' `Prov_Economy`) | a few hundred |
| `Sｱｲﾃﾑ.ADV:541` / `:543` (+ `' GOLD'` `:546`) | `n00K GOLD, t Troops` | `Unit_DeployCost` (**5**) | picker's info line | the Treasure picker's hovered unit | small |
| `Sﾌﾞﾀｲ.ADV:336` / `:338` (+ `:341`) | `War Expenses:n00K GOLD` | `Tmp0016` (**5**) | picker's info line | the deployment picker's total | sum of the line below |
| `Sﾌﾞﾀｲ.ADV:353` / `:355` (+ `:358`) | `（n00K GOLD, t）` | `Unit_DeployCost + Unit_Troops / 300` (**3**) | picker's info line | a party member's war expense | small |
| `Sﾌﾞﾀｲ.ADV:531` / `:533` (+ `:536`) | `n00K GOLD, t/m` | same (**5**) | picker's info line | the hovered unit's war expense | small |

### 3.3 Placeholder

| File:line | Shown | Context | Class |
|---|---|---|---|
| `Sﾌﾞﾀｲ.ADV:370` (+ `' GOLD'` `:373`) | `－－－－－－－－（－－－－0K GOLD` | an empty party row of the deployment picker | money, but no figure to group: leave it |

## 4. Not money (look-alikes)

| File:line | Shown | Why not |
|---|---|---|
| `Iｱﾘｽ.ADV:676` | `K` | a password hint in Alice's list ("CG", "K", "YOK"): a letter |
| `Gｱﾘｽ.ADV:1215` | `Ｋ` (full width) | a letter of the password entry's menu (`$…$`) |
| `Gｱﾘｽ.ADV:9014` | `Then K iz "Megumi", YOK iz` | the passwords explained |
| `Gﾏｼﾞｯｸ.ADV:203` | `（Pseudonym - K）` | a pen name |
| `FIGT表示.ADV:1296` | `K-... K-K-K-K-...` | a stutter |
| `Gｲｿﾛｸ.ADV:4305` | `K-King Rance...!` | a stutter |
| `ﾓｼﾞｷｬｯｼｭ.ADV:25` | `ＡＢＣＤＥＦＧＨＩＪＫＬＭＮ` | the font cache's warm-up string, never a message |
| `Cﾒｲｷｭｳ.ADV:2180` and on | `KD …` | a character's name (KD) |
| `Gｶｾｲ.ADV:1893` / `:1895` | `n` + `0."` | the same trick as `00K` — a printed figure with a digit appended — but it is a score (`N_Mars_Score`), not money |

## 5. Side list: the boundary

**Gold without K.** The translation writes some story sums in whole GOLD, with or without commas; these are prose,
not the engine's K figures, and are listed only to mark the boundary: `Cｼｮｳﾆﾝ.ADV:224` ("3,000,000 GOLD."),
`:227`/`:229` ("10,000" / "GOLD"), `:559`/`:561`, `:564`; `Cﾃｷﾄｼ.ADV:1413` (Rockearth (17)'s bribe, "50,000,000
GOLD", `Gold + 500`), `:2297`, `:2341` ("50000 GOLD"), `:2630` (Pluepet's 10,000,000, `Gold + 100`), `:3193` (M Land
(25)), `:3482`; `GDX.ADV:770`/`:772` (`Gold + 100`); `Gｴﾚﾅ.ADV:2250`, `:2668`, `:2867`; `Gｶﾌｪ.ADV:2583` (`Gold - 10`);
`Gｶﾞﾝｼﾞｰ.ADV:1539`; `Gｻｲｾﾞﾙ.ADV:440`; `Gｼｨﾙ.ADV:7154`, `:7225` (`Gold + 10`), `:7370`; `Gｾﾙ.ADV:216`, `:298`, `:568`,
`:665`, `:1092`, `:1114`/`:1116`; `Gｿﾙﾆｱ.ADV:1615`; `Gﾁｽﾞｺ.ADV:5374`; `Gﾌﾚｲｱ.ADV:2303`, `:2308`; `Gﾒﾅﾄﾞ.ADV:837`;
`Gﾚｲ.ADV:801`, `:810`, `:821`, `:840`; `Iﾄｳｿﾞｸ.ADV:570`/`:572`; `TEST.ADV:13`, `:326` (the debug file). No figure is
written with a bare `G`.

**Gold in the All Data graphs.** `各種DATA.ADV:71`, `:146`, `:195` title the funds, sundry and military graphs "In
Units of 10,000,000 GOLD"; their scale (`Ui_MoneyGraphScale`, `:72`…) and values (`Hist_Funds / 100` and the like,
`:117`, `:166`, `:215`) are money in that unit, printed without K. Our presenter draws All Data by hand.

**Province Economy.** `Cｼﾊｲﾄｼ.ADV:24`, `Cﾃｷﾄｼ.ADV:35`, `Cﾃｷﾄｼ2.ADV:33`, `Cﾆﾝｼﾞｬ.ADV:422` print `Prov_Economy`
("Economy: n") — the province's weekly revenue in 100K, but shown as a bare rating.

**Printed figures that are not money** (303 of the 337 `H`, grouped roughly by their variable and words): dates and
weeks 86 (`MAIN`, `ﾛｰﾄﾞｾｰﾌﾞ`, `Iｼｬﾝｸﾞﾘﾗ`, `Gｱﾋﾞｧﾄｰﾙ`…), stats 76 (HP, attack, defence, levels, modifiers: `Cﾌﾞｶ`,
`FIGT`, `G1`…), troops 61 (`COMMAND.ADV:449` and `:478` among them, the status window's troops beside the budget;
`Cﾌﾞｶ`, `FIGT`, `Sﾌﾞﾀｲ`, `Gﾀﾐ`…), other counts 46 (floors, girls, visits, rounds, scores), debug and system 26
(`TEST`, `PCMPLAY`, `WAO_SUB`, `WAO_MAP`, `ｽﾀMENU`, CG numbers in `Gﾜｵ`), the All Data graphs 8. Lines that mix both
kinds: `Cｼｮｳﾆﾝ.ADV:252`, `:254`, `:589`, `:591` ("1000 Troops: 8000KG") and the pickers' `n00K GOLD, t Troops`.

## 6. Open questions for the owner

1. **A printed figure and its `00` are one number.** In all 34 printed places the screen's sum is the figure and the
   literal `00K…` of the next line together: grouping must format n×100 and then drop the `00` from the suffix
   ("777" + "00KG）" → "77,700KG）"), keyed by the pair `H` line / suffix line. With n = 0 the original shows
   `000K GOLD` (an empty treasury, or `Gﾌﾚｲｱ.ADV:3073` with Gold below 2): keep `000K`, or print `0K`?
2. **Fixed widths.** `H 5` and `H 3` (`Cﾌﾞｶ.ADV:884`, `:895`, `:1022`; `Sｱｲﾃﾑ.ADV:541`; `Sﾌﾞﾀｲ.ADV:336`, `:353`,
   `:531`) pad the figure on the left to 5 or 3 places and keep only the last 5 or 3 digits (xsystem35's
   `format_number`): a figure of 1000 or more in an `H 3` shows its last three digits. Should a separator keep the
   column's width (padding counted with it), and should the truncation stay (a quirk the port keeps)?
3. **Which surfaces.** The menu items (`Cｼｮｳﾆﾝ.ADV:267`, `:271`, `:604`, `:608`; `Gｶﾊﾟｰﾗ.ADV:2039`, `:2045`,
   `:2051`) are drawn by our `]` menus (`SceneMenuView`), not the message windows; the pickers' lines (`Sｱｲﾃﾑ`,
   `Sﾌﾞﾀｲ`) by the original's picker screens. In our interface several places are covered by our own screens already
   (the status window by the status strip, `MAIN`'s windows by the week report, `Cﾌﾞｶ`'s questions answered unseen
   by our Subordinates screen, the pickers by the deployment and Treasure screens), so the separator matters there
   only with `--interface original`, which by options.md keeps the original's numbers. Include the menus? Leave the
   covered places out?
4. **Text around the figure.** The troop counts on the same line (`Cｼｮｳﾆﾝ.ADV:252` "200 Troops", `:254`, `:589`,
   `:591` "1000 Troops", "5000 Troops") are not money: leave them, or group them too, as our screens do troops? The
   minus in `MAIN.ADV:194-208` is a hyphen (`-2000K GOLD`); our screens use U+2212. Two literals split the figure from
   its `GOLD` across a line break (`Gﾄｳｹﾞﾝ.ADV:139`/`:141`, `:224`/`:226`) — only the `10000K` line needs the change.
5. **Overflow.** `Gold` is 0-65535: the longest printed sum grows from `6553500K GOLD` (13 characters) to
   `6,553,500K GOLD` (15); the longest line, `Cｼﾊｲﾄｼ.ADV:889-892`, from "of 6553500K GOLD was collected." to 33
   characters. A literal grows by one character at most ("Price: 100,000K GOLD"). Windows 111-123 have 31 cells a line
   (docs/ui/messages.md, §1) and the translation's lines run to about 30 half-width characters, so only an
   implausible treasury could reach a line's end — to be checked against our `MessageWindow`'s width when it is
   built; the pickers' lines with `H 5` sit in fixed columns and would shift.
6. **The whole-GOLD prose** (§5) already uses commas in some lines and not in others ("50000 GOLD", "10000 GOLD").
   Out of this request's scope (it is the translation's text, not a figure of the engine); left as is unless the owner
   asks.
