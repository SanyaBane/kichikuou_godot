using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// A moment of the title (<c>ｽﾀMENU</c>) the gallery holds it at: <see cref="Booted"/> edits the state after the boot (a
/// clear), <see cref="Draws"/> are the title's <c>Y 4</c> draws in order (then 1 each), <see cref="Gesture"/> queues what
/// the pointer does once the title polls it with its buttons loaded (its points from the state's own boxes,
/// <see cref="GalleryTitle.Centre"/>), and <see cref="AtKey"/> holds it at the key wait after the gesture instead of at
/// the next poll.
/// </summary>
internal sealed record TitleMoment(
    Action<GameState>? Booted = null, int[]? Draws = null, Action<PointerScript, GameState>? Gesture = null, bool AtKey = false);

/// <summary>
/// The title run from the player's files into a presenter of its own and held at a moment (docs/ui/gallery.md, §7
/// step 7): a fresh launch's boot (<c>SET.ADV</c>, no save) and <c>ｽﾀMENU</c> through <see cref="GameSession"/>, as the game
/// opens it, its logo's pauses answered at once — deterministic, synchronous, silent. The run is held at the title's mouse
/// poll once its pointer is at rest (the hover drawn), or at the key wait <see cref="TitleMoment.AtKey"/> asks for: that
/// input is never answered, so the presenter stays as it was then. The presenter masks the event CGs as the agent's
/// dumps do and leaves the message windows to ours, as our interface does.
/// </summary>
internal static class GalleryTitle
{
    /// <summary>The title's buttons (<c>ｽﾀMENU.ADV:805</c>; docs/strategy/survey-w4.md, §3.2): the requests slip, Start.</summary>
    public const int Requests = 3, NewGame = 6;

    private static readonly Lazy<ScenarioScripts> Scripts = new(() => new ScenarioScripts(GameData.ScenarioDirectory));

    /// <summary>The scene files parsed once for every title entry (a session reads only through it).</summary>
    private static readonly Lazy<CachedSceneSource> Scenes = new(() => new CachedSceneSource(Scripts.Value));

    /// <summary>The middle of <paramref name="button"/>'s box as the title loaded it.</summary>
    public static (int X, int Y) Centre(GameState s, int button)
    {
        var b = s.Buttons[button];
        return ((b.X1 + b.W / 2).Value, (b.Y1 + b.H / 2).Value);
    }

    /// <summary>A presenter of <paramref name="fresh"/> held at <paramref name="moment"/>; throws when the title never reaches it.</summary>
    public static GamePresenter HeldAt(Func<GamePresenter> fresh, TitleMoment moment)
    {
        var presenter = fresh();
        presenter.MaskEventCgs = true;                                  // as the agent's dumps (CLAUDE.md)
        presenter.CoverMessages = true;                                 // our message windows draw the windows
        var state = new GameState();
        presenter.Bind(state);
        var input = new HoldingInput(state, moment);
        var session = new GameSession(state, new Draws(moment.Draws ?? []), new View(presenter), input, Scripts.Value, Scenes.Value,
            new MemorySaveStore(), new NoFiles());
        var boot = session.BootAsync();
        if (!boot.IsCompleted)
            throw new InvalidOperationException("The boot waited for an input.");
        boot.GetAwaiter().GetResult();                                  // its exception, if it failed
        moment.Booted?.Invoke(state);
        var title = session.TitleAsync();
        if (title.IsFaulted)
            title.GetAwaiter().GetResult();
        if (!input.Held)
            throw new InvalidOperationException("The title ended before its moment.");
        if (moment.AtKey)
            presenter.Flush();                                          // as GameScreen at a key wait
        return presenter;
    }

    /// <summary>The script's events shown to the presenter, at once.</summary>
    private sealed class View(GamePresenter presenter) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            presenter.Show(scriptEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>The draws asked for in order, then 1 each (a value above the range would be no draw of it).</summary>
    private sealed class Draws(int[] values) : IScriptRandom
    {
        private int _next;

        public Sys3Int Next(Sys3Int n)
        {
            if (n.Value <= 1)
                return n;
            int value = _next < values.Length ? values[_next++] : 1;
            return (Sys3Int)Math.Clamp(value, 1, n.Value);
        }
    }

    /// <summary>The title reaches no file the port does not run on these moments.</summary>
    private sealed class NoFiles : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) =>
            throw new InvalidOperationException($"The title called {file}, which the gallery does not run.");
    }

    /// <summary>
    /// Key waits and pauses answered at once, the pointer from a <see cref="PointerScript"/>: the moment's gesture queued once
    /// the title polls with its boxes loaded; held — never answered — at the poll after it, the pointer at rest a few reads
    /// (the hover drawn), or at the first key wait after it (<see cref="TitleMoment.AtKey"/>).
    /// </summary>
    private sealed class HoldingInput(GameState state, TitleMoment moment) : IGameInput
    {
        /// <summary>Reads at rest after the gesture before the poll is held: the release loop's and a few of the hover's.</summary>
        private const int RestReads = 4;

        /// <summary>Reads after which a moment that never comes is an error, not a frozen gallery.</summary>
        private const int MaxReads = 20_000;

        private readonly PointerScript _pointer = new();
        private bool _queued;
        private int _rest, _reads;

        public bool Held { get; private set; }

        /// <summary>The title has loaded its boxes (<c>ｽﾀMENU.ADV:42-50</c>) and the gesture, if any, is done.</summary>
        private bool Ready => _queued && _pointer.Pending == 0;

        public Task<Sys3Int> WaitForKeyAsync()
        {
            _pointer.Interrupt();
            if (moment.AtKey && Ready)
                return Hold<Sys3Int>();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            _pointer.Interrupt();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<MouseState> GetMouseAsync()
        {
            if (++_reads > MaxReads)
                throw new InvalidOperationException($"The title polled the mouse {MaxReads} times without reaching its moment.");
            if (!_queued && state.Buttons[NewGame].X2 != 0)
            {
                _queued = true;
                moment.Gesture?.Invoke(_pointer, state);
            }
            bool ready = Ready;
            var m = _pointer.Next(out bool read);
            _rest = ready && !read ? _rest + 1 : 0;
            if (!moment.AtKey && _rest > RestReads)
                return Hold<MouseState>();
            return Task.FromResult(m);
        }

        private Task<T> Hold<T>()
        {
            Held = true;
            return new TaskCompletionSource<T>().Task;
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult(shape);

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) =>
            throw new InvalidOperationException($"The title opened a menu: {string.Join(", ", items.Select(item => item.At))}.");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)28, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)4]);

        public Task ResetTimerAsync(Sys3Int value) => Task.CompletedTask;

        public Task<Sys3Int> GetTimerAsync(int mode) => Task.FromResult(Sys3Int.Zero);

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) =>
            throw new InvalidOperationException("The title fought a battle.");

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("The title fought a battle.");
    }
}
