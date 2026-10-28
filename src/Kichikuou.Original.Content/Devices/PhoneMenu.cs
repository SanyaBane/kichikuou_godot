// The questions' texts are xsystem35's (https://github.com/kichikuou/xsystem35-sdl2): menu.c (the dialogs), Copyright
// (C) 2000- Masaki Chikama (Wren). GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The phone's Menu sheet held landscape (docs/ui/phone-build.md, step L1; docs/ui/phone.md, §26, the owner's LN3): the
/// menu bar's commands (docs/window-menu.md) as tiles four a row in a panel over the frame's middle, the screen dimmed;
/// the left side's Menu cell, which opened it, reads Close. Screen (Window | Fullscreen) is left out — a phone is always
/// full screen. A tile acts and the sheet closes; a switch's tile (Message skip, Sound) changes and the sheet stays; Restart
/// and Exit ask first in a callout at their tile, in the PC's dialogs' words; Debug (a development build's) opens its own
/// tiles, ‹ Menu first. The figures are the mock-up's in the reference phone's pixels (<see cref="PhoneFrame.Px"/>), the
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
    }

    /// <summary>A tile: its command, word and kind; a switch's state; greyed and deaf when <see cref="Disabled"/>.</summary>
    public sealed record Tile(Command Command, string Label, Kind Kind, bool On = false, bool Disabled = false)
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

    /// <summary>The first page: the menu bar's File, Options, Sound and MsgSkip, Debug in a development build.</summary>
    public static IReadOnlyList<Tile> Main(Settings s) =>
    [
        new(Command.Options, "Options", Kind.Act),
        new(Command.MessageSkip, "Message skip", Kind.Switch, s.SkipOn, !s.SkipEnabled),
        new(Command.Sound, "Sound", Kind.Switch, s.SoundOn),
        new(Command.Screenshot, "Screenshot", Kind.Act),
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

    /// <summary>A question's callout: at most this wide and this high, this far from its tile; its buttons' height and margins.</summary>
    private const float QuestionWidth = 1100, QuestionHeight = 420, QuestionGap = 30, ButtonHeight = 130, ButtonMargin = 40, ButtonGap = 8;

    /// <summary>The sheet's panel over the frame's middle.</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f)
    {
        float x = f.Frame.X + f.Px(PanelSide), y = f.LeftRoom.Y + f.Px(PanelTop);
        return new PhoneFrame.Box(x, y, f.Frame.Right - f.Px(PanelSide) - x, f.LeftRoom.Bottom - f.Px(PanelFoot) - y);
    }

    /// <summary>The panel's title ("Menu"): its left edge and the middle of its line.</summary>
    public static (float X, float Y) Title(PhoneFrame f) => (Panel(f).X + f.Px(40), Panel(f).Y + f.Px(70));

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
    /// pointing down at it, unless the screen's top leaves no room there — then under it, pointing up. Cancel and the
    /// confirming button side by side at its foot, the confirming one at the right.
    /// </summary>
    public static Callout QuestionAt(PhoneFrame f, PhoneFrame.Box tile)
    {
        var p = Panel(f);
        float margin = f.Px(TilesMargin);
        float w = Math.Min(f.Px(QuestionWidth), p.W - 2 * margin), h = f.Px(QuestionHeight), gap = f.Px(QuestionGap);
        float x = Math.Clamp(tile.X, p.X + margin, p.Right - margin - w);
        bool below = tile.Y - gap - h < f.LeftRoom.Y + f.Px(10);
        float y = below ? tile.Bottom + gap : tile.Y - gap - h;
        var box = new PhoneFrame.Box(x, y, w, h);
        float bm = f.Px(ButtonMargin), bh = f.Px(ButtonHeight), bg = f.Px(ButtonGap), mid = x + w / 2;
        float by = box.Bottom - bm - bh;
        return new Callout(box, tile.X + tile.W / 2, below ? tile.Bottom : tile.Y, below,
            new PhoneFrame.Box(x + bm, by, mid - bg - (x + bm), bh), new PhoneFrame.Box(mid + bg, by, box.Right - bm - (mid + bg), bh));
    }
}
