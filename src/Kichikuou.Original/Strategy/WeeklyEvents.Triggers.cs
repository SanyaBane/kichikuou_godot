namespace Kichikuou.Original.Strategy;

public sealed partial class WeeklyEvents
{
    /// <summary>
    /// <c>COM.ADV:304-947</c> — tier 2, reached only when no tier-1 trigger fired: the first
    /// trigger that applies plays its scene and the file returns (<c>%0:</c>), except the five
    /// that fall through (STR-16). The rolls are drawn whenever their line is reached (STR-14).
    /// </summary>
    private async Task FirstMatchAsync()
    {
        State.Unit_Cur = 10;                                                                // :304
        if (State.N_Rudrasaum_Turns > 2 && State.F_Menad_State < 7 && Unit.Status != 0)     // :305-309
        {
            await EventAsync("Gﾒﾅﾄﾞ.ADV", 4);
            return;
        }
        if (State.N_ALChurch_Stage < 20 && State.F_Zeth_War == 2 && State.F_Helman_War == 2) // :310-314
        {
            await EventAsync("GAL.ADV", 8);
            return;
        }
        if (State.F_Code_State == 1 && State.F_Zeth_War == 1)                               // :315-319
        {
            await EventAsync("Gｺｰﾄﾞ.ADV", 2);
            return;
        }
        if (State.DateMonth == 1 && State.DateWeek == 1 && State.F_NewYearFeastSeen == 0)  // :320-324 the New Year feast
        {
            await EventAsync("G2.ADV", 9);
            return;
        }
        State.Unit_Cur = 8;                                                                 // :325
        if (Unit.Status == 0 && State.F_Fururu_State < 10)                                  // :326-330
        {
            await EventAsync("Gｺﾙﾄﾞﾊﾞ.ADV", 1);
            return;
        }
        State.Unit_Cur = 38;                                                                // :331
        if (Unit.Status != 0 && State.F_Annis_State == 2)                                   // :332-336
        {
            await EventAsync("Gｱﾆｽ.ADV", 2);
            return;
        }
        State.Cmdr_Cur = 130;                                                               // :337
        if (State.F_Redeye_State == 4 && Cmdr.Status != 0)                                  // :338-342
        {
            await EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 3);
            return;
        }
        State.Unit_Cur = 17;                                                                // :343-351
        if (Unit.Status != 0)
        {
            State.Unit_Cur = 16;
            if (Unit.Status != 0 && State.N_Shizuka_Stage == 6)
            {
                await EventAsync("Gｼｽﾞｶ.ADV", 1);
                return;
            }
        }
        State.Unit_Cur = 6;                                                                 // :352
        State.Tmp0001 = Unit.Status;                                                        // :353
        State.Unit_Cur = 12;                                                                // :354
        if (Unit.Status != 0 && State.Tmp0001 != 0 && State.F_Haurein_Arc == 2)             // :355-359
        {
            await EventAsync("Gﾊｳﾚｰﾝ.ADV", 3);
            return;
        }
        State.Unit_Cur = 2;                                                                 // :360
        if (Unit.Status != 0 && State.F_SillClothes_State == 2)                             // :361-365
        {
            await EventAsync("Gｼｨﾙ.ADV", 30);
            return;
        }
        if (Unit.Status != 0 && State.F_Helman_War == 1 && State.F_Sill_SocksSeen == 0)     // :366-370
        {
            await EventAsync("Gｼｨﾙ2.ADV", 4);
            return;
        }
        if (Unit.Status != 0 && State.F_Helman_War == 2 && State.F_Sill_MassageSeen == 0 && State.StoryStage == 2) // :371-376
        {
            State.F_Sill_MassageSeen = 1;
            await EventAsync("Gｼｨﾙ2.ADV", 1);
            return;
        }
        State.Prov_Cur = 14;                                                                // :377
        if (State.F_Gandhi_State == 4 && Prov.Owner == 1)                                   // :378-382
        {
            await EventAsync("Gｶﾞﾝｼﾞｰ.ADV", 5);
            return;
        }
        if (State.F_SillCow_State == 1)                                                     // :383-387
        {
            await EventAsync("Gｼｨﾙ.ADV", 1);
            return;
        }
        if (State.F_SillCow_State == 4)                                                     // :388-392
        {
            await EventAsync("Gｼｨﾙ.ADV", 4);
            return;
        }
        if (State.F_LibraryGhost_State == 3)                                                // :393-397
        {
            await EventAsync("Gﾕｳﾚｲ.ADV", 4);
            return;
        }
        if (State.F_LibraryGhost_State == 5 && State.F_LibraryGhost_BookReq == 0)           // :398-401
            await EventAsync("Gﾕｳﾚｲ.ADV", 6);                                               // QUIRK: STR-16 — no %0
        State.Unit_Cur = 20;                                                                // :402
        if (Unit.Status != 0 && State.N_Freak_Stage == 1 && State.F_Redeye_State < 10)      // :403-407
        {
            State.Ev_WeeklyEventDone = 1;                                                   // :404, before the call
            await EventAsync("Gﾌﾘｰｸ.ADV", 2);                                               // QUIRK: STR-16 — no %0
        }
        if (State.N_Freak_Stage == 2)                                                       // :408-412
        {
            await EventAsync("Gﾌﾘｰｸ.ADV", 3);
            return;
        }
        if (State.N_Freak_Stage == 3 && State.F_Redeye_State > 9)                           // :413-417
        {
            await EventAsync("Gﾌﾘｰｸ.ADV", 5);
            return;
        }
        if (State.N_Freak_Stage == 6 && State.F_Redeye_State > 9)                           // :418-422
        {
            await EventAsync("Gﾌﾘｰｸ.ADV", 6);
            return;
        }
        if (State.F_Galtia_State != 0)                                                      // :423-442
        {
            State.Cmdr_Cur = 129;
            if (State.F_Babolat_Ev6Seen == 0 && State.F_Babolat_State == 1 && Cmdr.Status != 0)
            {
                await EventAsync("Gﾊﾞﾎﾞﾗ.ADV", 6);
                return;
            }
            State.Cmdr_Cur = 130;
            if (State.F_Redeye_Ev7Seen == 0 && State.F_Redeye_State != 0 && Cmdr.Status != 0)
            {
                await EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 7);
                return;
            }
            State.Cmdr_Cur = 132;
            if (State.F_Kite_Ev8Seen == 0 && State.F_Calory_State > 2 && Cmdr.Status != 0)
            {
                await EventAsync("Gｶｲﾄ.ADV", 8);
                return;
            }
        }
        if (State.F_Mokomoko_State == 14)                                                   // :443-447
        {
            await EventAsync("Gﾓｺﾓｺ.ADV", 2);
            return;
        }
        if (State.F_Mokomoko_State == 13)                                                   // :448-452
        {
            await EventAsync("Gﾓｺﾓｺ.ADV", 3);
            return;
        }
        if (State.Ev_KapalaReport == 1)                                                     // :453-458
        {
            State.Ev_KapalaReport = 0;
            await EventAsync("Gｶﾊﾟｰﾗ.ADV", 5);
            return;
        }
        Rnd(4);                                                                             // :459 QUIRK: STR-14
        if (State.RetVal == 1 && State.F_Kapala_State == 2)                                 // :460-464
        {
            await EventAsync("Gｶﾊﾟｰﾗ.ADV", 6);
            return;
        }
        Rnd(2);                                                                             // :465 QUIRK: STR-14
        if (State.F_Menad_State == 5 && State.RetVal == 1)                                  // :466-470
        {
            await EventAsync("Gﾒﾅﾄﾞ.ADV", 3);
            return;
        }
        Rnd(5);                                                                             // :471 QUIRK: STR-14
        if (State.StoryStage == 2 && State.F_Suzume_State == 2 && State.RetVal == 1)        // :472-476
        {
            await EventAsync("Gｽｽﾞﾒ.ADV", 1);
            return;
        }
        State.Item_Cur = 9;                                                                 // :477
        if (State.F_Wendy_State == 1 && Item.Holder == 100)                                 // :478-482
        {
            await EventAsync("Gｳｪﾝﾃﾞｨ.ADV", 1);
            return;
        }
        State.Unit_Cur = 37;                                                                // :483
        if (Unit.Status != 0 && State.F_Chizuko_State == 2)                                 // :484-488
        {
            await EventAsync("Gﾁｽﾞｺ.ADV", 3);
            return;
        }
        State.Unit_Cur = 13;                                                                // :489
        if (Unit.Status != 0 && State.N_Melfeis_Stage == 10)                                // :490-494
        {
            await EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 2);
            return;
        }
        if (Unit.Status != 0 && State.N_Melfeis_Stage == 8)                                 // :495-499
        {
            await EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 6);
            return;
        }
        State.Unit_Cur = 8;                                                                 // :500
        State.Tmp0001 = Unit.Status;                                                        // :501
        State.Unit_Cur = 13;                                                                // :502
        if (Unit.Status != 0 && State.N_Melfeis_Stage == 3 && State.F_Melfeis_Ev7Seen == 0 && State.Tmp0001 != 0) // :503-507
        {
            await EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 7);
            return;
        }
        State.Unit_Cur = 16;                                                                // :508
        if (Unit.Status != 0 && State.F_Maria_Tulip == 14)                                  // :509-513
        {
            await EventAsync("Gﾏﾘｱ.ADV", 2);
            return;
        }
        if (State.F_Maria_State == 5)                                                       // :514-518
        {
            await EventAsync("Gﾏﾘｱ.ADV", 3);
            return;
        }
        State.Unit_Cur = 24;                                                                // :519
        if (Unit.Status != 0 && State.Week > State.N_Custom_JoinWeek + 12 && State.N_Milli_CureStage != 4 && State.F_Mill_State < 2) // :520-524
        {
            await EventAsync("Gﾐﾘ.ADV", 1);
            return;
        }
        if (State.N_Miri_DeadlineWeek != 0 && State.N_Miri_DeadlineWeek < State.Week)       // :525-529
        {
            await EventAsync("Gﾐﾘ.ADV", 2);
            return;
        }
        State.Unit_Cur = 23;                                                                // :530
        if (Unit.Status != 0 && State.F_Ran_State == 4)                                     // :531-535
        {
            await EventAsync("Gﾗﾝ.ADV", 1);
            return;
        }
        State.Unit_Cur = 32;                                                                // :536
        if (Unit.Status != 0 && State.N_Miki_Timer < 11 && Unit.Item == 43)                 // :537-541
        {
            await EventAsync("Gﾐｷ.ADV", 7);
            return;
        }
        if (Unit.Status != 0 && State.N_Miki_Timer == 5)                                    // :542-546
        {
            await EventAsync("Gﾐｷ.ADV", 3);
            return;
        }
        if (Unit.Status != 0 && State.N_Miki_Timer == 0)                                    // :547-551
        {
            await EventAsync("Gﾐｷ.ADV", 4);
            return;
        }
        if (Unit.Status != 0 && State.VAR0386 != 0 && State.F_Miki_Ev11Seen == 0)           // :552-556
        {
            await EventAsync("Gﾐｷ.ADV", 11);
            return;
        }
        if (State.N_Miki_Stage == 4)                                                        // :557-560 a week without a scene
        {
            State.N_Miki_Stage = 5;
            return;
        }
        if (State.N_Miki_Stage == 5)                                                        // :561-565
        {
            await EventAsync("Gﾐｷ.ADV", 5);
            return;
        }
        if (State.StoryStage == 3 && State.N_Miki_Stage == 6 && State.F_Piar_State != 100 && State.F_Piar_State != 0) // :566-573
        {
            State.Cmdr_Cur = 137;
            if (Cmdr.Status != 0)
            {
                await EventAsync("Gﾐｷ.ADV", 6);
                return;
            }
        }
        if (State.StoryStage == 3 && State.N_Miki_Stage == 8 && State.F_Piar_State != 100 && State.F_Piar_State != 0) // :574-578
        {
            await EventAsync("Gﾐｷ.ADV", 9);
            return;
        }
        State.Unit_Cur = 48;                                                                // :579-587
        if (Unit.CgFull == 0 && State.F_HannyKing_State == 7)
        {
            State.Unit_Cur = 33;
            if (Unit.Status != 0)
            {
                await EventAsync("Gﾊﾆｰ.ADV", 2);
                return;
            }
        }
        State.Unit_Cur = 5;                                                                 // :588
        if (Unit.Status == 0 && State.F_Julia_State == 0)                                   // :589-593
        {
            await EventAsync("Gｼﾞｭﾘｱ.ADV", 1);
            return;
        }
        State.Unit_Cur = 5;                                                                 // :594
        State.Tmp0001 = Unit.Status;                                                        // :595
        State.Unit_Cur = 33;                                                                // :596
        if (Unit.Status == 0 && State.Tmp0001 == 0                                          // :597-601
            && (State.F_Aviator_State < 10 || State.F_Aviator_State == 100)
            && State.Bld_GirlsAcademy != 2)
        {
            await EventAsync("Gｱﾋﾞｧﾄｰﾙ.ADV", 2);
            return;
        }
        if (State.F_Arlcoate_State == 1)                                                    // :602-606
        {
            await EventAsync("Gｱｰﾙｺｰﾄ.ADV", 2);
            return;
        }
        if (State.F_Rafalia_State == 1)                                                     // :607-611
        {
            await EventAsync("Gﾗﾌｧﾘｱ.ADV", 2);
            return;
        }
        Rnd(15);                                                                            // :612 QUIRK: STR-14
        if (State.RetVal == 1 && State.F_Pastel_State != 0)                                 // :613-617
        {
            await EventAsync("Gｶﾗｰ.ADV", 2);
            return;
        }
        Rnd(3);                                                                             // :618 QUIRK: STR-14
        if (State.RetVal == 1 && State.F_Merim_Dungeon == 2)                                // :619-623
        {
            await EventAsync("Gﾒﾘﾑ.ADV", 3);
            return;
        }
        State.Prov_Cur = 3;                                                                 // :624
        if (Prov.Owner == 1 && State.F_Gandhi_State == 0)                                   // :625-629
        {
            await EventAsync("Gｶﾞﾝｼﾞｰ.ADV", 1);
            return;
        }
        State.Unit_Cur = 6;                                                                 // :630
        State.Tmp0001 = Unit.Status;                                                        // :631
        State.Unit_Cur = 11;                                                                // :632
        Rnd(10);                                                                            // :633 QUIRK: STR-14
        if (Unit.Status != 0 && State.Tmp0001 != 0 && State.RetVal == 1)                    // :634-638
        {
            await EventAsync("Gﾊﾞﾚｽ.ADV", 1);
            return;
        }
        State.Unit_Cur = 10;                                                                // :639
        if (Unit.Status != 0 && State.N_Kanami_State == 2 && State.F_Menad_State < 4)       // :640-644
        {
            await EventAsync("Gｶﾅﾐ.ADV", 13);
            return;
        }
        State.Unit_Cur = 18;                                                                // :645
        State.Tmp0001 = Unit.Status;                                                        // :646
        State.Unit_Cur = 22;                                                                // :647
        if (Unit.Status != 0 && State.Tmp0001 != 0 && State.F_Lelyukov_PattonTalkSeen == 0) // :648-652
        {
            await EventAsync("Gﾊﾟｯﾄﾝ.ADV", 13);
            return;
        }
        State.Unit_Cur = 21;                                                                // :653
        if (Unit.Status != 0 && State.N_Hubert_FreyaUses < 49 && State.N_Hubert_FreyaUses > 5 && State.N_Freya_State == 20) // :654-658
        {
            await EventAsync("Gﾋｭｰﾊﾞｰﾄ.ADV", 1);
            return;
        }
        State.Unit_Cur = 36;                                                                // :659
        if (Unit.Status != 0 && State.F_Aviator_State == 10 && Unit.Troops < 200)           // :660-663
            await EventAsync("Gｱﾋﾞｧﾄｰﾙ.ADV", 3);                                            // QUIRK: STR-16 — no %0
        if (State.N_Aaya_Stage == 4)                                                        // :664-667
            await EventAsync("Gｱｰﾔ.ADV", 3);                                                // QUIRK: STR-16 — no %0
        if (State.F_Zeth_War != 2 && State.F_Gandhi_State == 1                              // :668-674
            && State.Gandhi_NextEventWeek < State.Week && State.F_Gandhi_TravelStop1Seen == 0)
        {
            State.F_Gandhi_TravelStop1Seen = 1;
            State.Prov_Cur = 24;
            await EventAsync("Gｶﾞﾝｼﾞｰ.ADV", 6);
            return;
        }
        if (State.F_Zeth_War != 2 && State.F_Gandhi_State == 2                              // :675-681
            && State.Gandhi_NextEventWeek < State.Week && State.F_Gandhi_TravelStop2Seen == 0)
        {
            State.F_Gandhi_TravelStop2Seen = 1;
            State.Prov_Cur = 23;
            await EventAsync("Gｶﾞﾝｼﾞｰ.ADV", 6);
            return;
        }
        if (State.F_Zeth_War != 2 && State.F_Gandhi_State == 3                              // :682-688
            && State.Gandhi_NextEventWeek < State.Week && State.F_Gandhi_TravelStop3Seen == 0)
        {
            State.F_Gandhi_TravelStop3Seen = 1;
            State.Prov_Cur = 20;
            await EventAsync("Gｶﾞﾝｼﾞｰ.ADV", 6);
            return;
        }
        if (State.F_Seizel_State == 6)                                                      // :689-693
        {
            await EventAsync("Gｻｲｾﾞﾙ.ADV", 5);
            return;
        }
        Rnd(10);                                                                            // :694 QUIRK: STR-14
        if (State.N_Saizel_Weeks > 40 && State.F_Seizel_State != 20 && State.RetVal == 1)   // :695-699
        {
            await EventAsync("Gｻｲｾﾞﾙ.ADV", 9);
            return;
        }
        if (State.F_DX_State != 0 && State.F_DX_State != 100 && State.F_Mokomoko_State == 0) // :700-704
        {
            await EventAsync("Gﾓｺﾓｺ.ADV", 4);
            return;
        }
        if (State.F_Saitou_State == 1)                                                      // :705-709
        {
            await EventAsync("G2.ADV", 3);
            return;
        }
        State.Prov_Cur = 33;                                                                // :710
        if (Prov.Owner == 1 && State.F_Helman_War == 2 && State.F_Lelyukov_State == 90      // :711-715
            && (State.N_Amiran_Stage == 1 || State.N_Amiran_Stage == 100))
        {
            await EventAsync("Gｱﾐﾗﾝ.ADV", 2);
            return;
        }
        State.Unit_Cur = 7;                                                                 // :716
        if (Unit.Status != 0 && State.F_LeilaRick_State == 3)                               // :717-721
        {
            await EventAsync("Gﾘｯｸ.ADV", 1);
            return;
        }
        State.Unit_Cur = 37;                                                                // :722
        if (State.N_Kaoru_State == 2 && Unit.Status != 0)                                   // :723-727
        {
            await EventAsync("Gｶｸｻﾝ.ADV", 1);
            return;
        }
        State.Cmdr_Cur = 32;                                                                // :728
        if (Cmdr.Status != 0 && State.F_Helman_War == 2 && State.F_PattonArmy_State == 0)   // :729-733
        {
            await EventAsync("Gｱﾘｽﾄﾚｽ.ADV", 4);
            return;
        }
        State.Unit_Cur = 46;                                                                // :734
        if (Unit.Status != 0 && State.F_Somita_Fate == 0 && State.Somita_EventWeek < State.Week) // :735-739
        {
            await EventAsync("Gｿﾐｰﾀ.ADV", 2);
            return;
        }
        State.Unit_Cur = 26;                                                                // :740-748
        if (Unit.Status != 0)
        {
            Rnd(5);
            if (State.RetVal == 1)
            {
                await EventAsync("Gｶｾｲ.ADV", 4);
                return;
            }
        }
        if (Unit.Status != 0 && State.N_Mars_Score > 7)                                     // :749-753
        {
            await EventAsync("Gｶｾｲ.ADV", 5);
            return;
        }
        if (State.F_Nagi_State == 2 && State.Nagi_EvWeek + 9 < State.Week)                  // :754-758
        {
            await EventAsync("Gﾅｷﾞ.ADV", 7);
            return;
        }
        if (State.F_Nagi_State == 3 && State.Nagi_EvWeek + 19 < State.Week)                 // :759-763
        {
            await EventAsync("Gﾅｷﾞ.ADV", 8);
            return;
        }
        State.Unit_Cur = 4;                                                                 // :764
        if (Unit.Status != 0 && (State.N_Soul_Stage == 10 || State.N_Soul_Stage == 15))     // :765-769
        {
            await EventAsync("Gｿｳﾙ.ADV", 2);
            return;
        }
        if (Unit.Status != 0 && State.N_Soul_Stage > 19 && State.N_Soul_Stage != 200)       // :770-774
        {
            await EventAsync("Gｿｳﾙ.ADV", 3);
            return;
        }
        if (State.StoryStage > 1 && State.F_ShangriLa_State == 0)                           // :775-779
        {
            await EventAsync("Gﾏﾘｽ.ADV", 12);
            return;
        }
        if (State.F_Cell_Arc > 3 && State.F_Cell_ChurchArc == 0 && State.F_Helman_War == 2) // :780-784
        {
            await EventAsync("Gｾﾙ.ADV", 5);
            return;
        }
        if (State.F_Cell_ChurchArc == 10)                                                   // :785-789
        {
            await EventAsync("Gｾﾙ.ADV", 6);
            return;
        }
        if (State.F_Cell_ChurchArc == 11)                                                   // :790-794
        {
            await EventAsync("Gｾﾙ.ADV", 7);
            return;
        }
        if (State.F_Cell_ChurchArc == 12 && State.F_DX_State > 0 && State.F_DX_State != 100) // :795-799
        {
            await EventAsync("Gｾﾙ.ADV", 8);
            return;
        }
        Rnd(2);                                                                             // :800 QUIRK: STR-14
        if (State.F_Elena_State == 4 && State.RetVal == 1)                                  // :801-805
        {
            await EventAsync("Gｴﾚﾅ.ADV", 4);
            return;
        }
        if (State.F_Elena_State == 5 && State.RetVal == 2)                                  // :806-810
        {
            await EventAsync("Gｴﾚﾅ.ADV", 5);
            return;
        }
        State.Unit_Cur = 29;                                                                // :811
        if (State.F_Maris_KesselringTalkSeen == 0 && Unit.Status != 0 && State.F_Kesselring_State == 2) // :812-816
        {
            await EventAsync("Gﾏﾘｽ.ADV", 16);
            return;
        }
        if (Unit.Status != 0 && State.VAR0386 != 0 && State.F_Hornet_Joined != 0 && State.F_Satella_Ev3Seen == 0) // :817-821
        {
            await EventAsync("Gｻﾃﾗ.ADV", 3);
            return;
        }
        State.Unit_Cur = 25;                                                                // :822
        if (Unit.Status != 0 && State.F_Asuka_State == 3)                                   // :823-827
        {
            await EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 3);
            return;
        }
        if (Unit.Status != 0 && State.F_Asuka_State == 4)                                   // :828-832
        {
            await EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 4);
            return;
        }
        if (Unit.Status != 0 && State.F_Asuka_State == 5)                                   // :833-837
        {
            await EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 5);
            return;
        }
        if (Unit.Status != 0 && State.F_Asuka_State == 6)                                   // :838-842
        {
            await EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 6);
            return;
        }
        if (Unit.Status != 0 && State.F_Asuka_State == 7)                                   // :843-847
        {
            await EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 7);
            return;
        }
        if (Unit.Status != 0 && State.F_Mill_State == 1 && State.F_Merim_Dungeon == 1 && State.F_Merim_MillArc == 0) // :848-852
        {
            await EventAsync("Gﾒﾘﾑ.ADV", 5);
            return;
        }
        if (State.F_Rance_StiffShouldersSeen == 0 && State.F_Zeth_War == 2)                 // :853-857
        {
            await EventAsync("G2.ADV", 10);
            return;
        }
        if (State.N_Freya_Missions > 2 && State.N_Freya_State < 10 && State.F_Helman_War == 1) // :858-862
        {
            await EventAsync("Gﾌﾚｲｱ.ADV", 3);
            return;
        }
        if (State.F_Shariela_State == 1)                                                    // :863-867
        {
            await EventAsync("Gｼｬﾘｴﾗ.ADV", 2);
            return;
        }
        if (State.F_Shariela_State == 2 && State.Shariela_EvWeek + 40 < State.Week)         // :868-872
        {
            await EventAsync("Gｼｬﾘｴﾗ.ADV", 3);
            return;
        }
        if (State.F_Shariela_State == 3 && State.Shariela_EvWeek + 42 < State.Week)         // :873-877
        {
            await EventAsync("Gｼｬﾘｴﾗ.ADV", 4);
            return;
        }
        Rnd(10);                                                                            // :878 one roll for the next five, QUIRK: STR-14
        State.Unit_Cur = 5;                                                                 // :879
        State.Tmp0001 = Unit.Status;                                                        // :880
        State.Unit_Cur = 7;                                                                 // :881
        if (Unit.Status != 0 && State.Tmp0001 != 0 && State.RetVal == 1 && State.F_LeilaRick_State == 7) // :882-886
        {
            await EventAsync("Gﾚｲﾗ.ADV", 3);
            return;
        }
        if (Unit.Status != 0 && State.RetVal == 2 && State.F_British_State > 2 && State.F_GodsDoor_Quest > 1) // :887-891
        {
            await EventAsync("Gﾌﾞﾘﾃｨ.ADV", 2);
            return;
        }
        State.Unit_Cur = 33;                                                                // :892
        if (Unit.Status != 0 && State.RetVal == 3)                                          // :893-897
        {
            await EventAsync("Gｼﾞｭﾘｱ.ADV", 2);
            return;
        }
        if (State.F_GodsDoor_Quest != 0 && State.RetVal == 4)                               // :898-902
        {
            await EventAsync("Gﾎﾗｶﾞ.ADV", 3);
            return;
        }
        State.Unit_Cur = 39;                                                                // :903
        if (Unit.Status != 0 && State.RetVal == 5)                                          // :904-908
        {
            await EventAsync("Gﾏｼﾞｯｸ.ADV", 26);
            return;
        }
        if (State.Week > 15 && State.F_Maris_State == 0)                                    // :909-913
        {
            await EventAsync("Gﾏﾘｽ.ADV", 1);
            return;
        }
        if (State.Week > 20 && State.F_Maris_State == 1)                                    // :914-918
        {
            await EventAsync("Gﾏﾘｽ.ADV", 2);
            return;
        }
        if (State.Week > 21 && State.F_Maris_State == 2)                                    // :919-923
        {
            await EventAsync("Gﾏﾘｽ.ADV", 3);
            return;
        }
        if (State.F_Maris_State == 3)                                                       // :924-928
        {
            await EventAsync("Gﾏﾘｽ.ADV", 13);
            return;
        }
        if (State.F_Maris_State == 4)                                                       // :929-933
        {
            await EventAsync("Gﾏﾘｽ.ADV", 14);
            return;
        }
        Rnd(10);                                                                            // :934 QUIRK: STR-14
        if (State.F_Helman_War == 2 || State.F_Zeth_War == 2)                               // :935-937 certain once a war is won
            State.RetVal = 1;
        if (State.F_KaijuPrince_State == 1 && State.Unit48_Identity == 0 && State.RetVal == 1) // :938-941
            await EventAsync("Gｵｳｼﾞ.ADV", 1);                                               // QUIRK: STR-16 — no %0
        if (State.N_ALChurch_Stage == 100)                                                  // :942-946
            await EventAsync("GAL.ADV", 5);
        // :947 %0:
    }
}
