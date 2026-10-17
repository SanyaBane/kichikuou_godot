using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A province's place to the player, as our map's plates show it (docs/ui/command-screen.md, "The map's markers").</summary>
public enum MapRelation
{
    /// <summary>Ours (owner 1, from stage 1): a click opens <c>Cｼﾊｲﾄｼ</c>.</summary>
    Own,

    /// <summary>Not ours, next to one of ours (Adj1-5), or in the prologue Vladivostok (33) or Golak (34): a click opens <c>Cﾃｷﾄｼ</c>.</summary>
    Bordering,

    /// <summary>Any other: a click opens <c>Cﾃｷﾄｼ2</c>; its plate is dimmed.</summary>
    Other,
}

/// <summary>What a dungeon is, for its plate's icon.</summary>
public enum DungeonKind
{
    /// <summary>A dungeon one searches floor by floor down.</summary>
    Pit,

    /// <summary>A tower one searches floor by floor up (floor mode 2).</summary>
    Tower,

    /// <summary>A place of story scenes, never searched.</summary>
    Event,
}

/// <summary>
/// One marker of our map: a province or a dungeon at its map point (<c>MapX</c>, <c>MapY</c>, the
/// middle of the town's picture and of the original's click square), with what its plate shows and,
/// for a province, its card's lines (<see cref="MapMarkers.CardOf"/>).
/// </summary>
public sealed record MapMarker(
    MapHitKind Kind,
    int Id,
    string Name,
    int MapX,
    int MapY,
    bool Big,
    MapRelation Relation,
    bool Open,
    DungeonKind Dungeon,
    string? Progress,
    IReadOnlyList<string>? Card = null);

/// <summary>
/// The markers of our map during <c>COMMAND</c>'s map wait (docs/ui/command-screen.md, "The map's
/// markers"): a name plate under every province and dungeon, a click on it answered with the still
/// click the original expects at the marker's map point, so the script decides as always.
/// </summary>
public static class MapMarkers
{
    /// <summary>
    /// Every province with a name and a map point, then every dungeon with one, as the state has them
    /// now. Dungeon names are the <c>MS 8</c> lines of <c>Cﾒｲｷｭｳ.ADV</c>, read through <paramref name="literal"/>
    /// (null without the player's files: the dungeon is then left out). In the prologue (stage 0) the
    /// Thieves' Hideout (dungeon 1) is the only dungeon shown: the others the map shows then — the
    /// Monsters' Cave (2), the Thieves' Cave (3), Isolated Shrine Island (15) — still take the original's
    /// click, but it only pans the camera (DGN-20).
    /// </summary>
    public static IReadOnlyList<MapMarker> All(GameState state, Func<ScriptRef, string?> literal)
    {
        var markers = new List<MapMarker>();
        for (int p = 1; p <= state.Prov_Count.Value; p++)
        {
            var prov = state.Provs[p];
            string name = ProvinceName(state, p);
            if (name.Length == 0 || (prov.MapX == 0 && prov.MapY == 0))
                continue;
            markers.Add(new MapMarker(MapHitKind.Province, p, Plain(name), prov.MapX.Value, prov.MapY.Value,
                CommandPhase.HitRadius((Sys3Int)p) > 12, RelationOf(state, p), true, DungeonKind.Event, null, CardOf(state, p)));
        }
        for (int d = 1; d <= state.Dgn_Count.Value; d++)
        {
            var dgn = state.Dgns[d];
            if ((dgn.MapX == 0 && dgn.MapY == 0) || DungeonCommand.NameAt(state, d) is not { } at || literal(at) is not { } name)
                continue;
            if (state.StoryStage == 0 && d != 1)                                // QUIRK: DGN-20 — clickable, but no search before stage 1
                continue;
            var search = DungeonCommand.SearchOf(d);
            var kind = search is null ? DungeonKind.Event : search.Value.Mode == 2 ? DungeonKind.Tower : DungeonKind.Pit;
            bool open = dgn.Prov == 0 || state.Provs[dgn.Prov].Owner == 1;      // COMMAND.ADV:150, as GesturePoints.MapAt
            markers.Add(new MapMarker(MapHitKind.Dungeon, d, Plain(name), dgn.MapX.Value, dgn.MapY.Value, false,
                MapRelation.Other, open, kind, ProgressOf(state, d, kind, search?.MaxFloor ?? 0)));
        }
        return markers;
    }

    /// <summary>The province's name: string <c>Prov_StrBase + p − 1</c> (<c>MS 8</c> of <c>SET_MAP.ADV</c>).</summary>
    public static string ProvinceName(GameState state, int province)
    {
        int at = (state.Prov_StrBase + (Sys3Int)(province - 1)).Value;
        return at > 0 && at < state.Memory.Strings.Length ? state.Memory.Strings[at] : string.Empty;
    }

    /// <summary>
    /// Ours, next to ours or other — the three ways <c>COMMAND</c> dispatches a click
    /// (<c>CommandPhase.ProvinceClickedAsync</c>, <c>COMMAND.ADV:514-577</c>): in the prologue none is ours,
    /// Vladivostok (33) and Golak (34) open <c>Cﾃｷﾄｼ</c> and every other <c>Cﾃｷﾄｼ2</c>.
    /// </summary>
    public static MapRelation RelationOf(GameState state, int province)
    {
        if (state.StoryStage == 0)
            return province is 33 or 34 ? MapRelation.Bordering : MapRelation.Other;
        var prov = state.Provs[province];
        if (prov.Owner == 1)
            return MapRelation.Own;
        foreach (var adj in (ReadOnlySpan<Sys3Int>)[prov.Adj1, prov.Adj2, prov.Adj3, prov.Adj4, prov.Adj5])
            if (adj != 0 && state.Provs[adj].Owner == 1)
                return MapRelation.Bordering;
        return MapRelation.Other;
    }

    /// <summary>
    /// The province's card on hover (docs/ui/command-screen.md, "The province card"): the three figures the
    /// city's window prints, each with what it does — "Economy: 20 (2000K Gold)" (the weekly income, in the
    /// 100K GOLD the budget counts), "Defense: 10 (Walls +20%)" (two points off the attackers' hit chance a
    /// point), "Battle Area: 180 troops" (the most that fight at once in a city battle). Ours and the
    /// provinces the player has seen (<see cref="GameState.IsProvinceKnown"/>) show them, the others "??",
    /// as the original's window does for a city not scouted (<c>Cﾃｷﾄｼ.ADV:26-43</c>).
    /// </summary>
    public static IReadOnlyList<string> CardOf(GameState state, int province)
    {
        var prov = state.Provs[province];
        if (!state.IsOwnProvince(province) && !state.IsProvinceKnown(province))
            return ["Economy: ??", "Defense: ??", "Battle Area: ??"];
        int economy = prov.Economy.Value, defense = prov.Defense.Value;
        return [$"Economy: {economy} ({economy * 100}K Gold)", $"Defense: {defense} (Walls +{defense * 2}%)",
            $"Battle Area: {prov.BattleArea.Value} troops"];
    }

    /// <summary>
    /// The search's badge: the floor the last search ended on while one is under way
    /// (<c>Dgn_State</c> 1, <c>Dgn_SavedFloor</c>) against the last floor — "B5/14" down a pit,
    /// "5F/30" up a tower —, "✓" once searched whole (state 2), none before or at an event place.
    /// </summary>
    public static string? ProgressOf(GameState state, int dungeon, DungeonKind kind, int maxFloor)
    {
        if (kind == DungeonKind.Event)
            return null;
        var dgn = state.Dgns[dungeon];
        if (dgn.State == 2)
            return "✓";
        if (dgn.State != 1 || dgn.SavedFloor <= 0)
            return null;
        int floor = dgn.SavedFloor.Value;
        return kind == DungeonKind.Tower ? $"{floor}F/{maxFloor}" : $"B{floor}/{maxFloor}";
    }

    /// <summary>
    /// Where the marker's still click goes on the 640×400 screen, the camera as the view's corner (the
    /// map loop's form while it waits, <see cref="GesturePoints.ViewCorner"/>); null when the marker's
    /// point is out of view or the script would read the click as another marker's — an open marker
    /// must open itself, a dungeon not ours must open nothing (STR-19: the script still moves
    /// <c>Prov_Cur</c>, as for the original's click).
    /// </summary>
    public static (int X, int Y)? ClickPoint(GameState state, MapMarker marker)
    {
        if (GesturePoints.OnScreen(state, (Sys3Int)marker.MapX, (Sys3Int)marker.MapY, centred: false) is not { } point)
            return null;
        var hit = GesturePoints.MapAt(state, point.X, point.Y, centred: false);
        bool right = marker.Open ? hit == new MapHit(marker.Kind, marker.Id) : hit is null;
        return right ? point : null;
    }

    /// <summary>A left click on the marker's plate: the still click at its point (<see cref="ClickPoint"/>); false when there is none.</summary>
    public static bool Press(GameState state, MapMarker marker, PointerScript pointer)
    {
        if (ClickPoint(state, marker) is not { } point)
            return false;
        pointer.Click(point.X, point.Y);
        return true;
    }

    /// <summary>The scenario's <c>’</c> as <c>'</c>: the Japanese font draws it full width ("Monsters’  Cave").</summary>
    private static string Plain(string name) => name.Replace('’', '\'');
}

/// <summary>
/// The form of the camera during the map wait (<c>CommandPhase.MapAsync</c>), which the state does not
/// record: centred on the view from <see cref="MapScreenEvent"/> until the first read with nothing
/// pressed (the release wait, after which the loop makes it the view's corner), the corner while the
/// loop waits, drags and coasts, centred again once a right button alone is read (the menu's way out,
/// <c>COMMAND.ADV:60-63</c>) — a still click restores it too, but the wait ends at once then.
/// </summary>
public sealed class MapCameraForm
{
    /// <summary>The map wait is on (<see cref="MapScreenEvent"/>).</summary>
    public bool Waiting { get; private set; }

    /// <summary>The camera is the view's corner: our markers may be placed from it.</summary>
    public bool Corner { get; private set; }

    /// <summary>A <see cref="MapScreenEvent"/>.</summary>
    public void OnMapWait(bool waiting)
    {
        Waiting = waiting;
        Corner = false;
    }

    /// <summary>A mouse read the script took (<c>IM</c>): its buttons (16 left, 32 right).</summary>
    public void OnMouseRead(int buttons)
    {
        if (!Waiting)
            return;
        if (!Corner && buttons == 0)
            Corner = true;
        else if (Corner && buttons == 32)
            Corner = false;
    }
}
