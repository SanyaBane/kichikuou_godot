using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="MapMarkers"/> and <see cref="MapCameraForm"/>: our map's plates and their clicks (docs/ui/command-screen.md, "The map's markers").</summary>
public class MapMarkersTests
{
    /// <summary>
    /// Leazas Castle (1, ours, a big click square) at (100, 100), North (2, next to it) at (200, 100),
    /// Far (3, next to North only) at (300, 100), Out (4, ours) at (700, 100) out of the view; the
    /// Monsters' Cave (dungeon 2) in Leazas Castle searched to B5, the Thieves' Cave (dungeon 3) inside
    /// the castle's square, Rising Dragon Mountain (dungeon 11) in Far searched whole. The camera is
    /// the view's corner at (0, 0); story stage 1.
    /// </summary>
    private static GameState World()
    {
        var s = new GameState { Prov_StrBase = 10, Prov_Count = 4, Dgn_Count = 30, StoryStage = 1 };
        void Prov(int p, string name, int owner, int x, int y, params int[] adj)
        {
            s.Strings[10 + p - 1] = name;
            var r = s.Provs[p];
            r.Owner = (Sys3Int)owner;
            r.MapX = (Sys3Int)x;
            r.MapY = (Sys3Int)y;
            if (adj.Length > 0)
                r.Adj1 = (Sys3Int)adj[0];
        }
        Prov(1, "Leazas Castle", 1, 100, 100);
        Prov(2, "North", 4, 200, 100, 1);
        Prov(3, "Far", 4, 300, 100, 2);
        Prov(4, "Out", 1, 700, 100);
        void Dgn(int d, int prov, int x, int y, int state, int floor)
        {
            var r = s.Dgns[d];
            r.Prov = (Sys3Int)prov;
            r.MapX = (Sys3Int)x;
            r.MapY = (Sys3Int)y;
            r.State = (Sys3Int)state;
            r.SavedFloor = (Sys3Int)floor;
        }
        Dgn(2, 1, 150, 200, 1, 5);
        Dgn(3, 1, 105, 105, 0, 0);
        Dgn(11, 3, 300, 200, 2, 0);
        s.MapCamX = 0;
        s.MapCamY = 0;
        return s;
    }

    /// <summary>The names of <c>Cﾒｲｷｭｳ.ADV</c>'s <c>MS 8</c> lines the world's dungeons read.</summary>
    private static string? Literal(ScriptRef at) => at.Line switch
    {
        36 => "Monsters’ Cave",
        40 => "Thieves’ Cave",
        79 => "Rising Dragon Mountain",
        137 => "Titi Lake",
        140 => "Haniwa Temple",
        _ => null,
    };

    [Fact]
    public void Provinces_carry_their_relation_and_dungeons_their_kind_and_search()
    {
        var markers = MapMarkers.All(World(), Literal).ToDictionary(m => (m.Kind, m.Id));
        Assert.Equal(7, markers.Count);
        var castle = markers[(MapHitKind.Province, 1)];
        Assert.Equal(("Leazas Castle", MapRelation.Own, true), (castle.Name, castle.Relation, castle.Big));
        Assert.Equal(MapRelation.Bordering, markers[(MapHitKind.Province, 2)].Relation);
        Assert.Equal(MapRelation.Other, markers[(MapHitKind.Province, 3)].Relation);
        Assert.False(markers[(MapHitKind.Province, 2)].Big);

        var cave = markers[(MapHitKind.Dungeon, 2)];
        Assert.Equal(("Monsters' Cave", DungeonKind.Pit, true, "B5/14"), (cave.Name, cave.Dungeon, cave.Open, cave.Progress));
        var thieves = markers[(MapHitKind.Dungeon, 3)];
        Assert.Equal((DungeonKind.Event, (string?)null), (thieves.Dungeon, thieves.Progress));
        var tower = markers[(MapHitKind.Dungeon, 11)];
        Assert.Equal((DungeonKind.Tower, false, "✓"), (tower.Dungeon, tower.Open, tower.Progress));
    }

    /// <summary>
    /// The prologue (stage 0): owner 1 is Leazas, not the thieves — no province is ours, Vladivostok (33)
    /// and Golak (34) open <c>Cﾃｷﾄｼ</c> as the bordering ones do, every other is dimmed, and Leazas's
    /// figures stay "??" and are not remembered.
    /// </summary>
    [Fact]
    public void In_the_prologue_only_Vladivostok_and_Golak_are_not_dimmed()
    {
        var s = World();
        s.StoryStage = 0;
        Assert.Equal(MapRelation.Other, MapMarkers.RelationOf(s, 1));    // Leazas Castle (1), owner 1
        Assert.Equal(MapRelation.Other, MapMarkers.RelationOf(s, 2));    // North (2), next to it
        Assert.Equal(MapRelation.Bordering, MapMarkers.RelationOf(s, 33));
        Assert.Equal(MapRelation.Bordering, MapMarkers.RelationOf(s, 34));
        Assert.Equal("Economy: ??", MapMarkers.CardOf(s, 1)[0]);
        s.MarkOwnProvincesKnown();
        Assert.Empty(s.KnownProvinces);

        s.StoryStage = 1;
        Assert.Equal(MapRelation.Own, MapMarkers.RelationOf(s, 1));
        Assert.Equal(MapRelation.Other, MapMarkers.RelationOf(s, 33));
    }

    [Fact]
    public void A_tower_counts_its_floors_up()
    {
        var s = World();
        s.Dgns[11].State = 1;
        s.Dgns[11].SavedFloor = 7;
        Assert.Equal("7F/30", MapMarkers.ProgressOf(s, 11, DungeonKind.Tower, 30));
        s.Dgns[11].SavedFloor = 0;                                       // under way, from the top again
        Assert.Null(MapMarkers.ProgressOf(s, 11, DungeonKind.Tower, 30));
    }

    [Fact]
    public void Titi_Lake_reads_Haniwa_Temple_once_the_Hanny_King_has_come()
    {
        var s = World();
        var titi = s.Dgns[25];
        titi.Prov = 1;
        titi.MapX = 400;
        titi.MapY = 300;
        Assert.Equal("Titi Lake", MapMarkers.All(s, Literal).Single(m => m.Kind == MapHitKind.Dungeon && m.Id == 25).Name);
        s.F_HannyKing_State = 2;
        Assert.Equal("Haniwa Temple", MapMarkers.All(s, Literal).Single(m => m.Kind == MapHitKind.Dungeon && m.Id == 25).Name);
    }

    [Fact]
    public void A_plate_is_answered_with_the_still_click_at_its_point()
    {
        var s = World();
        var north = MapMarkers.All(s, Literal).Single(m => m.Kind == MapHitKind.Province && m.Id == 2);
        Assert.Equal([(16, 200, 100), (0, 200, 100)], Gestures(p => Assert.True(MapMarkers.Press(s, north, p))));

        s.MapCamX = 100;                                                 // dragged: the corner moved
        Assert.Equal((100, 100), MapMarkers.ClickPoint(s, north));
    }

    [Fact]
    public void A_plate_whose_click_the_script_would_read_otherwise_gives_none()
    {
        var s = World();
        var markers = MapMarkers.All(s, Literal);
        Assert.Null(MapMarkers.ClickPoint(s, markers.Single(m => m.Kind == MapHitKind.Province && m.Id == 4)));  // out of view
        Assert.Null(MapMarkers.ClickPoint(s, markers.Single(m => m.Kind == MapHitKind.Dungeon && m.Id == 3)));   // in the castle's square
        Assert.Equal((300, 200), MapMarkers.ClickPoint(s, markers.Single(m => m.Kind == MapHitKind.Dungeon && m.Id == 11)));  // not ours: opens nothing
        Assert.Empty(Gestures(p => Assert.False(MapMarkers.Press(s, markers.Single(m => m.Kind == MapHitKind.Province && m.Id == 4), p))));
    }

    /// <summary>
    /// A province's card: ours with its figures and what they do, one not ours "??" until it is known —
    /// ours at a map wait once, or scouted (<see cref="GameState.KnownProvinces"/>) — and then its figures
    /// as the state has them now. Dungeons have none.
    /// </summary>
    [Fact]
    public void A_province_card_shows_the_figures_of_ours_and_of_the_known_ones_and_else_question_marks()
    {
        var s = World();
        s.Provs[1].Economy = 20;
        s.Provs[1].Defense = 10;
        s.Provs[1].BattleArea = 180;
        s.Provs[2].Economy = 12;
        s.Provs[2].Defense = 0;
        s.Provs[2].BattleArea = 232;
        Assert.Equal(["Economy: 20 (2000K Gold)", "Defense: 10 (Walls +20%)", "Battle Area: 180 troops"], MapMarkers.CardOf(s, 1));
        Assert.Equal(["Economy: ??", "Defense: ??", "Battle Area: ??"], MapMarkers.CardOf(s, 2));

        s.MarkProvinceKnown(2);
        Assert.Equal(["Economy: 12 (1200K Gold)", "Defense: 0 (Walls +0%)", "Battle Area: 232 troops"], MapMarkers.CardOf(s, 2));
        var markers = MapMarkers.All(s, Literal);
        Assert.Equal(MapMarkers.CardOf(s, 2), markers.Single(m => m is { Kind: MapHitKind.Province, Id: 2 }).Card);
        Assert.All(markers.Where(m => m.Kind == MapHitKind.Dungeon), m => Assert.Null(m.Card));

        s.MarkOwnProvincesKnown();                                       // Leazas Castle (1) and Out (4)
        Assert.Equal([1, 2, 4], s.KnownProvinces);
        s.Provs[4].Owner = 4;                                            // lost: still known
        Assert.Equal("Economy: 0 (0K Gold)", MapMarkers.CardOf(s, 4)[0]);
    }

    [Fact]
    public void The_camera_is_the_corner_from_the_release_wait_to_a_right_button()
    {
        var form = new MapCameraForm();
        form.OnMouseRead(0);
        Assert.False(form.Corner);                                       // no map wait
        form.OnMapWait(true);
        form.OnMouseRead(32);                                            // still held from the menu
        Assert.False(form.Corner);
        form.OnMouseRead(0);                                             // the release wait ends: the loop makes it the corner
        Assert.True(form.Corner);
        form.OnMouseRead(16);
        form.OnMouseRead(48);                                            // a right press during a drag snaps back, still the corner
        Assert.True(form.Corner);
        form.OnMouseRead(32);                                            // the menu's way out: centred again
        Assert.False(form.Corner);
        form.OnMapWait(false);
        Assert.False(form.Waiting);
    }

    private static List<(int, int, int)> Gestures(Action<PointerScript> queue)
    {
        var pointer = new PointerScript { SettleReads = 0 };
        queue(pointer);
        var reads = new List<(int, int, int)>();
        while (pointer.Pending > 0)
        {
            var m = pointer.Next(out _);
            reads.Add((m.Button.Value, m.X.Value, m.Y.Value));
        }
        return reads;
    }
}
