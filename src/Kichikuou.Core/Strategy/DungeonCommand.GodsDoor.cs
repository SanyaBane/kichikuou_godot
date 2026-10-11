using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

// Cﾒｲｷｭｳ.ADV's God's Door (survey-dungeon.md §2.2, part D2): found in the Ancient Ruins (dungeon
// 30) by Rance holding item 1 or 2 once KD's quest is under way, then its own menu in the dungeon's
// — the four statues placed, Rance's talk, Kouhime's hint, and the way into Iｶﾐ.
public sealed partial class DungeonCommand
{
    /// <summary>
    /// <c>L_04ddc</c> (<c>:3500-3827</c>, called from the dispatch with Rance (unit 1) under the
    /// cursor): God's Door found — the map closed, the ruins' picture (1015), Rance and Maris; then
    /// by Rance's item the scene with portrait 80 (item 1) or 34 (item 2), the door's music and its
    /// picture, and the dungeon's music again.
    /// </summary>
    private async Task GodsDoorFoundAsync()
    {
        State.F_GodsDoor_Found = 1;
        await Script.MapCallAsync(0);
        State.CallArg = 1015;
        await WaoAsync(331);                                            // \L_0766d
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_075c5
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_07635
        await WaoAsync(113);                                            // \L_075a9
        await EngineAsync("ZM", 32);
        await TextAsync(3513);
        await EngineAsync("ZM", 24);
        await TextAsync(3517);
        await WaoAsync(1);                                              // \L_075b7
        await WaoAsync(0);                                              // \L_0759b
        await SpeakAsync(111, 3523);                                    // \L_07571
        await SpeakAsync(121, 3532, 3541);                              // \L_075e1
        await SpeakAsync(111, 3548);
        if (Unit.Item == 1)                                             // :3551-3687
        {
            await SpeakAsync(111, 3554);
            await WaoAsync(322);                                        // \L_07643
            State.CallArg = 80;
            await WaoAsync(321);
            await SpeakAsync(121, 3566);
            await SpeakAsync(111, 3571);
            await SpeakAsync(121, 3578);
            await SpeakAsync(111, 3585);
            await SpeakAsync(121, 3592, 3601);
            await SpeakAsync(123, 3606);                                // \L_07619
            await WaoAsync(312);                                        // \L_075d3
            await WaoAsync(322);
            await WaoAsync(332);                                        // \L_0767b
            await Script.MusicAsync(26);
            await DoorPictureAsync();                                   // \L_0539e
            State.CallArg = 1;
            await WaoAsync(311);
            State.CallArg = 80;
            await WaoAsync(321);
            await DoorHeaderAsync(3623);
            await SpeakAsync(111, 3633);
            await SpeakAsync(121, 3638);
            await SpeakAsync(121, 3643);
            await SpeakAsync(111, 3652);
            await SpeakAsync(121, 3659, 3664, 3671);
            await SpeakAsync(111, 3680);
            await Script.MusicAsync(21);
            return;
        }
        if (Unit.Item == 2)                                             // :3688-3827
        {
            await SpeakAsync(111, 3691);
            await WaoAsync(322);
            State.CallArg = 34;
            await WaoAsync(321);
            await SpeakAsync(123, 3699);
            await SpeakAsync(111, 3708);
            await SpeakAsync(121, 3719);
            await SpeakAsync(113, 3724, 3733);
            await WaoAsync(312);
            await WaoAsync(322);
            await WaoAsync(332);
            await Script.MusicAsync(26);
            await DoorPictureAsync();
            State.CallArg = 1;
            await WaoAsync(311);
            State.CallArg = 34;
            await WaoAsync(321);
            await DoorHeaderAsync(3750);
            await SpeakAsync(111, 3760);
            await SpeakAsync(121, 3769);
            await SpeakAsync(111, 3780, 3783, 3788);
            await SpeakAsync(121, 3797);
            await SpeakAsync(111, 3806);
            await SpeakAsync(121, 3811);
            await SpeakAsync(111, 3820);
            await Script.MusicAsync(21);
            return;
        }
        await DoorPictureAsync();                                       // falls into L_0539e (the gate needs item 1 or 2)
    }

    /// <summary>The door's header in window 113 (<paramref name="line"/> at 32 dots, the run 4 lines on at 24), closed.</summary>
    private async Task DoorHeaderAsync(int line)
    {
        await WaoAsync(113);
        await EngineAsync("ZM", 32);
        await TextAsync(line);
        await EngineAsync("ZM", 24);
        await TextAsync(line + 4);
        await WaoAsync(1);
        await WaoAsync(0);
    }

    /// <summary>
    /// <c>L_0539e</c> (<c>:3828-3854</c>): the door — open (1051) once the quest is at 6, else
    /// closed (1022, <c>WAO_SUB</c> 333) with the statues placed so far in <c>Tmp0001-0004</c> (the
    /// gourd 1054, the monkey 1055, the sunflower 1053, the bonsai 1052), and Rance's portrait.
    /// </summary>
    private async Task DoorPictureAsync()
    {
        if (State.F_GodsDoor_Quest == 6)
        {
            State.CallArg = 1051;
            await WaoAsync(331);
            return;
        }
        State.Tmp0001 = 0;
        State.Tmp0002 = 0;
        State.Tmp0003 = 0;
        State.Tmp0004 = 0;
        if (State.F_Statue_Gourd != 0)
            State.Tmp0001 = 1054;
        if (State.F_Statue_Monkey != 0)
            State.Tmp0002 = 1055;
        if (State.F_Statue_Sunflower != 0)
            State.Tmp0003 = 1053;
        if (State.F_Statue_Bonsai != 0)
            State.Tmp0004 = 1052;
        State.CallArg = 1022;
        await WaoAsync(333);                                            // \L_07689
        State.CallArg = 1;
        await WaoAsync(311);
    }

    /// <summary>
    /// <c>L_05437</c> and its menu <c>L_054b8</c> (<c>:3855-3932</c>), from the dungeon's menu: the
    /// door's music and picture, its header, Kouhime's hint (<c>L_05ea4</c>) while her state is
    /// past 9 and KD's quest at 3, else Rance's line; then the menu until Leave — a statue for each
    /// of items 15-18 someone holds, Rance's talk while Rance (unit 1) is under the cursor and the
    /// door shut, Enter once it is open (into <c>Iｶﾐ</c>, for good), Leave (back to the dungeon's
    /// menu, <c>@L_0076a</c>); a cancel asks again.
    /// </summary>
    private async Task GodsDoorAsync()
    {
        await WaoAsync(0);                                              // \L_0759b
        await Script.MusicAsync(26);
        await WaoAsync(312);
        await WaoAsync(332);
        await DoorPictureAsync();                                       // \L_0539e
        State.CallArg = 1;
        await WaoAsync(311);
        await DoorHeaderAsync(3866);
        if (State.F_Kou_State > 9 && State.F_GodsDoor_Quest == 3)       // :3874-3876
            await KouhimesHintAsync();                                  // @L_05ea4
        else
            await SpeakAsync(113, 3879);
        while (true)
        {
            await WaitForReleaseAsync();                                // L_054b8
            var items = new List<SceneMenuItem>();
            var labels = new List<int>();
            State.Item_Cur = 15;
            if (Item.Holder != 0)
                Offer(3890);                                            // the gourd
            State.Item_Cur = 16;
            if (Item.Holder != 0)
                Offer(3897);                                            // the monkey
            State.Item_Cur = 17;
            if (Item.Holder != 0)
                Offer(3904);                                            // the sunflower
            State.Item_Cur = 18;
            if (Item.Holder != 0)
                Offer(3911);                                            // the bonsai
            if (State.Unit_Cur == 1 && State.F_GodsDoor_Quest != 6)
                Offer(3917);                                            // Rance's talk
            if (State.F_GodsDoor_Quest == 6)
                Offer(3923);                                            // Enter
            Offer(3928);                                                // Leave
            int choice = await SelectAsync(items);
            switch (choice == 0 ? 0 : labels[choice - 1])
            {
                case 3890:
                    await PlaceStatueAsync(15, 3953);                   // L_055f6
                    break;
                case 3897:
                    await PlaceStatueAsync(16, 3973);                   // L_05666
                    break;
                case 3904:
                    await PlaceStatueAsync(17, 3993);                   // L_056d6
                    break;
                case 3911:
                    await PlaceStatueAsync(18, 4013);                   // L_05746
                    break;
                case 3917:
                    await RancesTalkAsync();                            // L_0589d
                    break;
                case 3923:
                    await EnterAsync();                                 // L_06056
                    break;
                case 3928:
                    await SpeakAsync(113, 3936);                        // L_055bc
                    await WaoAsync(312);
                    await WaoAsync(332);
                    await Script.MusicAsync(21);
                    return;
            }

            void Offer(int line)
            {
                items.Add(MenuItem(line));
                labels.Add(line);
            }
        }
    }

    /// <summary>
    /// <c>L_055f6</c>, <c>L_05666</c>, <c>L_056d6</c>, <c>L_05746</c> (<c>:3947-4026</c>): a statue
    /// (item 15-18) placed — nobody holds it any more, its flag set, its line, the door redrawn, and
    /// with all four the door opens (<c>L_057b6</c>).
    /// </summary>
    private async Task PlaceStatueAsync(int item, int line)
    {
        State.Item_Cur = (Sys3Int)item;
        Item.Holder = 0;
        if (item == 15)
            State.F_Statue_Gourd = 1;
        if (item == 16)
            State.F_Statue_Monkey = 1;
        if (item == 17)
            State.F_Statue_Sunflower = 1;
        if (item == 18)
            State.F_Statue_Bonsai = 1;
        await SpeakAsync(113, line);
        await WaoAsync(312);
        await WaoAsync(332);
        await DoorPictureAsync();                                       // \L_0539e
        State.CallArg = 1;
        await WaoAsync(311);
        if (State.F_Statue_Gourd == 1 && State.F_Statue_Monkey == 1 && State.F_Statue_Sunflower == 1 && State.F_Statue_Bonsai == 1)
            await DoorOpensAsync();                                     // \L_057b6
    }

    /// <summary>
    /// <c>L_057b6</c> (<c>:4027-4076</c>): the door opens — Rance's lines, between window fades
    /// <c>F_GodsDoor_Quest := 6</c> and the open door, portrait 80's line.
    /// </summary>
    private async Task DoorOpensAsync()
    {
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 4032);
        await SpeakAsync(113, 4039);
        await SpeakAsync(111, 4046);
        await EngineAsync("PW1", State.FadeSpeed);
        State.F_GodsDoor_Quest = 6;
        await WaoAsync(312);
        await WaoAsync(332);
        await DoorPictureAsync();                                       // \L_0539e
        State.CallArg = 1;
        await WaoAsync(311);
        await EngineAsync("PW0", State.FadeSpeed);
        await SpeakAsync(111, 4061);
        State.CallArg = 80;
        await WaoAsync(321);
        await SpeakAsync(121, 4068);
        await WaoAsync(322);
    }

    /// <summary>The runs of Rance's talk in window 131, each with its key wait (<c>:4277-4365</c>).</summary>
    private static readonly int[] RancesTalkRuns =
    [
        4277, 4280, 4283, 4290, 4297, 4302, 4307, 4310, 4313, 4318, 4323, 4328, 4333, 4336, 4341, 4344,
        4349, 4352, 4355, 4359, 4363,
    ];

    /// <summary>
    /// <c>L_0589d</c> (<c>:4077-4410</c>): Rance's talk before the shut door — Maris (6), Leila (5),
    /// portrait 37; the scene in window 131 with CG 806 / 212 between fades and music 0, then 5; the
    /// map reset, the door's music and picture again, Maris's last lines. Nothing but the screen and
    /// the music changes.
    /// </summary>
    private async Task RancesTalkAsync()
    {
        State.CallArg = 1;
        await WaoAsync(311);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(111, 4084);
        await SpeakAsync(121, 4091);
        await SpeakAsync(111, 4096);
        await SpeakAsync(121, 4103);
        await SpeakAsync(111, 4108);
        await SpeakAsync(121, 4113);
        await SpeakAsync(111, 4118, 4127, 4132, 4135);
        await SpeakAsync(121, 4140);
        await SpeakAsync(113, 4145);
        await SpeakAsync(121, 4150);
        await SpeakAsync(111, 4155, 4160);
        await SpeakAsync(121, 4169);
        await WaoAsync(322);
        State.CallArg = 5;
        await WaoAsync(321);
        await SpeakAsync(121, 4179);
        await WaoAsync(322);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 4193);
        await SpeakAsync(112, 4198);                                    // \L_0758d
        await SpeakAsync(111, 4207);
        await SpeakAsync(121, 4214);
        await SpeakAsync(111, 4219);
        await SpeakAsync(121, 4224);
        await SpeakAsync(122, 4229);                                    // \L_075fd
        await SpeakAsync(111, 4234);
        await WaoAsync(322);
        State.CallArg = 37;
        await WaoAsync(321);
        await SpeakAsync(121, 4246);
        await SpeakAsync(111, 4251);
        await SpeakAsync(121, 4258, 4261);
        await EngineAsync("PF1", State.FadeSpeed);
        await Script.MusicAsync(0);
        await EngineAsync("G", 806);
        await EngineAsync("G", 212);
        await Script.MusicAsync(5);
        await WaoAsync(131);                                            // \L_07651
        await TextAsync(4271);
        await EngineAsync("PF0", State.FadeSpeed);
        await WaoAsync(1);
        foreach (int line in RancesTalkRuns)
        {
            await TextAsync(line);
            await WaoAsync(1);
        }
        await EngineAsync("PF1", State.FadeSpeed);
        await WaoAsync(0);
        await Script.MapCallAsync(0);
        await Script.MusicAsync(26);
        await EngineAsync("PF0", State.FadeSpeed);
        await WaoAsync(312);
        await WaoAsync(332);
        await DoorPictureAsync();                                       // \L_0539e
        State.CallArg = 1;
        await WaoAsync(311);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(111, 4381);
        await SpeakAsync(121, 4386);
        await SpeakAsync(111, 4391);
        await SpeakAsync(121, 4396);
        await SpeakAsync(112, 4401);
    }

    /// <summary>
    /// <c>L_05ea4</c> (<c>:4411-4497</c>): Kouhime's hint, <c>F_GodsDoor_Quest := 4</c> — and for
    /// each of the monkey (16), the sunflower (17) and the bonsai (18) nobody holds and not placed,
    /// a line (the first one found and the later ones in their own words, <c>Tmp0001</c>).
    /// </summary>
    private async Task KouhimesHintAsync()
    {
        State.F_GodsDoor_Quest = 4;
        await SpeakAsync(112, close: false, 4415, 4420, 4425, 4432, 4437);
        State.Tmp0001 = 0;
        State.Item_Cur = 16;
        if (Item.Holder == 0 && State.F_Statue_Monkey == 0)
        {
            await TextAsync(4445);
            await WaoAsync(1);
            State.Tmp0001 = 1;
        }
        State.Item_Cur = 17;
        if (Item.Holder == 0 && State.F_Statue_Sunflower == 0)
        {
            if (State.Tmp0001 == 1)
            {
                await TextAsync(4455);
                await WaoAsync(1);
            }
            if (State.Tmp0001 == 0)
            {
                State.Tmp0001 = 1;
                await TextAsync(4463);
                await WaoAsync(1);
            }
        }
        State.Item_Cur = 18;
        if (Item.Holder == 0 && State.F_Statue_Bonsai == 0)
        {
            if (State.Tmp0001 == 1)
            {
                await TextAsync(4473);
                await WaoAsync(1);
            }
            if (State.Tmp0001 == 0)
            {
                await TextAsync(4480);
                await WaoAsync(1);
            }
        }
        await TextAsync(4487);
        await WaoAsync(1);
        await TextAsync(4492);
        await WaoAsync(1);
        await WaoAsync(0);
    }

    /// <summary>
    /// <c>L_06056</c> (<c>:4498-4556</c>): Enter — Rance's and Maris's last lines, then <c>Iｶﾐ</c>,
    /// which ends only in <c>&amp;#OVER</c> (six sites, no return). Were it to return, the file
    /// would fall into Merim's inscriptions (<c>L_06153</c>) and return from no label call.
    /// </summary>
    private async Task EnterAsync()
    {
        await SpeakAsync(111, 4501);
        await SpeakAsync(123, 4506);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 4515);
        await SpeakAsync(111, 4522, 4531, 4540);
        await SpeakAsync(113, 4549);
        await Script.CallFileAsync("Iｶﾐ.ADV");
        throw new InvalidOperationException($"{Ref(4556)}: Iｶﾐ.ADV returned, which it never does (it ends only in &#OVER).");
    }
}
