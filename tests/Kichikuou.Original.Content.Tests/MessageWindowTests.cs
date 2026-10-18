using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our message windows (docs/ui/messages.md, step 1): the model <see cref="MessageWindow"/> from <c>WAO_SUB</c>'s frame and the
/// text box, and <see cref="GamePresenter"/> leaving the frame and the text out while a UI draws them.
/// </summary>
public class MessageWindowTests
{
    private static readonly Func<int, (byte, byte, byte)> NoColour = _ => (0, 0, 0);

    /// <summary>Each frame <c>WAO_SUB</c> opens is its kind at the mock-ups' place; the full-screen window and none are not ours.</summary>
    [Theory]
    [InlineData(807, MessageKind.SpeechLeft, 436, 4)]
    [InlineData(808, MessageKind.ThoughtLeft, 436, 4)]
    [InlineData(810, MessageKind.SpeechRight, 58, 512)]
    [InlineData(811, MessageKind.ThoughtRight, 58, 512)]
    [InlineData(809, MessageKind.Narration, 248, 536)]
    public void A_frame_is_its_kind_of_window(int frame, MessageKind kind, int x, int y)
    {
        var window = MessageWindow.Of(frame, new TextBox((0, 0, 640, 400)), NoColour);
        Assert.NotNull(window);
        Assert.Equal(kind, window.Kind);
        Assert.Equal((x, y), (window.Panel.X, window.Panel.Y));
        Assert.Empty(window.Glyphs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(815)]
    [InlineData(894)]
    public void Other_frames_are_not_ours(int frame) => Assert.Null(MessageWindow.Of(frame, new TextBox((0, 0, 640, 400)), NoColour));

    /// <summary>
    /// The characters stay at the cells the engine placed them in, in the screen's pixels; the lines get their spaces back
    /// from the cells, so a table line keeps its columns (<c>Cﾌﾞｶ</c>'s 121); a colour other than the window's is kept.
    /// </summary>
    [Fact]
    public void The_text_keeps_its_cells_and_its_columns()
    {
        var box = new TextBox((43, 265, 378, 141));                            // 121's text box
        box.Print("Barres's unit.\nHP    4/    5", 24, MessageWindow.PlainColor);
        box.NewLine(24);
        box.Print("Troops", 24, 3);
        var window = MessageWindow.Of(810, box, i => ((byte)i, 0, 0))!;

        Assert.Equal(["Barres's unit.", "HP    4/    5", "Troops"], window.Lines);
        Assert.Equal(new MessageGlyph(86, 534, 48, 'B', null), window.Glyphs[0]);
        Assert.Equal(new MessageGlyph(86 + 24, 534, 48, 'a', null), window.Glyphs[1]);
        Assert.Equal((86, 534 + 2 * 52), (window.Glyphs.First(g => g.C == 'T').X, window.Glyphs.First(g => g.C == 'T').Y));
        Assert.Equal(((byte)3, (byte)0, (byte)0), window.Glyphs.First(g => g.C == 'T').Colour);
        Assert.Equal("", window.Title);
    }

    /// <summary>A narration whose first line is at <c>ZM 32</c> is a place's title, the room under it (<c>Cｼﾊｲﾄｼ.ADV:1185-1192</c>).</summary>
    [Fact]
    public void A_line_at_ZM_32_is_a_places_title()
    {
        var box = new TextBox((140, 278, 376, 140));                           // 113's
        box.Print("　　　Leazas Castle\n", 32, MessageWindow.PlainColor);
        box.Print("\n　　　　　Audience Room", 24, MessageWindow.PlainColor);
        var title = MessageWindow.Of(809, box, NoColour)!;
        Assert.Equal((MessageKind.PlaceTitle, "Leazas Castle", "Audience Room"), (title.Kind, title.Title, title.Subtitle));

        box.Clear();
        box.Print("　　　Titi Lake", 32, MessageWindow.PlainColor);              // G1.ADV:105-107: the title alone
        var alone = MessageWindow.Of(809, box, NoColour)!;
        Assert.Equal((MessageKind.PlaceTitle, "Titi Lake", ""), (alone.Kind, alone.Title, alone.Subtitle));

        box.Clear();
        box.Print("Rance descended into Leazas", 24, MessageWindow.PlainColor);
        Assert.Equal(MessageKind.Narration, MessageWindow.Of(809, box, NoColour)!.Kind);

        box.Clear();
        box.Print("< Load Complete >", 32, MessageWindow.PlainColor);       // more than a room under it: a narration
        box.Print("\nData Number(1)\nLP: 3", 24, MessageWindow.PlainColor);
        Assert.Equal(MessageKind.Narration, MessageWindow.Of(809, box, NoColour)!.Kind);

        var speech = new TextBox((235, 8, 378, 141));                           // 111's
        speech.Print("A title", 32, MessageWindow.PlainColor);                 // a speech window at ZM 32 stays a speech
        Assert.Equal(MessageKind.SpeechLeft, MessageWindow.Of(807, speech, NoColour)!.Kind);
    }

    /// <summary>Text outside the panel (a box <c>B4</c> moved while the window stays) leaves the window to the original's drawing.</summary>
    [Fact]
    public void Text_outside_the_panel_is_not_ours()
    {
        var box = new TextBox((10, 10, 200, 100));
        box.Print("Elsewhere", 24, MessageWindow.PlainColor);
        Assert.Null(MessageWindow.Of(810, box, NoColour));
    }

    [Fact]
    public void Two_windows_showing_the_same_are_the_same()
    {
        var box = new TextBox((43, 265, 378, 141));
        box.Print("Sorry.", 24, MessageWindow.PlainColor);
        var a = MessageWindow.Of(810, box, NoColour)!;
        Assert.True(a.SameAs(MessageWindow.Of(810, box, NoColour)));
        Assert.False(a.SameAs(MessageWindow.Of(811, box, NoColour)));
        box.Print("!", 24, MessageWindow.PlainColor);
        Assert.False(a.SameAs(MessageWindow.Of(810, box, NoColour)));
        Assert.False(a.SameAs(null));
    }

    /// <summary>
    /// <see cref="GamePresenter.CoverMessages"/>: while a 121 is up, the presenter gives its text as the model and leaves the
    /// text out of the screen (the frame too: the stage under it shows); without the cover, or once the window closes, none.
    /// </summary>
    [Fact]
    public void The_presenter_leaves_out_the_window_a_UI_draws()
    {
        var (presenter, state) = Presenter();
        Open(presenter, state, 121, 21, 810);
        state.Strings[1] = "Sorry, Darling.";
        presenter.Show(new PrintStringEvent(1));

        Assert.Null(presenter.MessageWindow);                                   // not covered: the original's window
        Assert.True(TextDrawn(presenter));

        presenter.CoverMessages = true;
        Assert.Equal(["Sorry, Darling."], presenter.MessageWindow!.Lines);
        Assert.Equal(MessageKind.SpeechRight, presenter.MessageWindow.Kind);
        Assert.False(TextDrawn(presenter));

        presenter.Show(new WindowEvent(0, 0));                                  // closed
        state.Ui_WinType = 0;
        state.Ui_WinFrameCg = 0;
        Assert.Null(presenter.MessageWindow);
    }

    /// <summary>In a battle (step 5), under All Data or the week report, and for the full-screen window (step 2) the presenter keeps drawing.</summary>
    [Fact]
    public void The_presenter_keeps_the_windows_it_does_not_hand_over()
    {
        var (presenter, state) = Presenter();
        presenter.CoverMessages = true;
        Open(presenter, state, 131, 31, 815);
        Assert.Null(presenter.MessageWindow);

        Open(presenter, state, 111, 11, 807);
        Assert.NotNull(presenter.MessageWindow);
        presenter.Show(new BattleScreenEvent("FIGT.ADV", true));
        Assert.Null(presenter.MessageWindow);
        presenter.Show(new BattleScreenEvent("FIGT.ADV", false));
        Assert.NotNull(presenter.MessageWindow);
        presenter.MenuCovered = true;                                           // our province menu covers the window already
        Assert.Null(presenter.MessageWindow);
    }

    /// <summary>A menu over the window is the screen's, shown again over ours: its box with the frame, in the screen's pixels.</summary>
    [Fact]
    public void A_menu_gives_its_area()
    {
        var (presenter, state) = Presenter();
        Assert.Null(presenter.MenuArea);
        presenter.OpenMenu([]);                                                 // the boot's selection window 1: (250, 50), 150 wide
        Assert.Equal((484, 84, 340, 36), presenter.MenuArea);
        presenter.CloseMenu();
        Assert.Null(presenter.MenuArea);
    }

    private static (GamePresenter, GameState) Presenter()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState();
        presenter.Bind(state);
        return (presenter, state);
    }

    /// <summary><c>WAO_SUB</c> opens a window: the event first, then its state (<c>OriginalScriptContext.WaoOpen</c>).</summary>
    private static void Open(GamePresenter presenter, GameState state, int op, int type, int frame)
    {
        presenter.Show(new WindowEvent((Sys3Int)op, 0));
        state.Ui_WinType = (Sys3Int)type;
        state.Ui_WinFrameCg = (Sys3Int)frame;
        presenter.Invalidate();
    }

    /// <summary>Whether anything is drawn in 121's text area (the stage is black, no frame CG is there).</summary>
    private static bool TextDrawn(GamePresenter presenter)
    {
        presenter.Invalidate();
        presenter.Compose();
        for (int y = 265; y < 300; y++)
            for (int x = 43; x < 300; x++)
                if (presenter.Screen.PixelAt(x, y) is var (r, g, b, _) && (r | g | b) != 0)
                    return true;
        return false;
    }
}
