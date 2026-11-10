// The questions' texts are xsystem35's (https://github.com/kichikuou/xsystem35-sdl2): menu.c (the dialogs), Copyright
// (C) 2000- Masaki Chikama (Wren). GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using Kichikuou.Original.Content.Saves;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The phone's Menu sheet held landscape (docs/ui/phone-build.md, step L1; docs/ui/phone.md, §26, the owner's LN3): the
/// menu bar's commands (docs/window-menu.md) as tiles four a row in a panel over the frame's middle, the screen dimmed;
/// the left side's Menu cell, which opened it, reads Close. Screen (Window | Fullscreen) is left out — a phone is always
/// full screen. A tile acts and the sheet closes; a switch's tile (Message skip, Sound) changes and the sheet stays; Restart
/// and Exit ask first in a callout at their tile, in the PC's dialogs' words; Debug (a development build's) opens its own
/// tiles, ‹ Menu first. Held upright (step P1; phone.md, §25, PN1, PN1q) the sheet rises from the bar, the bar's Menu cell
/// reading Close: the tiles are rows across the screen from the thumb up, a question's callout over its row. The figures
/// are the mock-up's in the reference phone's pixels (<see cref="PhoneFrame.Px"/>), the
/// boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneMenu
{
    /// <summary>A tile's command: the menu bar's, Debug's, and ‹ Menu back to the first page.</summary>
    public enum Command
    {
        /// <summary>The options window.</summary>
        Options,
        /// <summary>MsgSkip, a switch.</summary>
        MessageSkip,
        /// <summary>Sound, a switch.</summary>
        Sound,
        /// <summary>File → Save Screenshot.</summary>
        Screenshot,
        /// <summary>File → Restart, asking first.</summary>
        Restart,
        /// <summary>File → Exit, asking first.</summary>
        Exit,
        /// <summary>Debug's page (a development build's).</summary>
        Debug,
        /// <summary>‹ Menu: back to the first page.</summary>
        Back,
        /// <summary>Debug → Battle Captures, asking first.</summary>
        BattleCaptures,
        /// <summary>Debug → Portraits.</summary>
        Portraits,
        /// <summary>Debug → Effects.</summary>
        Effects,
        /// <summary>Debug → Screen Gallery, asking first.</summary>
        ScreenGallery,
        /// <summary>Debug → Hide Dimmed Map Names, a switch.</summary>
        HideDimmedNames,
        /// <summary>Debug → Menu Row Height.</summary>
        MenuRowHeight,
        /// <summary>File → Import Original Save…: the page of the game folder's saves (docs/save-import.md, §8).</summary>
        ImportSave,
        /// <summary>A save of that page (<see cref="Tile.Index"/> its place in <see cref="SaveShelf.Saves"/>), asking first.</summary>
        OriginalSave,
        /// <summary>The page's note when the folder holds no save: words, no tap.</summary>
        Note,
    }

    /// <summary>What a tap on a tile does.</summary>
    public enum Kind
    {
        /// <summary>Acts; the sheet closes.</summary>
        Act,
        /// <summary>Changes a switch; the sheet stays.</summary>
        Switch,
        /// <summary>Asks first (<see cref="QuestionOf"/>), then acts.</summary>
        Asks,
        /// <summary>Opens a page of tiles.</summary>
        Page,
        /// <summary>A save of the original: asks before its load, or says why it cannot be imported (<see cref="Tile.Refused"/>).</summary>
        Save,
        /// <summary>Words across the page (<see cref="Command.Note"/>), not a tile to tap.</summary>
        Note,
    }

    /// <summary>
    /// A tile: its command, word and kind; a switch's state; greyed and deaf when <see cref="Disabled"/>. A save's tile
    /// (<see cref="Kind.Save"/>) has its place in the shelf, its date and its stage — or, <see cref="Refused"/>, the reason in a
    /// word —, greyed but answering with the reason.
    /// </summary>
    public sealed record Tile(Command Command, string Label, Kind Kind, bool On = false, bool Disabled = false, int Index = -1,
        string Date = "", string Stage = "", bool Refused = false)
    {
        /// <summary>The small line under the word: a switch's state, "asks first", a page's arrow.</summary>
        public string Sub => Kind switch
        {
            Kind.Switch => On ? "On" : "Off",
            Kind.Asks => "asks first",
            Kind.Page when Command != Command.Back => "›",
            _ => "",
        };
    }

    /// <summary>A question before a command (the PC's confirmation box): its title, its line and the confirming button's word.</summary>
    public sealed record Question(string Title, string Line, string Confirm);

    /// <summary>What the tiles show: the message skip's switch and whether it may be used now, the sound's, a development build's Debug, the map's dimmed names.</summary>
    public readonly record struct Settings(bool SkipOn, bool SkipEnabled, bool SoundOn, bool Development, bool HideDimmedNames);

    /// <summary>The PC's confirmation boxes' line (<c>confirm_lose_progress</c>).</summary>
    public const string LoseProgress = "Any unsaved progress will be lost.";

    /// <summary>The game folder's saves of the original (<see cref="OriginalSaveFolder.Read"/>) and the folder as the page names it.</summary>
    public sealed record SaveShelf(IReadOnlyList<OriginalSaveFile> Saves, string Folder);

    /// <summary>
    /// The page of the game folder's saves (docs/save-import.md, §8, the owner's S1): ‹ Menu first, then a tile per save —
    /// its word ("Save E"), its date and stage, or the reason it cannot be imported; with none, the note where they go.
    /// </summary>
    public static IReadOnlyList<Tile> Saves(SaveShelf shelf) => shelf.Saves.Count == 0
        ? [new(Command.Back, "‹ Menu", Kind.Page), new(Command.Note, NoSaves(shelf.Folder), Kind.Note)]
        : [new(Command.Back, "‹ Menu", Kind.Page),
            .. shelf.Saves.Select((s, i) => s.Refused
                ? new Tile(Command.OriginalSave, s.Word, Kind.Save, Index: i, Date: "cannot be imported", Stage: s.RefusalWord, Refused: true)
                : new Tile(Command.OriginalSave, s.Word, Kind.Save, Index: i, Date: s.ShortDate, Stage: s.StageText))];

    /// <summary>The page's title.</summary>
    public const string SavesTitle = "Original saves";

    /// <summary>The note of a page with no save: its paragraphs, the last the folder (mock-up S1n).</summary>
    public static string NoSaves(string folder) =>
        "No saves of the original in the game folder.\n" +
        "The original writes them next to KICHIKUSA.ALD, as KICHIKUSA.asd … KICHIKUSJ.asd. Copy them with the game into\n" +
        folder;

    /// <summary>The question before a save's load: the PC's (docs/save-import.md, §7), its lines one under another.</summary>
    public static Question ImportQuestion(OriginalSaveFile save) =>
        new("Load the original's save?", string.Join("\n", [.. save.QuestionLines, LoseProgress]), "Load");

    /// <summary>A refused save's notice: the PC's message box, its one button OK (an empty <see cref="Question.Confirm"/>).</summary>
    public static Question RefusalOf(OriginalSaveFile save) => new($"{save.File} cannot be imported.", save.Refusal ?? "", "");

    /// <summary>The first page: the menu bar's File, Options, Sound and MsgSkip, Debug in a development build.</summary>
    public static IReadOnlyList<Tile> Main(Settings s) =>
    [
        new(Command.Options, "Options", Kind.Act),
        new(Command.MessageSkip, "Message skip", Kind.Switch, s.SkipOn, !s.SkipEnabled),
        new(Command.Sound, "Sound", Kind.Switch, s.SoundOn),
        new(Command.Screenshot, "Screenshot", Kind.Act),
        new(Command.ImportSave, "Import original save", Kind.Page),       // as the PC's File menu (docs/save-import.md, §8)
        new(Command.Restart, "Restart", Kind.Asks),
        new(Command.Exit, "Exit", Kind.Asks),
        .. s.Development ? [new Tile(Command.Debug, "Debug", Kind.Page)] : Array.Empty<Tile>(),
    ];

    /// <summary>Debug's page: the menu bar's Debug menu, ‹ Menu first.</summary>
    public static IReadOnlyList<Tile> Debug(Settings s) =>
    [
        new(Command.Back, "‹ Menu", Kind.Page),
        new(Command.BattleCaptures, "Battle Captures", Kind.Asks),
        new(Command.Portraits, "Portraits", Kind.Act),
        new(Command.Effects, "Effects", Kind.Act),
        new(Command.ScreenGallery, "Screen Gallery", Kind.Asks),
        new(Command.HideDimmedNames, "Hide Dimmed Map Names", Kind.Switch, s.HideDimmedNames),
        new(Command.MenuRowHeight, "Menu Row Height", Kind.Act),
    ];

    /// <summary>The question a command asks first (the PC's confirmation box's words), or null.</summary>
    public static Question? QuestionOf(Command command) => command switch
    {
        Command.Restart => new("Restart game?", LoseProgress, "Restart"),
        Command.Exit => new("Quit game?", LoseProgress, "Quit"),
        Command.BattleCaptures => new("Open the battle captures?", LoseProgress, "Open"),
        Command.ScreenGallery => new("Open the screen gallery?", LoseProgress, "Open"),
        _ => null,
    };

    /// <summary>The panel's margins from the frame's sides, the screen's top and the gesture zone.</summary>
    private const float PanelSide = 160, PanelTop = 150, PanelFoot = 60;

    /// <summary>The tiles' room in the panel: under its title, within its margins; the room between tiles.</summary>
    private const float TilesTop = 140, TilesMargin = 30, TileGap = 16;

    /// <summary>The tiles a row.</summary>
    public const int Columns = 4;

    /// <summary>
    /// The tiles' words held landscape and the rows' upright: BIZ UDGothic, one size for the sheet (docs/ui/style.md,
    /// "Buttons side by side"), shrunk together where a word is too long for its tile or row.
    /// </summary>
    public const float TileText = 44, RowText = 46;

    /// <summary>A question's answers, the red button's and Cancel's: Cinzel 700 at one size, shrunk together.</summary>
    public const float ButtonText = 46;

    /// <summary>A question's callout: at most this wide and this high, this far from its tile; its buttons' height and margins.</summary>
    private const float QuestionWidth = 1100, QuestionHeight = 420, QuestionGap = 30, ButtonHeight = 130, ButtonMargin = 40, ButtonGap = 8;

    /// <summary>A question's lines: their pitch, and their room's margin at either side of the callout.</summary>
    public const float LinePitch = 50, LineMargin = 40;

    /// <summary>A question's callout's width: held landscape the mock-up's, kept within the panel; upright across the screen.</summary>
    public static float QuestionBoxWidth(PhoneFrame f) => f.IsPortrait
        ? f.Room.W - 2 * f.Px(UpQuestionSide)
        : Math.Min(f.Px(QuestionWidth), Panel(f).W - 2 * f.Px(TilesMargin));

    /// <summary>The room a question's lines have across its callout, which the view wraps them to.</summary>
    public static float QuestionTextWidth(PhoneFrame f) => QuestionBoxWidth(f) - 2 * f.Px(LineMargin);

    /// <summary>The sheet's panel over the frame's middle.</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f)
    {
        float x = f.Frame.X + f.Px(PanelSide), y = f.LeftRoom.Y + f.Px(PanelTop);
        return new PhoneFrame.Box(x, y, f.Frame.Right - f.Px(PanelSide) - x, f.LeftRoom.Bottom - f.Px(PanelFoot) - y);
    }

    /// <summary>The panel's title ("Menu"): its left edge and the middle of its line.</summary>
    public static (float X, float Y) Title(PhoneFrame f) => (Panel(f).X + f.Px(40), Panel(f).Y + f.Px(70));

    /// <summary>
    /// Portrait (docs/ui/phone.md, §25, PN1): the rows' height (7.4 mm) and the room between them, the rule's room, the rows'
    /// sides' distance from the screen's, the title's room over them, their foot's room over the bar, the panel's foot over
    /// the bar; the title's place in the panel.
    /// </summary>
    private const float UpNoteHeight = 560, UpRowHeight = 130, UpRowGap = 12, UpRule = 40, UpRowSide = 40, UpTitle = 130, UpOverBar = 40, UpPanelFoot = 14,
        UpTitleX = 32, UpTitleY = 66, UpQuestionSide = 60, UpQuestionGap = 44;

    /// <summary>The sheet's panel for <paramref name="tiles"/>: held landscape over the frame's middle, upright from the bar up round its rows (PN1).</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f, IReadOnlyList<Tile> tiles)
    {
        if (!f.IsPortrait)
            return Panel(f);
        float top = RowsTop(f, tiles) - f.Px(UpTitle);
        return new PhoneFrame.Box(f.Bar.X, top, f.Bar.W, f.Bar.Y - f.Px(UpPanelFoot) - top);
    }

    /// <summary>The panel's title for <paramref name="tiles"/>: its left edge and the middle of its line.</summary>
    public static (float X, float Y) Title(PhoneFrame f, IReadOnlyList<Tile> tiles) =>
        f.IsPortrait ? (Panel(f, tiles).X + f.Px(UpTitleX), Panel(f, tiles).Y + f.Px(UpTitleY)) : Title(f);

    /// <summary>
    /// The boxes of <paramref name="tiles"/>: held landscape <see cref="Columns"/> a row (<see cref="Tiles(PhoneFrame, int)"/>);
    /// upright rows across the screen ending over the bar (PN1) — the first page's from the thumb up (Options lowest), a
    /// rule between Save Screenshot and the switches, Debug on top; Debug's page top down, ‹ Menu on top.
    /// </summary>
    public static IReadOnlyList<PhoneFrame.Box> Tiles(PhoneFrame f, IReadOnlyList<Tile> tiles)
    {
        if (!f.IsPortrait)
            return LandscapeTiles(f, tiles);
        var boxes = new PhoneFrame.Box[tiles.Count];
        float y = RowsTop(f, tiles), x = f.Room.X + f.Px(UpRowSide), w = f.Room.W - 2 * f.Px(UpRowSide);
        foreach (int i in TopDown(tiles))
        {
            if (RuleOver(tiles, i))
                y += f.Px(UpRule);
            float h = f.Px(tiles[i].Kind == Kind.Note ? UpNoteHeight : UpRowHeight);
            boxes[i] = new PhoneFrame.Box(x, y, w, h);
            y += h + f.Px(UpRowGap);
        }
        return boxes;
    }

    /// <summary>
    /// The boxes held landscape: <see cref="Columns"/> a row, two rows at least (a short page's tiles the first page's size);
    /// a note from its cell to the last column's right and the grid's foot.
    /// </summary>
    private static IReadOnlyList<PhoneFrame.Box> LandscapeTiles(PhoneFrame f, IReadOnlyList<Tile> tiles)
    {
        var grid = Tiles(f, Math.Max(tiles.Count, 2 * Columns));
        var boxes = grid.Take(tiles.Count).ToArray();
        for (int i = 0; i < boxes.Length; i++)
        {
            if (tiles[i].Kind != Kind.Note)
                continue;
            var cell = grid[i];
            boxes[i] = new PhoneFrame.Box(cell.X, cell.Y, grid[Columns - 1].Right - cell.X, grid[^1].Bottom - cell.Y);
        }
        return boxes;
    }

    /// <summary>The tile of <paramref name="tiles"/> under (<paramref name="x"/>, <paramref name="y"/>), or -1 (a gap, off the tiles).</summary>
    public static int TileAt(PhoneFrame f, IReadOnlyList<Tile> tiles, float x, float y)
    {
        var boxes = Tiles(f, tiles);
        for (int i = 0; i < boxes.Count; i++)
            if (boxes[i].Contains(x, y))
                return i;
        return -1;
    }

    /// <summary>Whether a page is the first one, laid from the thumb up: not Debug's (‹ Menu first).</summary>
    private static bool FromThumb(IReadOnlyList<Tile> tiles) => tiles.Count == 0 || tiles[0].Command != Command.Back;

    private static IEnumerable<int> TopDown(IReadOnlyList<Tile> tiles) =>
        FromThumb(tiles) ? Enumerable.Range(0, tiles.Count).Reverse() : Enumerable.Range(0, tiles.Count);

    /// <summary>Whether the rule stands over row <paramref name="i"/>, top down: the first page's switches under Save Screenshot.</summary>
    private static bool RuleOver(IReadOnlyList<Tile> tiles, int i) =>
        FromThumb(tiles) && tiles[i].Command == Command.Sound && i + 1 < tiles.Count && tiles[i + 1].Command == Command.Screenshot;

    /// <summary>The rows' top held upright: their stack ends over the bar.</summary>
    private static float RowsTop(PhoneFrame f, IReadOnlyList<Tile> tiles)
    {
        int rules = Enumerable.Range(0, tiles.Count).Count(i => RuleOver(tiles, i));
        float stack = tiles.Sum(t => f.Px((t.Kind == Kind.Note ? UpNoteHeight : UpRowHeight) + UpRowGap)) - f.Px(UpRowGap)
            + rules * f.Px(UpRule);
        return f.Bar.Y - f.Px(UpOverBar) - stack;
    }

    /// <summary>The boxes of <paramref name="count"/> tiles, <see cref="Columns"/> a row, left to right and top down, filling the panel under its title.</summary>
    public static IReadOnlyList<PhoneFrame.Box> Tiles(PhoneFrame f, int count)
    {
        var p = Panel(f);
        float x0 = p.X + f.Px(TilesMargin), y0 = p.Y + f.Px(TilesTop), gap = f.Px(TileGap);
        float width = p.Right - f.Px(TilesMargin) - x0, height = p.Bottom - f.Px(TilesMargin) - y0;
        int rows = Math.Max(1, (count + Columns - 1) / Columns);
        float w = (width - (Columns - 1) * gap) / Columns, h = (height - (rows - 1) * gap) / rows;
        return [.. Enumerable.Range(0, count).Select(i => new PhoneFrame.Box(x0 + i % Columns * (w + gap), y0 + i / Columns * (h + gap), w, h))];
    }

    /// <summary>The tile of <paramref name="count"/> under (<paramref name="x"/>, <paramref name="y"/>), or -1 (a gap, off the tiles).</summary>
    public static int TileAt(PhoneFrame f, int count, float x, float y)
    {
        var tiles = Tiles(f, count);
        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i].Contains(x, y))
                return i;
        return -1;
    }

    /// <summary>A question's callout at its tile: its box, and its tip at the tile's top edge (or its foot when the callout stands under it).</summary>
    public sealed record Callout(PhoneFrame.Box Box, float TipX, float TipY, bool Below, PhoneFrame.Box Cancel, PhoneFrame.Box Confirm);

    /// <summary>
    /// The callout of the question a tile asks: as wide as the mock-up's (PN1q), kept within the panel; above the tile,
    /// pointing down at it, unless the screen's top leaves no room there — then under it, pointing up. The confirming
    /// button and Cancel side by side at its foot, Cancel at the right: a way back stands right of the action (owner,
    /// 2026-10-30).
    /// </summary>
    public static Callout QuestionAt(PhoneFrame f, PhoneFrame.Box tile, int lines = 1)
    {
        if (f.IsPortrait)
            return PortraitQuestion(f, tile, lines);
        var p = Panel(f);
        float margin = f.Px(TilesMargin);
        float w = QuestionBoxWidth(f), h = f.Px(QuestionHeight + (Math.Max(1, lines) - 1) * LinePitch), gap = f.Px(QuestionGap);
        float x = Math.Clamp(tile.X, p.X + margin, p.Right - margin - w);
        // Under the tile only when the top leaves no room over it and the foot does under it; else over it, kept on the screen.
        float above = tile.Y - gap - h, top = f.LeftRoom.Y + f.Px(10);
        bool below = above < top && tile.Bottom + gap + h <= f.LeftRoom.Bottom - f.Px(10);
        float y = below ? tile.Bottom + gap : Math.Max(top, above);
        var box = new PhoneFrame.Box(x, y, w, h);
        float bm = f.Px(ButtonMargin), bh = f.Px(ButtonHeight), bg = f.Px(ButtonGap), mid = x + w / 2;
        float by = box.Bottom - bm - bh;
        return new Callout(box, tile.X + tile.W / 2, below ? tile.Bottom : tile.Y, below,
            new PhoneFrame.Box(mid + bg, by, box.Right - bm - (mid + bg), bh), new PhoneFrame.Box(x + bm, by, mid - bg - (x + bm), bh));
    }

    /// <summary>A question's callout held upright (PN1q): across the screen over its row, pointing down at it; under it when the top leaves no room.</summary>
    private static Callout PortraitQuestion(PhoneFrame f, PhoneFrame.Box tile, int lines)
    {
        float x = f.Room.X + f.Px(UpQuestionSide), w = QuestionBoxWidth(f), gap = f.Px(UpQuestionGap);
        float h = f.Px(QuestionHeight + (Math.Max(1, lines) - 1) * LinePitch);
        float above = tile.Y - gap - h, top = f.Room.Y + f.Px(10);
        bool below = above < top && tile.Bottom + gap + h <= f.Room.Bottom - f.Px(10);
        var box = new PhoneFrame.Box(x, below ? tile.Bottom + gap : Math.Max(top, above), w, h);
        float bm = f.Px(ButtonMargin), bh = f.Px(ButtonHeight), bg = f.Px(ButtonGap), mid = x + w / 2, by = box.Bottom - bm - bh;
        return new Callout(box, tile.X + tile.W / 2, below ? tile.Bottom : tile.Y, below,
            new PhoneFrame.Box(mid + bg, by, box.Right - bm - (mid + bg), bh), new PhoneFrame.Box(x + bm, by, mid - bg - (x + bm), bh));
    }

}
