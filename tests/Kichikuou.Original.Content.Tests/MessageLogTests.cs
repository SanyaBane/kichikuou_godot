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
/// The message log (docs/ui/messages.md, §5 step 6): <see cref="MessageLog"/>'s pages from the windows' key waits and the menus'
/// answers, its ruled lines as mock-up L3a lays them out, and <see cref="GamePresenter"/> filling it.
/// </summary>
public class MessageLogTests
{
    private static readonly Func<int, (byte, byte, byte)> NoColour = _ => (0, 0, 0);

    /// <summary>A window's kind is the page's; 111/112 speak for the left portrait, 121/122 for the right one, 131 names its own.</summary>
    [Fact]
    public void A_page_is_its_windows_kind_and_its_portraits_speaker()
    {
        static string Name(int cg) => cg switch { 1 => "RANCE", 6 => "MARIS AMARYLLIS", _ => "" };
        LogPage Page(int frame, string text, int left = 1, int right = 6) =>
            MessageLog.PageOf(Window(frame, text), left, right, Name)!;

        Assert.Equal(new LogPage(LogKind.Speech, "MARIS AMARYLLIS", ["King Rance.", "May I interrupt?"]),
            Page(810, "King Rance.\nMay I interrupt?"), new PageComparer());
        Assert.Equal(("RANCE", LogKind.Speech), Speaker(Page(807, "Yeah, what is it?")));
        Assert.Equal(("", LogKind.Speech), Speaker(Page(807, "Yeah, what is it?", left: 0)));   // no portrait up
        Assert.Equal(("RANCE", LogKind.Thought), Speaker(Page(808, "Mhm, I see.")));
        Assert.Equal(("MARIS AMARYLLIS", LogKind.Thought), Speaker(Page(811, "...")));
        Assert.Equal(("", LogKind.Narration), Speaker(Page(809, "Maris showed up.")));

        var bar = new TextBox((8, 305, 626, 140));
        bar.Print("Sill（Ah... My clothes...）", 24, MessageWindow.CgBarColor);
        var thought = MessageLog.PageOf(MessageWindow.Of(MessageWindow.CgBarCg, bar, NoColour)!, 1, 6, Name)!;
        Assert.Equal(("Sill", LogKind.Thought), Speaker(thought));
        Assert.Equal(["Ah... My clothes..."], thought.Lines);
    }

    /// <summary>A place's title is one page of its two lines, trimmed; an empty window none.</summary>
    [Fact]
    public void A_places_title_and_an_empty_window()
    {
        var box = new TextBox((140, 278, 376, 140));
        box.Print("　　　Leazas Castle", MessageWindow.TitleSize, MessageWindow.PlainColor);
        box.NewLine(24);
        box.Print("　　　　　Audience Room", 24, MessageWindow.PlainColor);
        var page = MessageLog.PageOf(MessageWindow.Of(809, box, NoColour)!, 0, 0, _ => "")!;
        Assert.Equal(LogKind.Place, page.Kind);
        Assert.Equal(["Leazas Castle", "Audience Room"], page.Lines);

        Assert.Null(MessageLog.PageOf(MessageWindow.Of(809, new TextBox((140, 278, 376, 140)), NoColour)!, 0, 0, _ => ""));
    }

    /// <summary>
    /// L3a's lines: close, the name at the first line of a run of one speaker and kind, a place's title one heading, of a choice
    /// the chosen item alone under "CHOICE" (owner, 2026-10-19).
    /// </summary>
    [Fact]
    public void The_lines_name_a_run_once()
    {
        var log = new MessageLog();
        log.Add(new LogPage(LogKind.Place, "", ["Leazas Castle", "Audience Room"]));
        log.Add(new LogPage(LogKind.Narration, "", ["Rance was lying on the carpet", "at his side."]));
        log.Add(new LogPage(LogKind.Speech, "MARIS", ["Thank you very much."]));
        log.Add(new LogPage(LogKind.Speech, "MARIS", ["Sir Ex is an excellent", "commander."]));
        log.Add(new LogPage(LogKind.Thought, "MARIS", ["Hmm."]));
        log.Add(new LogPage(LogKind.Speech, "RANCE", ["Hmmmmmm."]));
        log.Add(new LogPage(LogKind.Choice, "", ["Keep Donating", "Stop Donating"], 1));

        Assert.Equal(
        [
            new LogLine(LogKind.Place, "", "Leazas Castle — Audience Room"),
            new LogLine(LogKind.Narration, "", "Rance was lying on the carpet"),
            new LogLine(LogKind.Narration, "", "at his side."),
            new LogLine(LogKind.Speech, "MARIS", "Thank you very much."),
            new LogLine(LogKind.Speech, "", "Sir Ex is an excellent"),
            new LogLine(LogKind.Speech, "", "commander."),
            new LogLine(LogKind.Thought, "MARIS", "Hmm."),
            new LogLine(LogKind.Speech, "RANCE", "Hmmmmmm."),
            new LogLine(LogKind.Choice, MessageLog.ChoiceLabel, "Keep Donating", Chosen: true),
        ], log.Lines());
    }

    /// <summary>The log keeps the last <see cref="MessageLog.Capacity"/> pages.</summary>
    [Fact]
    public void The_oldest_pages_go_first()
    {
        var log = new MessageLog();
        for (int i = 0; i < MessageLog.Capacity + 3; i++)
            log.Add(new LogPage(LogKind.Narration, "", [$"page {i}"]));
        Assert.Equal(MessageLog.Capacity, log.Pages.Count);
        Assert.Equal("page 3", log.Pages[0].Lines[0]);
    }

    /// <summary>
    /// The presenter logs each key wait of a window, in either interface, with its portrait's name; several pages of one
    /// window are pages of their own; the play's narration (<c>MAIN</c>'s revenue, <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s load report) and a
    /// battle's windows are not logged (owner, 2026-10-19).
    /// </summary>
    [Fact]
    public void The_presenter_logs_each_key_wait()
    {
        var (presenter, state) = Presenter();
        state.Ui_PortraitR = 6;
        Open(presenter, state, 121, 21, 810);
        Print(presenter, state, "Thank you very much.");
        Wait(presenter);
        Print(presenter, state, "Sir Ex is an excellent");
        Wait(presenter);
        Close(presenter, state);

        Assert.Equal([("MARIS AMARYLLIS", "Thank you very much."), ("MARIS AMARYLLIS", "Sir Ex is an excellent")],
            presenter.Log.Pages.Select(p => (p.Speaker, p.Lines[0])));

        presenter.CoverMessages = true;                                         // our windows: the same pages
        Open(presenter, state, 113, 13, 809);
        presenter.Show(new MessageEvent(new ScriptRef("Gﾏﾘｽ.ADV", 430)));     // an event scene's narration
        Wait(presenter);
        Assert.Equal(LogKind.Narration, presenter.Log.Pages[^1].Kind);
        Close(presenter, state);

        foreach (var file in new[] { "MAIN.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV" })            // the play's narration: not logged
        {
            Open(presenter, state, 113, 13, 809);
            presenter.Show(new ValueMessageEvent(new ScriptRef(file, 100), [(Sys3Int)20200]));
            Wait(presenter);
            Close(presenter, state);
        }
        Open(presenter, state, 113, 13, 809);
        Print(presenter, state, "Revenue");                                     // narration of no known file: not logged
        Wait(presenter);

        presenter.Show(new BattleScreenEvent("FIGT.ADV", true));                // a battle's windows: not logged
        Print(presenter, state, "Charge!");
        Wait(presenter);
        Assert.Equal(3, presenter.Log.Pages.Count);
    }

    /// <summary>
    /// The windows the UI answers unseen (<see cref="GamePresenter.AnswersUnseen"/>: an order of our Subordinates screen, its
    /// <c>Cﾌﾞｶ</c> dialogue) are not logged; once the order is over they are again (owner, 2026-10-23).
    /// </summary>
    [Fact]
    public void Windows_answered_unseen_are_not_logged()
    {
        var (presenter, state) = Presenter();
        bool unseen = true;
        presenter.AnswersUnseen = () => unseen;
        state.Ui_PortraitR = 6;
        Open(presenter, state, 121, 21, 810);
        Print(presenter, state, "Understood.");
        Wait(presenter);
        Close(presenter, state);
        Assert.Empty(presenter.Log.Pages);

        unseen = false;
        Open(presenter, state, 121, 21, 810);
        Print(presenter, state, "Understood.");
        Wait(presenter);
        Close(presenter, state);
        Assert.Equal("Understood.", presenter.Log.Pages.Single().Lines[0]);
    }

    /// <summary>
    /// A dialogue's question logs its answer, a cancelled one not: an event scene's menu (<c>GAL</c>), or a play file's asked
    /// after a speech, its window closed (<c>Cﾃｲﾄ</c>: Maris's "You summoned me, King Rance?", <c>WAO_SUB</c> 0, the buildings).
    /// The play's menus never (owner, 2026-10-19): <c>COMMAND</c>'s opens its status window after the speech, <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s
    /// comes under its narration.
    /// </summary>
    [Fact]
    public void A_dialogues_question_logs_its_answer()
    {
        var (presenter, state) = Presenter();
        presenter.OpenMenu(Items("GAL.ADV", 990, 991));
        var items = presenter.Menu!.Items.Select(item => item.Text).ToArray();
        presenter.MenuAnswered(1);
        presenter.CloseMenu();
        Assert.Equal(new LogPage(LogKind.Choice, "", items, 1), presenter.Log.Pages.Single(), new PageComparer());

        presenter.OpenMenu(Items("GAL.ADV", 990, 991));
        presenter.MenuAnswered(0);                                              // cancelled
        presenter.CloseMenu();
        Assert.Single(presenter.Log.Pages);

        state.Ui_PortraitR = 6;
        Open(presenter, state, 121, 21, 810);
        Print(presenter, state, "You summoned me, King Rance?");
        Wait(presenter);
        Close(presenter, state);
        Ask(presenter, "Cﾃｲﾄ.ADV", 2);                                         // after the speech: logged
        Assert.Equal((LogKind.Choice, 2), (presenter.Log.Pages[^1].Kind, presenter.Log.Pages[^1].Chosen));

        Open(presenter, state, 123, 23, 809);                                   // COMMAND's status window, no key wait
        Ask(presenter, "COMMAND.ADV", 6);
        Close(presenter, state);
        Open(presenter, state, 113, 13, 809);
        presenter.Show(new MessageEvent(new ScriptRef("ﾛｰﾄﾞｾｰﾌﾞ.ADV", 50)));
        Wait(presenter);
        Ask(presenter, "ﾛｰﾄﾞｾｰﾌﾞ.ADV", 7);
        Assert.Equal(3, presenter.Log.Pages.Count);
    }

    /// <summary>
    /// The port's own words (ours, owner's decision 30): a menu of its items (Kanami's choice in <c>Gｺｰﾄﾞ</c>) shows their texts as
    /// they are, nothing unresolved, and its answer is logged as an event scene's; a narration it prints in a scene
    /// (Kentarou's hint, a <see cref="PortTextEvent"/> standing at <c>Gｹﾝﾀﾛｳ:610</c>) is logged as the scene's, one at no
    /// place (a save's text) is not.
    /// </summary>
    [Fact]
    public void The_ports_own_menu_and_narration_are_shown_and_logged()
    {
        var (presenter, state) = Presenter();
        presenter.OpenMenu([SceneMenuItem.OfPort(new ScriptRef("Gｺｰﾄﾞ.ADV", 1460), "Stop Code"),
            SceneMenuItem.OfPort(new ScriptRef("Gｺｰﾄﾞ.ADV", 1458), "Go with Code")]);
        Assert.Equal(["Stop Code", "Go with Code"], presenter.MenuItems);
        presenter.MenuAnswered(2);
        presenter.CloseMenu();
        Assert.Equal(new LogPage(LogKind.Choice, "", ["Stop Code", "Go with Code"], 2), presenter.Log.Pages.Single(), new PageComparer());
        Assert.Empty(presenter.Unresolved);

        var hint = new ScriptRef("Gｹﾝﾀﾛｳ.ADV", 610);
        Open(presenter, state, 113, 13, 809);
        presenter.Show(new PortTextEvent("Kentarou seems to be catching", hint));
        presenter.Show(new LineBreakEvent());
        presenter.Show(new PortTextEvent("on to the move.", hint));
        Wait(presenter);
        Close(presenter, state);
        Assert.Equal(new LogPage(LogKind.Narration, "", ["Kentarou seems to be catching", "on to the move."]), presenter.Log.Pages[^1],
            new PageComparer());

        Open(presenter, state, 113, 13, 809);
        presenter.Show(new PortTextEvent("　Saving the game as it is now."));
        Wait(presenter);
        Assert.Equal(2, presenter.Log.Pages.Count);
    }

    /// <summary>The event scenes are <c>G*</c> and <c>I*</c>; the commands', pickers', data's, battle's, week's, saves' and title's files are not.</summary>
    [Theory]
    [InlineData("GAL.ADV", true)]
    [InlineData("Gｴｸｽ.ADV", true)]
    [InlineData("Iﾏｼﾞﾝ.ADV", true)]
    [InlineData("COMMAND.ADV", false)]
    [InlineData("MAIN.ADV", false)]
    [InlineData("Cﾃｲﾄ.ADV", false)]
    [InlineData("Sｱｲﾃﾑ.ADV", false)]
    [InlineData("ﾛｰﾄﾞｾｰﾌﾞ.ADV", false)]
    [InlineData("各種DATA.ADV", false)]
    [InlineData("FIGT_B.ADV", false)]
    [InlineData("ｽﾀMENU.ADV", false)]
    public void The_event_scenes_are_the_G_and_I_files(string file, bool scene) => Assert.Equal(scene, MessageLog.IsEventScene(file));

    private static void Wait(GamePresenter presenter)
    {
        presenter.Show(new WindowEvent(11, 0));
        presenter.AfterKey();
    }

    private static void Ask(GamePresenter presenter, string file, int answer)
    {
        presenter.OpenMenu(Items(file, 188, 190, 192, 194, 196, 198, 200));
        presenter.MenuAnswered(answer);
        presenter.CloseMenu();
    }

    private static (string, LogKind) Speaker(LogPage page) => (page.Speaker, page.Kind);

    private static MessageWindow Window(int frame, string text)
    {
        var box = frame is 807 or 808 ? new TextBox((235, 8, 378, 141))
            : frame == 809 ? new TextBox((140, 278, 376, 140))
            : new TextBox((43, 265, 378, 141));
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                box.NewLine(24);
            box.Print(lines[i], 24, MessageWindow.PlainColor);
        }
        return MessageWindow.Of(frame, box, NoColour)!;
    }

    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    private static (GamePresenter, GameState) Presenter()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs())
        {
            PortraitName = cg => cg == 6 ? "MARIS AMARYLLIS" : "",
        };
        var state = new GameState();
        presenter.Bind(state);
        return (presenter, state);
    }

    private static void Print(GamePresenter presenter, GameState state, string text)
    {
        state.Strings[1] = text;
        presenter.Show(new PrintStringEvent(1));
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

    /// <summary>Pages compared by their contents (the record compares its lists by reference).</summary>
    private sealed class PageComparer : IEqualityComparer<LogPage>
    {
        public bool Equals(LogPage? x, LogPage? y) =>
            x is not null && y is not null && x.Kind == y.Kind && x.Speaker == y.Speaker && x.Chosen == y.Chosen
            && x.Lines.SequenceEqual(y.Lines);

        public int GetHashCode(LogPage page) => HashCode.Combine(page.Kind, page.Speaker);
    }
}
