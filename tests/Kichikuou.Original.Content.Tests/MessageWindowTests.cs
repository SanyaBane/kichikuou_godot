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

    /// <summary>Each frame <c>WAO_SUB</c> opens is its kind at the mock-ups' place; the battle's bubbles and none are not ours.</summary>
    [Theory]
    [InlineData(807, MessageKind.SpeechLeft, 436, 4)]
    [InlineData(808, MessageKind.ThoughtLeft, 436, 4)]
    [InlineData(810, MessageKind.SpeechRight, 58, 512)]
    [InlineData(811, MessageKind.ThoughtRight, 58, 512)]
    [InlineData(809, MessageKind.Narration, 248, 536)]
    [InlineData(815, MessageKind.CgBar, 0, 588)]
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

    /// <summary>Option → Text Speed…'s preview: a 121 with the lines where <c>WAO_SUB</c> prints them, one glyph for each character but the spaces.</summary>
    [Fact]
    public void A_sample_is_a_121_printed_as_WAO_SUB_prints()
    {
        string[] lines = ["I believe that we should first", "prepare our cities for their"];
        var window = MessageWindow.Sample(lines);

        Assert.Equal(MessageKind.SpeechRight, window.Kind);
        Assert.Equal(lines, window.Lines);
        Assert.Equal(lines.Sum(l => l.Count(c => c != ' ')), window.Glyphs.Count);
        Assert.Equal(new MessageGlyph(86, 534, 48, 'I', null), window.Glyphs[0]);
        Assert.Equal((86, 534 + 52), (window.Glyphs.First(g => g.C == 'p').X, window.Glyphs.First(g => g.C == 'p').Y));
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

    /// <summary>
    /// 131: the speaker's name of <c>Name「…」</c> is split off, the closing bracket dropped and the line under it moved left by
    /// the name's cells, so both start at the bar's text edge (<c>Gｶﾅﾐ.ADV:330-334</c>); the text's own colour (190) is the bar's.
    /// </summary>
    [Fact]
    public void The_bar_splits_off_the_speakers_name()
    {
        var bar = Bar("Rance「Right now, we aren’t a king and a ninja.\n      We’re an interrogator and a prisoner. So...」");
        Assert.Equal((MessageKind.CgBar, "Rance", false, false), (bar.Kind, bar.Speaker, bar.Thinks, bar.Unclosed));
        Assert.Equal(["Right now, we aren’t a king and a ninja.", "We’re an interrogator and a prisoner. So..."], bar.Lines);
        Assert.Equal(new MessageGlyph(16, 614, 48, 'R', null), bar.Glyphs[0]);
        Assert.Equal((16, 614 + 52), (bar.Glyphs.First(g => g.C == 'W').X, bar.Glyphs.First(g => g.C == 'W').Y));
        Assert.DoesNotContain(bar.Glyphs, g => g.C is '「' or '」');
        Assert.Equal('.', bar.Glyphs[^1].C);
    }

    /// <summary>A thought (<c>Name（…）</c>, <c>Iﾄｳｿﾞｸ.ADV:1755-1759</c>), names of several words, and the lines that are no speech.</summary>
    [Fact]
    public void The_bar_knows_thoughts_names_and_narration()
    {
        var thought = Bar("Rance（They’re so persistent!\n      Whether it’s morning or night or I’m taking a\n      shit, I don’t get a break!）");
        Assert.Equal(("Rance", true, false), (thought.Speaker, thought.Thinks, thought.Unclosed));
        Assert.Equal(["They’re so persistent!", "Whether it’s morning or night or I’m taking a", "shit, I don’t get a break!"], thought.Lines);

        Assert.Equal("Pink-Haired Girl", Bar("Pink-Haired Girl「Welcome!」").Speaker);
        Assert.Equal("PG-7", Bar("PG-7「Yes.」").Speaker);
        Assert.Equal("???", Bar("???「Who's there?」").Speaker);

        var narration = Bar("Rance was on the outskirts of Golak.\nHe was alone.");
        Assert.Equal(("", false), (narration.Speaker, narration.Unclosed));
        Assert.Equal(["Rance was on the outskirts of Golak.", "He was alone."], narration.Lines);
        Assert.Equal(["  ＜ptsssh＞"], Bar("  ＜ptsssh＞").Lines);                // a sound keeps its indent
        Assert.Equal("", Bar("I’ll service（it）").Speaker);                    // not a name
    }

    /// <summary>
    /// A speech not closed at the window's end goes on in the next window when its first line is indented to the speech's
    /// text (<c>Gﾗﾌｧﾘｱ.ADV:652-657</c>); a narration after it does not.
    /// </summary>
    [Fact]
    public void A_speech_left_open_goes_on_in_the_next_bar()
    {
        var open = Bar("Rance「Uuuh?");
        Assert.Equal(("Rance", true), (open.Speaker, open.Unclosed));
        Assert.Equal(["Uuuh?"], open.Lines);

        var next = Bar("      ...Are you asking me to fire Rick?」", open);
        Assert.Equal(("Rance", false), (next.Speaker, next.Unclosed));
        Assert.Equal(["...Are you asking me to fire Rick?"], next.Lines);
        Assert.Equal(16, next.Glyphs[0].X);

        Assert.Equal("", Bar("      ...Are you asking me to fire Rick?」").Speaker);  // nothing open before
        Assert.Equal("", Bar("Rance ran off.", open).Speaker);                 // not indented: narration
    }

    /// <summary>
    /// The presenter hands 131 over (not under a picker, which prints its own text there: <c>FIGT_B.ADV:77-80</c>), and a
    /// speech left open at a key wait goes on in the next text.
    /// </summary>
    [Fact]
    public void The_presenter_hands_over_the_bar()
    {
        var (presenter, state) = Presenter();
        presenter.CoverMessages = true;
        Open(presenter, state, 131, 31, 815);
        state.Strings[1] = "Rance「Uuuh?";
        presenter.Show(new PrintStringEvent(1));
        Assert.Equal(("Rance", MessageKind.CgBar), (presenter.MessageWindow!.Speaker, presenter.MessageWindow.Kind));

        presenter.Show(new WindowEvent(11, 0));                                 // its key wait, then the next text
        presenter.AfterKey();
        state.Strings[1] = "      ...Are you asking me to fire Rick?」";
        presenter.Show(new PrintStringEvent(1));
        Assert.Equal("Rance", presenter.MessageWindow!.Speaker);
        Assert.Equal(["...Are you asking me to fire Rick?"], presenter.MessageWindow.Lines);

        presenter.Show(new PickerScreenEvent(PickerKind.Deployment, Sys3Int.Zero, true));
        Assert.Null(presenter.MessageWindow);
    }

    /// <summary>
    /// After <c>WAO_SUB</c> 0 the full-screen window stays on the screen (<c>WAO_SUB.ADV:267-270</c>): ours too, until something
    /// is drawn over its place or a window opens; the debug mask stays on the scene left there until the whole screen is drawn.
    /// </summary>
    [Fact]
    public void A_closed_bar_stays_until_drawn_over()
    {
        var (presenter, state) = Presenter();
        presenter.CoverMessages = true;
        presenter.MaskEventCgs = true;
        Open(presenter, state, 131, 31, 815);
        state.Strings[1] = "Rance「God damn iiiiiiiiiiit!!!!」";
        presenter.Show(new PrintStringEvent(1));
        Close(presenter, state);
        Assert.Equal("Rance", presenter.MessageWindow?.Speaker);
        Assert.True(Masked(presenter));

        presenter.Show(new EngineEvent("CF", [Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)640, (Sys3Int)100, (Sys3Int)15]));
        Assert.NotNull(presenter.MessageWindow);                                // above the bar
        Assert.True(Masked(presenter));
        presenter.Show(new EngineEvent("CF", [Sys3Int.Zero, (Sys3Int)380, (Sys3Int)20, (Sys3Int)20, (Sys3Int)15]));
        Assert.Null(presenter.MessageWindow);                                   // over it

        Open(presenter, state, 131, 31, 815);
        presenter.Show(new PrintStringEvent(1));
        Close(presenter, state);
        Open(presenter, state, 121, 21, 810);                                   // a window opens
        Assert.Equal(MessageKind.SpeechRight, presenter.MessageWindow?.Kind);
        Close(presenter, state);
        Assert.Null(presenter.MessageWindow);
        Assert.True(Masked(presenter));
        presenter.Show(new EngineEvent("CF", [Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        Assert.False(Masked(presenter));                                        // the whole screen drawn over
    }

    /// <summary>In a battle (step 5), under All Data or the week report the presenter keeps drawing.</summary>
    [Fact]
    public void The_presenter_keeps_the_windows_it_does_not_hand_over()
    {
        var (presenter, state) = Presenter();
        presenter.CoverMessages = true;
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

    /// <summary><c>WAO_SUB</c> 0: the event first, then the state.</summary>
    private static void Close(GamePresenter presenter, GameState state)
    {
        presenter.Show(new WindowEvent(0, 0));
        state.Ui_WinType = 0;
        state.Ui_WinFrameCg = 0;
        presenter.Invalidate();
    }

    /// <summary>131's text in its text box (<c>GamePresenter.Window</c>: (8, 305), size 24, colour 190), as the model reads it.</summary>
    private static MessageWindow Bar(string text, MessageWindow? continuing = null)
    {
        var box = new TextBox((8, 305, 626, 140));
        box.Print(text, 24, MessageWindow.CgBarColor);
        return MessageWindow.Of(MessageWindow.CgBarCg, box, NoColour, continuing)!;
    }

    /// <summary>Whether the debug mask's grey is on the screen above the bar.</summary>
    private static bool Masked(GamePresenter presenter)
    {
        presenter.Invalidate();
        presenter.Compose();
        return presenter.Screen.PixelAt(320, 100) == (96, 96, 96, 255);
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
