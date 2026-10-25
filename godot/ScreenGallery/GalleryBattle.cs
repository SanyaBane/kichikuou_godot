using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>When in a replayed battle a moment falls: after an event shown, at a key wait, or at <c>FIGT_SEL</c>.</summary>
internal enum MomentKind { Event, Key, Commands }

/// <summary>
/// A moment of a replayed battle: how many events the presenter had been shown, what kind of moment it is, the event
/// just shown (<see cref="MomentKind.Event"/>) or the menu asked (<see cref="MomentKind.Commands"/>).
/// </summary>
internal sealed record BattleMoment(int Events, MomentKind Kind, ScriptEvent? Event = null, FigtSelMenu? Menu = null);

/// <summary>
/// A captured battle replayed into a presenter of its own and held at a moment (docs/ui/gallery.md, §6): the original
/// script run on the capture's inputs, as <c>BattleSceneTests</c> runs it — deterministic, synchronous (the replay's
/// inputs are answered at once), silent. The replay looks at the presenter at each moment; at the <c>n</c>th one
/// <c>chosen</c> picks, the event that follows is never shown nor answered, so the presenter stays as it was then and
/// the replay never finishes. Each entry replays into its own presenter: the board's pointer changes the scene's
/// focus, so a presenter shared by entries would show what the one before did.
/// </summary>
internal static class GalleryBattle
{
    /// <summary>
    /// The battle the board's entries show: an attack of story stage 1 over four rounds (the owner's capture run of
    /// 2026-10-10).
    /// </summary>
    public const string Capture = "20261010-213758-114-FIGT_A.golden";

    private static readonly Dictionary<string, GoldenCapture> Captures = [];

    /// <summary>
    /// A presenter held at the <paramref name=nth/> (from 1) moment <paramref name=chosen/> picks in
    /// <paramref name=file/>, and the moment; throws when the capture is missing or has no such moment.
    /// </summary>
    public static (GamePresenter Presenter, BattleMoment Moment) HeldAt(Func<GamePresenter> fresh,
        Func<GamePresenter, BattleMoment, bool> chosen, int nth = 1, string file = Capture)
    {
        if (!Captures.TryGetValue(file, out var capture))
        {
            string path = Path.Combine(GameData.GoldenDirectory, file);
            if (!File.Exists(path))
                throw new InvalidOperationException($"The capture {file} is not in the extract's golden folder (KICHIKUOU_EXTRACT_DIR).");
            Captures[file] = capture = GoldenCapture.Load(path);
        }
        var presenter = fresh();
        var view = new HoldingView(presenter);
        BattleMoment? hit = null;
        int found = 0;
        void Seen(BattleMoment moment)
        {
            if (hit is not null || !chosen(presenter, moment) || ++found < nth)
                return;
            hit = moment;
            view.HoldAt = moment.Events;
        }
        view.Seen = Seen;
        var answers = new ReplayAnswers(Key: key =>
        {
            Seen(new BattleMoment(view.Shown, MomentKind.Key));
            return key;
        });
        _ = GoldenReplay.RunAsync(capture, view: view, answers: answers,
            started: s =>
            {
                presenter.Bind(s);
                presenter.Show(new BattleScreenEvent(capture.Root, true));
            },
            commands: (menu, _) => Seen(new BattleMoment(view.Shown, MomentKind.Commands, Menu: menu)));
        if (hit is null || !view.Held)
            throw new InvalidOperationException($"{file} has {found} of the {nth} moments asked for.");
        return (presenter, hit);
    }

    /// <summary>
    /// Shows the replay's events to the presenter until <see cref="HoldAt"/> have been shown; the next one is never
    /// shown and never answered, so the replay stops there.
    /// </summary>
    private sealed class HoldingView(GamePresenter presenter) : IScriptView
    {
        public int Shown { get; private set; }

        public int HoldAt { get; set; } = -1;

        public bool Held { get; private set; }

        public Action<BattleMoment> Seen { get; set; } = _ => { };

        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            if (Held || Shown == HoldAt)
            {
                Held = true;
                return new TaskCompletionSource().Task;
            }
            presenter.Show(scriptEvent);
            Shown++;
            Seen(new BattleMoment(Shown, MomentKind.Event, scriptEvent));
            return Task.CompletedTask;
        }
    }
}
