using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The save screen (<see cref="LoadSaveScreen"/>, <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>) as the player meets it, with
/// the literals from the player's files: the weekly autosave, a manual save (a copy of the
/// autosave, STR-11), a load (the chosen slot becomes the autosave and the game leaves for the
/// autosave's call) and the way back ("Load Complete"). Skipped without the decompile.
/// </summary>
public class LoadSaveScreenTests
{
    private const string File = "ﾛｰﾄﾞｾｰﾌﾞ.ADV";
    private static readonly ScriptRef Call = new("MAIN.ADV", 20);

    private sealed record Rig(GameState State, OriginalScriptContext Script, RecordingView View, ScriptedSceneInput Input, MemorySaveStore Saves, LoadSaveScreen Screen);

    private static Rig? NewRig()
    {
        if (!System.IO.File.Exists(Path.Combine(LocalData.ScenarioDirectory, File)))
            return null;
        var state = new GameState();
        var view = new RecordingView();
        var input = new ScriptedSceneInput();
        var saves = new MemorySaveStore(() => new DateTime(2026, 10, 9, 12, 0, 45));
        var script = new OriginalScriptContext(state, new QueuedRandom(), view, input, new RecordingRules { State = state })
        {
            Saves = saves,
            SaveResume = Call,
        };
        state.Ui_WinType = 43;
        return new Rig(state, script, view, input, saves, new LoadSaveScreen(script, new ScenarioScripts(LocalData.ScenarioDirectory)));
    }

    private static async Task AutosaveAsync(Rig rig, int week)
    {
        rig.State.Week = (Sys3Int)week;
        rig.State.RetVal = 3;
        await rig.Screen.RunAsync();
    }

    [Fact]
    public async Task The_autosave_fills_slot_1_and_says_so()
    {
        if (NewRig() is not { } rig)
            return;

        await AutosaveAsync(rig, 13);

        Assert.Equal([1], rig.Saves.Slots);
        var slot = rig.Saves.Read(1)!;
        Assert.Equal(Call, slot.Resume);
        Assert.Equal(13, slot.Variables[21].Value);                     // Tmp0021 := Week
        Assert.Equal(1, slot.Variables[22].Value);                      // Tmp0022 := 1, the slot's number
        Assert.Equal(3, slot.Variables[0].Value);                       // RetVal as QD found it
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text: { File: File, Line: 50 } });                  // "＜ Auto-Save ＞"
        Assert.Contains(rig.View.Events, e => e is ValueMessageEvent { Text.Line: 549 } v && v.Values[0] == 1);      // "Data Number（1）"
        Assert.Contains(rig.View.Events, e => e is ValueMessageEvent { Text.Line: 558 } v && v.Values.SequenceEqual([(Sys3Int)3, (Sys3Int)4, (Sys3Int)1])); // LP 3, Month 4, Week 1
        Assert.Equal(0, rig.State.RetVal.Value);                        // LT found the slot
        Assert.Equal(45, rig.State.Tmp0006.Value);                      // the slot file's second (STR-22)
    }

    [Fact]
    public async Task A_manual_save_copies_the_autosave_and_a_load_comes_back_through_Load_Complete()
    {
        if (NewRig() is not { } rig)
            return;
        await AutosaveAsync(rig, 13);
        rig.State.Week = 15;                                            // later in the game
        rig.State.Gold = 999;

        rig.State.RetVal = 2;                                           // COMMAND's Load/Save
        rig.Input.Choices.Enqueue(2);                                   // Save
        rig.Input.Choices.Enqueue(1);                                   // slot "1" = slot 2
        rig.Input.Choices.Enqueue(0);                                   // leave the list
        rig.Input.Choices.Enqueue(3);                                   // Don't
        await rig.Screen.RunAsync();

        var saved = rig.Saves.Read(2)!;
        Assert.Equal(13, saved.Variables[517].Value);                   // this week's start, not week 15 (STR-11)
        Assert.Equal(2, saved.Variables[22].Value);                     // its number patched in (QP)
        var list = rig.Input.Menus[1];
        Assert.Equal(9, list.Count);
        Assert.Equal(new ScriptRef(File, 504), list[0].Texts.Single().Text);   // empty before the save: dashes
        var relisted = rig.Input.Menus[2];
        Assert.Equal(new ScriptRef(File, 489), relisted[0].Texts.Single().Text);
        Assert.Equal([(Sys3Int)3, (Sys3Int)4, (Sys3Int)1], relisted[0].Texts.Single().Numbers);

        rig.State.RetVal = 2;
        rig.Input.Choices.Enqueue(1);                                   // Load
        rig.Input.Choices.Enqueue(2);                                   // "1" = slot 2 (after "Auto")
        var loaded = await Assert.ThrowsAsync<GameLoadedException>(() => rig.Screen.RunAsync());

        Assert.Equal(2, loaded.Slot);
        Assert.Equal(Call, loaded.Resume);
        Assert.Equal(13, rig.State.Week.Value);
        Assert.NotEqual(999, rig.State.Gold.Value);
        Assert.Equal(2, rig.Saves.Read(1)!.Variables[22].Value);        // slot 2 is now the autosave too (QC 1, 2)
        Assert.Equal(0, rig.State.RetVal.Value);

        rig.View.Events.Clear();
        rig.State.CurrentBgm = 7;
        await rig.Screen.ResumeAsync();

        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text: { File: File, Line: 71 } });                  // "＜ Load Complete ＞"
        Assert.Contains(rig.View.Events, e => e is ValueMessageEvent { Text.Line: 549 } v && v.Values[0] == 2);      // "Data Number（2）"
        Assert.Contains(rig.View.Events, e => e is MusicEvent { Track.Value: 7 });
        Assert.Equal(0, rig.State.RetVal.Value);                        // RetVal := Tmp0022 = 2 picks slot 2 for LT, which sets 0
    }
}
