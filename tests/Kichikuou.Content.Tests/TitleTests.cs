using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The title, <c>ｽﾀMENU</c> (milestone 6 step 2; docs/strategy/survey-w4.md §3), run from the player's
/// files after the boot (<see cref="EndGame"/> without the new game) with a scripted mouse: the logo,
/// the buttons the clears unlock and the rolls, New game, Load, Alice's Mansion, <c>Gｻｲﾄｳ</c>, the
/// requests slip, a button's description and the debug starts. A run ends where the title polls
/// the mouse again with every gesture done (<see cref="PointerIdleException"/>) or where it jumps to
/// <c>MAIN</c>. Every key wait and pause is answered at once; every draw is 1 unless queued.
/// Skipped without the decompile.
/// </summary>
public class TitleTests
{
    private const string Title = "ｽﾀMENU.ADV";
    private const int Clears = 79;

    /// <summary>
    /// The buttons' boxes (<c>ｽﾀMENU.ADV:805</c>, x, y, width, height), loaded for 0-10 only (STR-13);
    /// a click goes to the middle of one.
    /// </summary>
    private static readonly (int X, int Y, int W, int H)[] Boxes =
    [
        (0, 0, 240, 154), (248, 0, 187, 127), (434, 2, 202, 100), (545, 105, 92, 122), (0, 272, 96, 117),
        (95, 270, 151, 130), (261, 263, 173, 137), (463, 238, 177, 162), (390, 120, 144, 30), (332, 240, 174, 20),
        (74, 136, 470, 100),
    ];

    private const int FortuneCheck = 0, MusicRoom = 1, AliceMansion = 2, Requests = 3, Saitou = 4, Load = 5, NewGame = 6, Gallery = 7;

    /// <summary>
    /// A first launch (no clears): the logo (its waits cancelable, then not again, <c>ﾌﾟﾛﾛｰｸﾞ.ADV:1, 151</c>),
    /// the title's CGs and music, the boxes of buttons 0-10 (not 11, STR-13), and the buttons a first
    /// launch has: Alice's Mansion, Load, New game, 8 drawn, 9 and 10 clickable but not drawn; the
    /// gallery's CG 240 counted as seen; no roll without clears. The title then waits for the mouse.
    /// </summary>
    [Fact]
    public async Task A_first_launch_shows_the_logo_and_the_buttons_it_has()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        Assert.False(game.Script.PauseCancelable);                      // ｽﾀｰﾄ.ADV:858 Y 3, 10000 at the boot

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        var s = game.State;
        Assert.Equal([0, 0, 1, 0, 0, 1, 1, 0, 1, 2, 2], Enumerable.Range(0, 11).Select(i => (int)s.MenuItems[i].MenuItem.Value));
        Assert.Equal(1, s.CgSeen[240].CgSeen.Value);
        for (int i = 0; i < Boxes.Length; i++)
        {
            var (x, y, w, h) = Boxes[i];
            var b = s.Buttons[i];
            Assert.Equal((x, y, w, h, x + w, y + h), ((int)b.X1.Value, (int)b.Y1.Value, (int)b.W.Value, (int)b.H.Value, (int)b.X2.Value, (int)b.Y2.Value));
        }
        Assert.Equal(0, s.Buttons[11].X2.Value);                        // STR-13: never loaded
        Assert.Empty(game.Random.Draws);
        Assert.Contains(game.View.Events, e => IsCg(e, 1700));          // the logo
        Assert.Contains(game.View.Events, e => e is EngineEvent { Command: "CE" });
        foreach (int cg in new[] { 301, 302, 303 })
            Assert.Contains(game.View.Events, e => IsCg(e, cg));
        Assert.Contains(game.View.Events, e => e is MusicEvent { Track.Value: 27 });
        Assert.True(game.Input.Reads > 1000);
        Assert.False(game.Script.PauseCancelable);                      // ﾌﾟﾛﾛｰｸﾞ.ADV:151
    }

    /// <summary>
    /// With a clear (page 4's clear count) Fortune Check, the music room and the gallery are
    /// unlocked; the requests slip and <c>Gｻｲﾄｳ</c> only on a roll of 10 of 10 and 20 of 20: the
    /// script's <c>{RetVal - 9:</c> and <c>{RetVal - 19:</c> are 0 for every lower draw only because the
    /// engine's <c>-</c> stops at 0 (ENG-01, STR-34).
    /// </summary>
    [Theory]
    [InlineData(10, 20, 1, 1)]
    [InlineData(9, 19, 0, 0)]
    [InlineData(1, 8, 0, 0)]
    [InlineData(10, 1, 1, 0)]
    public async Task Clears_unlock_buttons_and_the_rolls_pass_only_at_their_top(int roll3, int roll4, int requests, int saitou)
    {
        var game = await EndGame.BootAsync(newGame: false, random: new QueuedRandom(roll3, roll4));
        if (game is null)
            return;
        game.State.Fates[Clears].Fate = 1;

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        var s = game.State;
        Assert.Equal([10, 20], game.Random.Draws);
        Assert.Equal([1, 1, 1, requests, saitou, 1, 1, 1, 1, 2, 2], Enumerable.Range(0, 11).Select(i => (int)s.MenuItems[i].MenuItem.Value));
    }

    /// <summary>
    /// New game: the release of the click over "Start" (button 6) runs <c>L_005ef</c> and
    /// <c>L_00670</c>: the camera, the map, week 8 with 50 gold, the prologue scene, then the jump
    /// to <c>MAIN</c> (<c>:291</c>).
    /// </summary>
    [Fact]
    public async Task New_game_plays_the_prologue_scene_and_jumps_to_MAIN()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        Click(game, NewGame);
        var s = game.State;
        (int, int, int, int, int)? atPrologue = null;
        game.OnCall = file =>
        {
            if (file == "Iﾌﾟﾛﾛｰｸﾞ.ADV")
                atPrologue = (s.Week.Value, s.StoryStage.Value, s.Gold.Value, s.MapCamX.Value, s.MapCamY.Value);
        };

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal("MAIN.ADV", jump.File);
        Assert.Equal((8, 0, 50, 1000, 400), atPrologue);
        Assert.Contains("Iﾌﾟﾛﾛｰｸﾞ.ADV 0", game.Calls);
        Assert.Contains(game.View.Events, e => e is MapEvent { Mode.Value: 0 });   // :285-286 the map shown
    }

    /// <summary>
    /// Load (button 5): the save screen's load list (<c>ﾛｰﾄﾞｾｰﾌﾞ</c> with <c>RetVal</c> 0); backing out
    /// puts the pointer back where it was (<c>IZ</c>, <c>:242</c>) and the title waits again; a slot
    /// chosen loads (<c>LD</c>) and leaves the title for the loaded game.
    /// </summary>
    [Fact]
    public async Task Load_backs_out_to_the_title_or_loads_the_slot()
    {
        var saved = await EndGame.BootAsync();
        if (saved is null)
            return;
        int week = saved.State.Week.Value;
        saved.Script.SaveResume = new ScriptRef("MAIN.ADV", 20);
        saved.Script.SaveAll(1);
        var game = (await EndGame.BootAsync(newGame: false, saves: saved.Script.Saves))!;
        Click(game, Load);
        game.Input.Menu.Enqueue(0);                                     // backs out of the list

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Contains("ﾛｰﾄﾞｾｰﾌﾞ.ADV", Files(game));
        Assert.Contains(game.View.Events, e => e is EngineEvent { Command: "IZ" });

        Click(game, Load);
        game.Input.Menu.Enqueue(1);                                     // "Auto": slot 1

        var loaded = await Assert.ThrowsAsync<GameLoadedException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal(new ScriptRef("MAIN.ADV", 20), loaded.Resume);
        Assert.Equal(week, game.State.Week.Value);
    }

    /// <summary>Alice's Mansion (button 2, always there): <c>Iｱﾘｽ</c> runs from the files; "Return to Game" leads back to the title, drawn again.</summary>
    [Fact]
    public async Task Alice_s_Mansion_runs_and_returns_to_the_title()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        Click(game, AliceMansion);
        game.Input.Menu.Enqueue("Iｱﾘｽ.ADV", 60);                        // Return to Game

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Contains(game.Calls, c => c.StartsWith("Iｱﾘｽ.ADV", StringComparison.Ordinal));
        Assert.Equal(2, game.View.Events.Count(e => IsCg(e, 301)));     // the title drawn again (L_00035)
        Assert.Contains(game.View.Events, e => e is MusicEvent { Track.Value: 28 });
        Assert.Equal(27, game.View.Events.OfType<MusicEvent>().Last().Track.Value);
    }

    /// <summary>
    /// <c>Gｻｲﾄｳ</c> 1 (button 4, unlocked by its roll of 20): Saitou's scene runs from the files and
    /// leaves <c>F_Saitou_State</c> 1 for the next game (STR-36); the title is drawn again and rolls again.
    /// </summary>
    [Fact]
    public async Task Saitou_runs_from_the_title_and_leaves_her_state_for_the_game()
    {
        var game = await EndGame.BootAsync(newGame: false, random: new QueuedRandom(1, 20) { DefaultToOne = true });
        if (game is null)
            return;
        game.State.Fates[Clears].Fate = 1;
        Click(game, Saitou);
        game.Input.Menu.Enqueue("Gｻｲﾄｳ.ADV", 87);                       // No More

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Contains("Gｻｲﾄｳ.ADV 1", game.Calls);
        Assert.Equal(1, game.State.F_Saitou_State.Value);
        Assert.Equal([10, 20, 10, 20], game.Random.Draws);
    }

    /// <summary>
    /// The requests slip (button 3, unlocked by its roll of 10): <c>Y 4, 100</c> chooses between the
    /// first 5 requests and all 15 (the word table <c>[15]</c>, <c>#L_00fb3, 1</c>), the pick skips that
    /// many "／／"-separated entries of the strings <c>#L_00fb3, 2</c> names (<c>F1</c>, <c>MC</c>), and its
    /// lines print 30 dots apart from (48, 240); a pick equal to the last one is drawn again.
    /// </summary>
    [Fact]
    public async Task The_requests_slip_prints_one_request_and_never_the_same_twice_in_a_row()
    {
        // The rolls (10: the slip, 1: no Saitou); the first slip 64 → of 5: 2; the second 64 → of 5:
        // 2 again, drawn again, 65 → of 15: 3.
        var game = await EndGame.BootAsync(newGame: false, random: new QueuedRandom(10, 1, 64, 2, 64, 2, 65, 3));
        if (game is null)
            return;
        game.State.Fates[Clears].Fate = 1;
        var lines = new List<(int Y, string Text)>();
        int y = 0;
        game.View.OnShow = e =>
        {
            if (e is EngineEvent { Command: "T" } t)
                y = t.Args[1].Value;
            if (e is MessageEvent { Text: { File: Title, Line: 226 } })
                lines.Add((y, game.State.Strings[1]));
        };
        Click(game, Requests);
        Click(game, Requests);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal([10, 20, 100, 5, 100, 5, 100, 15], game.Random.Draws);
        Assert.Equal(
            [(240, "Don’t sell"), (270, "used games"), (240, "Don’t toss"), (270, "cigarettes"), (300, "in the road")],
            lines);
        Assert.Equal(3, game.State.Ui_StartMenuLastPick.Value);
        Assert.Equal("／／", game.State.Strings[2]);
    }

    /// <summary>A right click on a button shows its description in window 113 until a key after the release (<c>L_00c80</c>); nothing starts.</summary>
    [Fact]
    public async Task A_right_click_shows_the_button_s_description()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        var (x, y, w, h) = Boxes[NewGame];
        game.Input.Pointer.RightClick(x + w / 2, y + h / 2);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        int open = game.View.Events.FindIndex(e => e is WindowEvent { Op.Value: 113 });
        int text = game.View.Events.FindIndex(e => e is MessageEvent { Text: { File: Title, Line: 645 } });
        int close = game.View.Events.FindLastIndex(e => e is WindowEvent { Op.Value: 0 });
        Assert.True(open >= 0 && open < text && text < close, $"open {open}, text {text}, close {close}");
        Assert.Equal([Title, "ﾌﾟﾛﾛｰｸﾞ.ADV"], Files(game));
        Assert.Equal(0, game.State.Ui_WinType.Value);
    }

    /// <summary>
    /// The debug starts (<c>DebugMode</c> 1, Alice's secret, EVT-09): "Start" opens From 1-4
    /// (<c>:249-266</c>); a cancel puts the pointer back and the title waits again; From 4 sets week
    /// 77 at story stage 2 (STR-38) with the world <c>:360-546</c> builds — the provinces up to Temple AL
    /// HQ (77) Leazas's, seven Kayblis's, Nikkou (item 2) held by Kentarou (unit 31) — and jumps to
    /// <c>MAIN</c> without a scene.
    /// </summary>
    [Fact]
    public async Task The_debug_menu_cancels_or_starts_from_4()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        var s = game.State;
        s.DebugMode = 1;
        Click(game, NewGame);
        game.Input.Menu.Enqueue(0);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal([new ScriptRef(Title, 251), new(Title, 255), new(Title, 259), new(Title, 263)], game.Input.Menus[0]);
        Assert.Contains(game.View.Events, e => e is EngineEvent { Command: "IZ" });

        Click(game, NewGame);
        game.Input.Menu.Enqueue(Title, 263);                            // From 4

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal("MAIN.ADV", jump.File);
        Assert.Equal((77, 2, 60000), ((int)s.Week.Value, (int)s.StoryStage.Value, (int)s.Gold.Value));
        Assert.All(Enumerable.Range(2, 76), p => Assert.Equal(1, s.Provs[p].Owner.Value));
        Assert.All(new[] { 78, 79, 80, 81, 82, 83, 89 }, p => Assert.Equal(8, s.Provs[p].Owner.Value));
        Assert.Equal((31, 2), ((int)s.Items[2].Holder.Value, (int)s.Units[31].Item.Value));   // his own since SET_CHR.ADV:1204
        Assert.Equal(2, s.Units[32].Status.Value);
        Assert.All(Files(game), f => Assert.Contains(f, new[] { Title, "ﾌﾟﾛﾛｰｸﾞ.ADV" }));
    }

    /// <summary>
    /// From 2 (<c>:292-303</c>): week 12 at story stage 1, then <c>Iﾊﾝﾗﾝ</c> with the <c>Ev_Entry</c>
    /// the title left (below 300: its <c>:10-</c>, a week on and 685 gold), then <c>MAIN</c>; From 3
    /// (<c>:304-359</c>): week 16 at stage 2 with 30000 gold, Rance's unit as "Leazas Knights" with the
    /// L. Holy Sword (item 6), then <c>Iｼﾚｲﾌﾞ</c> 2.
    /// </summary>
    [Theory]
    [InlineData(255, "Iﾊﾝﾗﾝ.ADV")]
    [InlineData(259, "Iｼﾚｲﾌﾞ.ADV")]
    public async Task From_2_and_3_play_their_scene_and_jump_to_MAIN(int line, string scene)
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        var s = game.State;
        s.DebugMode = 1;
        (int Week, int Stage, int Gold, int Item)? atScene = null;
        game.OnCall = file =>
        {
            if (file == scene)
                atScene = (s.Week.Value, s.StoryStage.Value, s.Gold.Value, s.Units[1].Item.Value);
        };
        Click(game, NewGame);
        game.Input.Menu.Enqueue(Title, line);
        if (line == 259)
            game.Input.Menu.Enqueue("Gｴｸｽ.ADV", 2452);                  // Iｼﾚｲﾌﾞ 2's Ex: Join Me

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal("MAIN.ADV", jump.File);
        if (line == 255)
        {
            Assert.Equal(12, atScene?.Week);
            Assert.Equal((13, 1, 685), ((int)s.Week.Value, (int)s.StoryStage.Value, (int)s.Gold.Value));
        }
        else
        {
            Assert.Equal((16, 2, 30000, 6), atScene);
            Assert.Contains("Iｼﾚｲﾌﾞ.ADV 2", game.Calls);
        }
    }

    /// <summary>
    /// The gallery and the music room (buttons 7 and 1, with a clear) are not ported (decision 19):
    /// their files go to the unported files' hooks and the title is drawn again.
    /// </summary>
    [Fact]
    public async Task The_gallery_and_the_music_room_go_to_the_unported_files()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        game.State.Fates[Clears].Fate = 1;
        Click(game, Gallery);
        Click(game, MusicRoom);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Equal(["CG.ADV", "MMODE.ADV"], game.Unported.Calls.Select(c => c.File));
        Assert.Equal(3, game.View.Events.Count(e => IsCg(e, 301)));
    }

    /// <summary>The files called, in order (the title itself and its logo included).</summary>
    private static List<string> Files(EndGame game) => [.. game.Calls.Select(c => c[..c.LastIndexOf(' ')])];

    private static void Click(EndGame game, int button)
    {
        var (x, y, w, h) = Boxes[button];
        game.Input.Pointer.Click(x + w / 2, y + h / 2);
    }

    private static bool IsCg(ScriptEvent e, int cg) =>
        e is EngineEvent { Command: "G" } g && g.Args[0] == (Sys3Int)cg;
}
