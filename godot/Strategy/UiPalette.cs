using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The colours of our screens by their role: the lacquer-and-gold style's (the owner's mock-up 4a, docs/ui/command-screen.md)
/// and the ledger's paper and inks (docs/ui/subordinates.md, G3). A screen asks <see cref="UiPalettes.For"/> for its part's
/// palette and draws with its roles and parts (<c>UiPalette.Drawing.cs</c>); the palettes are <see cref="Lacquer"/>, the
/// default, and <see cref="Steel"/> (owner, 2026-10-24: Option → Display → Palette, docs/ui/options.md). A palette is a
/// record, so a mixed one is a <c>with</c> of another.
/// </summary>
internal sealed partial record UiPalette
{
    public required string Key { get; init; }       // in settings.cfg and --palette

    // The lacquer: panels, rows, gold, their texts.
    public required Color Panel { get; init; }            // a panel's lacquer (a little see-through)
    public required Color Backdrop { get; init; }         // a whole screen's backdrop under its wave pattern
    public required Color Gold { get; init; }             // the frames, the dividers' diamonds
    public required Color DimGold { get; init; }          // the inner frames, a row's frame
    public required Color Name { get; init; }            // names, a button's text
    public required Color Subtitle { get; init; }
    public required Color Label { get; init; }
    public required Color Value { get; init; }
    public required Color Row { get; init; }              // a row's box, a card, a button
    public required Color RowHover { get; init; }
    public required Color RowPressed { get; init; }
    public required Color HoverLine { get; init; }        // a hovered row's frame, the accent
    public required Color Text { get; init; }
    public required Color Quiet { get; init; }
    public required Color OffText { get; init; }          // what cannot be chosen
    public required Color OffTag { get; init; }
    public required Color SpentPip { get; init; }
    public required Color OffButton { get; init; }
    public required Color OffLine { get; init; }
    public required Color BadgeFill { get; init; }        // inside a place's gold diamond
    public required Color FaceBack { get; init; }         // behind a face
    public required Color ChosenFill { get; init; }       // the chosen row or cell, the one a callout asks about
    public required Color CalloutFill { get; init; }      // a callout's lacquer
    public required Color RowShade { get; init; }         // a list's row at rest (the save list)
    public required Color Rim { get; init; }              // the darkest outline (the star's)
    public required Color Groove { get; init; }           // a scroll bar's track
    public required Color Seam { get; init; }             // a faint rule on the lacquer
    public required Color ThoughtFill { get; init; }      // a thought's window
    public required Color NarrationFill { get; init; }    // the narration's window
    public required Color NarrationText { get; init; }
    public required Color DockBand { get; init; }         // the band behind a docked window
    public required Color Field { get; init; }            // a field or button on the paper

    // The ledger: our tables as paper in their lacquer frames (the owner's mock-ups G3 and E1/E2).
    public required Color Paper { get; init; }
    public required Color PaperHead { get; init; }
    public required Color PaperRule { get; init; }
    public required Color Ink { get; init; }
    public required Color InkQuiet { get; init; }
    public required Color InkFaint { get; init; }          // fainter than quiet, still apart from the grey rows
    public required Color InkGrey { get; init; }
    public required Color VermilionInk { get; init; }
    public required Color HeadInk { get; init; }
    public required Color Vermilion { get; init; }
    public required Color DangerFill { get; init; }       // a button that deletes: dark red lacquer in a vermilion frame
    public required Color DangerHover { get; init; }
    public required Color DangerLine { get; init; }       // its inner frame
    public required Color DangerText { get; init; }
    public required Color ChosenRow { get; init; }
    public required Color PartyRow { get; init; }
    public required Color SealText { get; init; }
    public required Color BarGood { get; init; }
    public required Color BarLow { get; init; }
    public required Color BarBad { get; init; }
    public required Color Plus { get; init; }
    public required Color Minus { get; init; }

    // The marks on Talk and the harem (owner's decision 30, docs/ui/talk-marks.md §6: H and S): NEW green, ??? amber on the
    // lacquer, a pill's dark ink; on the ledger's paper green and vermilion ink, a read talk's faint outline.
    public required Color MarkNew { get; init; }
    public required Color MarkMaybe { get; init; }
    public required Color PillInk { get; init; }
    public required Color InkGreen { get; init; }
    public required Color ReadOutline { get; init; }

    // The battle board's backdrop (owner, 2026-10-24, mock-ups S3 and S4; docs/ui/battle-screen.md, "The board's
    // backdrop"): Sengoku Rance's broken stones, their gaps, and the line where an empty place's card would stand.
    public required Color Stone { get; init; }
    public required Color StoneGap { get; init; }
    public required Color PlaceLine { get; init; }

    /// <summary>The name in the options: "Steel" for steel.</summary>
    public string Title => char.ToUpperInvariant(Key[0]) + Key[1..];

    /// <summary>Our screens' lacquer and gold, the default.</summary>
    public static readonly UiPalette Lacquer = new()
    {
        Key = "lacquer",
        Panel = new(22 / 255f, 16 / 255f, 12 / 255f, 0.96f),
        Backdrop = Color.Color8(14, 10, 8),
        Gold = Color.Color8(196, 160, 86),
        DimGold = Color.Color8(110, 86, 46),
        Name = Color.Color8(232, 199, 122),
        Subtitle = Color.Color8(160, 140, 104),
        Label = Color.Color8(150, 134, 108),
        Value = Color.Color8(236, 226, 204),
        Row = Color.Color8(34, 26, 20),
        RowHover = Color.Color8(64, 48, 28),
        RowPressed = Color.Color8(24, 18, 13),
        HoverLine = Color.Color8(222, 186, 104),
        Text = Color.Color8(244, 232, 206),
        Quiet = Color.Color8(176, 160, 130),
        OffText = Color.Color8(98, 88, 74),
        OffTag = Color.Color8(52, 46, 40),
        SpentPip = Color.Color8(60, 50, 38),
        OffButton = Color.Color8(40, 35, 30),
        OffLine = Color.Color8(66, 58, 48),
        BadgeFill = Color.Color8(60, 44, 24),
        FaceBack = Color.Color8(52, 40, 28),
        ChosenFill = Color.Color8(60, 30, 22),
        CalloutFill = Color.Color8(30, 22, 16, 250),
        RowShade = Color.Color8(26, 20, 16, 230),
        Rim = Color.Color8(26, 18, 12),
        Groove = Color.Color8(40, 32, 24),
        Seam = Color.Color8(60, 46, 30),
        ThoughtFill = Color.Color8(30, 26, 30),
        NarrationFill = Color.Color8(52, 38, 24),
        NarrationText = Color.Color8(226, 204, 150),
        DockBand = Color.Color8(10, 8, 6),
        Field = Color.Color8(236, 224, 194),
        Paper = Color.Color8(224, 208, 172),
        PaperHead = Color.Color8(196, 176, 136),
        PaperRule = Color.Color8(150, 120, 80, 110),
        Ink = Color.Color8(40, 28, 18),
        InkQuiet = Color.Color8(100, 80, 56),
        InkFaint = Color.Color8(130, 112, 86),
        InkGrey = Color.Color8(150, 132, 104),
        VermilionInk = Color.Color8(150, 52, 30),
        HeadInk = Color.Color8(120, 40, 26),
        Vermilion = Color.Color8(170, 40, 28),
        DangerFill = Color.Color8(52, 20, 14),
        DangerHover = Color.Color8(80, 28, 18),
        DangerLine = Color.Color8(110, 40, 28),
        DangerText = Color.Color8(236, 132, 104),
        ChosenRow = Color.Color8(190, 60, 40, 60),
        PartyRow = Color.Color8(200, 160, 80, 70),
        SealText = Color.Color8(246, 230, 206),
        BarGood = Color.Color8(70, 110, 50),
        BarLow = Color.Color8(160, 110, 30),
        BarBad = Color.Color8(170, 50, 36),
        Plus = Color.Color8(150, 196, 110),
        Minus = Color.Color8(222, 110, 90),
        MarkNew = Color.Color8(150, 196, 110),
        MarkMaybe = Color.Color8(226, 168, 72),
        PillInk = Color.Color8(24, 18, 12),
        InkGreen = Color.Color8(52, 104, 40),
        ReadOutline = Color.Color8(150, 132, 104, 160),
        Stone = Color.Color8(72, 65, 54),
        StoneGap = Color.Color8(30, 24, 19),
        PlaceLine = Color.Color8(110, 86, 46),
    };

    /// <summary>
    /// Gold on grey steel (the battle screen's steel palette of 2026-10-12 carried over every screen): cool greys for the
    /// lacquer, a cool paper with slate inks for the ledger; the red, the bars and the marks stay.
    /// </summary>
    public static readonly UiPalette Steel = Lacquer with
    {
        Key = "steel",
        Panel = new(43 / 255f, 45 / 255f, 49 / 255f, 0.96f),
        Backdrop = Color.Color8(26, 27, 30),
        Gold = Color.Color8(201, 168, 90),
        DimGold = Color.Color8(120, 104, 70),
        Name = Color.Color8(244, 230, 192),
        Subtitle = Color.Color8(160, 163, 168),
        Label = Color.Color8(150, 153, 158),
        Value = Color.Color8(238, 240, 242),
        Row = Color.Color8(59, 62, 68),
        RowHover = Color.Color8(87, 91, 99),
        RowPressed = Color.Color8(52, 54, 59),
        HoverLine = Color.Color8(242, 199, 92),
        Text = Color.Color8(240, 240, 238),
        Quiet = Color.Color8(165, 167, 171),
        OffText = Color.Color8(108, 111, 117),
        OffTag = Color.Color8(58, 60, 65),
        SpentPip = Color.Color8(68, 71, 77),
        OffButton = Color.Color8(47, 49, 53),
        OffLine = Color.Color8(74, 77, 83),
        BadgeFill = Color.Color8(59, 62, 68),
        FaceBack = Color.Color8(50, 53, 58),
        ChosenFill = Color.Color8(72, 44, 40),
        CalloutFill = Color.Color8(40, 42, 46, 250),
        RowShade = Color.Color8(34, 36, 40, 230),
        Rim = Color.Color8(24, 25, 28),
        Groove = Color.Color8(47, 49, 53),
        Seam = Color.Color8(74, 77, 83),
        ThoughtFill = Color.Color8(36, 38, 44),
        NarrationFill = Color.Color8(52, 55, 61),
        NarrationText = Color.Color8(214, 218, 224),
        DockBand = Color.Color8(12, 13, 15),
        Field = Color.Color8(226, 229, 233),
        Paper = Color.Color8(217, 220, 224),
        PaperHead = Color.Color8(185, 190, 198),
        PaperRule = Color.Color8(124, 133, 146, 110),
        Ink = Color.Color8(31, 36, 43),
        InkQuiet = Color.Color8(79, 89, 102),
        InkFaint = Color.Color8(111, 120, 132),
        InkGrey = Color.Color8(138, 146, 156),
        HeadInk = Color.Color8(45, 74, 110),
        PartyRow = Color.Color8(201, 168, 90, 70),
        PillInk = Color.Color8(24, 25, 28),
        ReadOutline = Color.Color8(138, 146, 156, 160),
        Stone = Color.Color8(68, 71, 75),
        StoneGap = Color.Color8(32, 34, 38),
        PlaceLine = Color.Color8(80, 175, 75),      // Sengoku Rance's own green
    };

    public static IReadOnlyList<UiPalette> All { get; } = [Lacquer, Steel];

    /// <summary>The palette of that key, or null.</summary>
    public static UiPalette? Named(string key) => All.FirstOrDefault(p => p.Key == key);
}
