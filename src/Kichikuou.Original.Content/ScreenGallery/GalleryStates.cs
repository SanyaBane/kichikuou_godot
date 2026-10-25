using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.ScreenGallery;

/// <summary>A sample message window of the screen gallery: its dump's name, its kind, its lines as the script prints them.</summary>
public sealed record MessageSample(string Id, MessageKind Kind, IReadOnlyList<string> Lines, bool Read = false)
{
    /// <summary>The window, printed where <c>WAO_SUB</c> prints it.</summary>
    public MessageWindow Window() => MessageWindow.SampleOf(Kind, Lines, Read);
}

/// <summary>
/// The screen gallery's states (docs/ui/gallery.md, §4): edits of a copy of a kept save's state that put a screen of ours
/// into one of its states without playing to it, and the samples its windows show. No rule reads these states; each
/// edit is one the script makes on the way there (named at each), unless the entry using it says it is made up.
/// </summary>
public static class GalleryStates
{
    /// <summary>
    /// Every kind of our message windows, under the names unattended play dumps them by (CLAUDE.md, "Commands"). The
    /// lines are the gallery's own, not the game's.
    /// </summary>
    public static IReadOnlyList<MessageSample> Messages { get; } =
    [
        new("message-speech-left", MessageKind.SpeechLeft, ["The rebels are gathering in the", "south, King Rance."]),
        new("message-speech-right", MessageKind.SpeechRight, ["Then we strike before they are", "ready. Gather the troops!"]),
        new("message-thought-left", MessageKind.ThoughtLeft, ["(Hmm... something is not right", "about this...)"]),
        new("message-thought-right", MessageKind.ThoughtRight, ["(If only the king would listen", "to reason for once...)"]),
        new("message-narration", MessageKind.Narration, ["Three days later, the army", "reached the border."]),
        new("message-read", MessageKind.Narration, ["Read before: this text takes", "the already-read colour."], Read: true),
        new("message-place-title", MessageKind.PlaceTitle, ["Leazas Castle", "The Throne Room"]),
        new("message-cg-bar-speech", MessageKind.CgBar, ["Maris「The preparations are complete, King Rance.」"]),
        new("message-cg-bar-thought", MessageKind.CgBar, ["Rance（Gahahaha, this is going to be fun.）"]),
        new("message-cg-bar-narration", MessageKind.CgBar, ["The night passed quietly in the castle."]),
    ];

    /// <summary>The Treasure screen's view (<c>Ui_ItemViewMode</c>): 0 the characters, 1 the items, nothing picked.</summary>
    public static void TreasureView(GameState s, int view)
    {
        s.Ui_ItemViewMode = (Sys3Int)view;
        s.Ui_ItemUnitSel = 0;
        s.Ui_ItemSel = 0;
    }

    /// <summary>A unit picked first: <c>Sｱｲﾃﾑ</c> turns to the items and asks "Which item?" (<c>Ui_ItemUnitSel</c>).</summary>
    public static void TreasureUnitPicked(GameState s, int unit)
    {
        TreasureView(s, 1);
        s.Ui_ItemUnitSel = (Sys3Int)unit;
    }

    /// <summary>An item picked first: <c>Sｱｲﾃﾑ</c> turns to the characters and asks "Which character?" (<c>Ui_ItemSel</c>).</summary>
    public static void TreasureItemPicked(GameState s, int item)
    {
        TreasureView(s, 0);
        s.Ui_ItemSel = (Sys3Int)item;
    }

    /// <summary>The units at status 1 or 2 holding an item that moves, in their order (none: an empty list).</summary>
    public static IReadOnlyList<int> MovableHolders(GameState s) =>
        [.. Enumerable.Range(1, 50).Where(n => s.Units[n].Status.Value is 1 or 2
            && s.Units[n].Item.Value is var item && item != 0 && !TreasureRoster.Fixed(item))];

    /// <summary>The unit at status 1 with the fewest troops of its maximum, under it (0: none) — Replenish's likely one.</summary>
    public static int NeediestUnit(GameState s) =>
        Enumerable.Range(1, 50)
            .Where(n => s.Units[n].Status.Value == 1 && s.Units[n].Troops < s.Units[n].MaxTroops)
            .OrderBy(n => (double)s.Units[n].Troops.Value / s.Units[n].MaxTroops.Value)
            .FirstOrDefault();
}
