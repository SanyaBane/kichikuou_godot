using System.Diagnostics;
using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The marks on Talk and the harem in the game (owner's decision 30, docs/ui/talk-marks.md §8 step 8): computed by the
/// session's dry run (<see cref="PickerMarks"/>) as <c>Sﾊｰﾚﾑ</c> or <c>Sﾌﾞｶ</c> opens, from the state at that event, and
/// handed to our Harem and Subordinates screens; the scenes read ahead at the map waits, one talk or visit a frame, so the
/// first picker opens without reading them. Our interface only (<c>--interface original</c> shows the original's pickers).
/// </summary>
public partial class GameScreen
{
    private PickerMarks? _pickerMarks;                  // the session's marks, for our screens
    private double _warmMilliseconds;

    /// <summary>A picker's event as it comes: our screens' marks computed when the harem or the Subordinates screen opens.</summary>
    private void MarkPicker(ScriptEvent scriptEvent)
    {
        if (scriptEvent is not PickerScreenEvent { Open: true, Kind: PickerKind.Harem or PickerKind.Subordinates } picker
            || !InterfaceChoice.New || _pickerMarks is not { } marks || _presenter.State is not { } state || _script is null)
            return;
        marks.OnPicker(picker, state, _script.NewScenesFirst);
        var shown = picker.Kind == PickerKind.Harem ? marks.Visits : marks.Talks;
        GD.Print($"Kichikuou: the {(picker.Kind == PickerKind.Harem ? "harem's" : "talks'")} marks in {marks.LastMilliseconds:F0} ms: "
            + (shown.Count == 0 ? "none" : string.Join(", ", shown.OrderBy(m => m.Key).Select(m => $"{m.Key} {m.Value}"))) + ".");
        foreach (string failure in marks.Failures)
            GD.PushWarning($"Kichikuou: no mark for {failure}");
    }

    /// <summary>The scenes read ahead while COMMAND's map waits (once a session; a frame's step is one talk or visit).</summary>
    private void WarmMarks()
    {
        if (_pickerMarks is not { Warming: true } marks || !InterfaceChoice.New || !_presenter.MapWaiting || _presenter.State is not { } state)
            return;
        var clock = Stopwatch.StartNew();
        bool left = marks.Warm(state);
        _warmMilliseconds += clock.Elapsed.TotalMilliseconds;
        if (!left)
            GD.Print($"Kichikuou: the talks' and visits' scenes read ahead in {_warmMilliseconds:F0} ms (over the map waits' frames).");
    }
}
