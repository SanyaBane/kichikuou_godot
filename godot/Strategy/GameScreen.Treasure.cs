using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, round V) in the game: shown over <c>Sｱｲﾃﾑ</c> (our interface),
/// under the message windows and menus; the script's refusals and questions show over it at the clicked cell
/// (<see cref="TreasureCallout"/>, round W), its Yes/No inside them. A cell's click, a tab, Back and End are the
/// picker's player's clicks through the screen's pointer
/// (<see cref="TreasureRoster"/>). Unattended play clicks through the screen with its own pointer
/// (<see cref="AutoTreasureAsync"/>).
/// </summary>
public partial class GameScreen
{
    private TreasureView _treasure = null!;
    private PointerScript _treasurePointer = new();     // the script's pointer while our Treasure screen shows
    private TreasureCallout _treasureCallout = null!;   // the script's refusals and questions at the clicked cell
    private int _treasureAsked;                         // the cell clicked last
    private IReadOnlyList<string>? _treasureConfirm;    // our own question up in the callout (ReplaceQuestion)

    /// <summary>The pointer the screen's clicks go to: unattended play's own, else the screen's.</summary>
    private PointerScript TreasurePointer => _options.Auto ? _pointer : _treasurePointer;

    private void ReadyTreasure(FacePictures faces)
    {
        _treasure = new TreasureView(GodotGlyphRenderer.Chosen, faces);
        _treasure.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _treasure.PickPressed += TreasureCellPressed;
        _treasure.TurnPressed += () => TreasureGesture(TreasureRoster.Turn);
        _treasure.BackPressed += () => TreasureGesture(TreasureRoster.Back);
        _treasure.EndPressed += () => TreasureGesture(TreasureRoster.End);
        _content.AddChild(_treasure);
        _content.MoveChild(_treasure, _messageView.GetIndex());        // the script's windows and menus over it
        _treasureCallout = new TreasureCallout(GodotGlyphRenderer.Chosen);
        _content.AddChild(_treasureCallout);
    }

    /// <summary>A cell's click, remembered as the cell the script's next refusal or question is about.</summary>
    private void TreasurePick(int number, PointerScript pointer)
    {
        _treasureAsked = number;
        TreasureRoster.Pick(number, pointer);
    }

    /// <summary>
    /// A cell pressed on the screen: to the picker, unless an item picked first would replace the unit's own — then our
    /// question first (<see cref="TreasureRoster.ReplaceQuestion"/>), in the callout at the cell, and the click only on Yes.
    /// </summary>
    private async void TreasureCellPressed(int number)
    {
        if (TreasurePointer.Pending > 0 || _treasureConfirm is not null || _presenter.State is not { } s)
            return;
        if (TreasureRoster.ReplaceQuestion(s, number) is not { } lines)
        {
            TreasureGesture(p => TreasurePick(number, p));
            return;
        }
        _treasureConfirm = lines;
        _treasureAsked = number;
        _treasureCallout.OpenMenu(["Yes", "No"]);
        UpdateTreasureCallout(null);
        int answer = 0;
        try
        {
            answer = await _treasureCallout.ChooseAsync();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _treasureConfirm = null;
            _treasureCallout.CloseMenu();
            UpdateTreasureCallout(null);
        }
        if (answer == 1 && _treasure.Visible)
            TreasureGesture(p => TreasurePick(number, p));
    }

    /// <summary>
    /// The script's window over our Treasure screen — a refusal, or the question under its Yes/No — as a callout at the cell
    /// clicked last (docs/ui/pickers.md, round W, W2) instead of our message window; returns whether it takes the window's
    /// place. The script prints the question a few frames before it opens the Yes/No, so the callout waits, hidden, for
    /// the key wait (a refusal) or the menu (a question) and appears at its whole size.
    /// </summary>
    private bool UpdateTreasureCallout(MessageWindow? window)
    {
        if (_treasureConfirm is { } own && _treasure.Visible)
        {
            ShowTreasureCallout(own, false);                        // our own question, its menu open
            _treasure.Asked = _treasureAsked;
            return false;
        }
        if (!TreasureCalloutFor(window))
        {
            _treasure.Asked = 0;
            _treasureCallout.Hide();
            return false;
        }
        if (_keyWaiting || _treasureCallout.MenuOpen)
        {
            // The picker's question in our words (the owner, 2026-10-27); its refusals keep the script's.
            var ours = _treasureCallout.MenuOpen && _presenter.State is { } s ? TreasureRoster.Question(s, _treasureAsked) : null;
            ShowTreasureCallout(ours ?? window!.Lines, _keyWaiting);
        }
        else
            _treasureCallout.Visible = false;                       // until the key wait or the menu, not its menu closed
        _treasure.Asked = _treasureCallout.Visible ? _treasureAsked : 0;
        return true;
    }

    /// <summary>
    /// The callout at the cell clicked last — in the touch mode beside the screen's button, the last tap (docs/ui/pickers.md,
    /// §6, the owner's X4); on the phone held landscape beside it at the phone's sizes.
    /// </summary>
    private void ShowTreasureCallout(IReadOnlyList<string> lines, bool waiting) =>
        _treasure.ShowCallout(_treasureCallout, lines, _treasureAsked, waiting, PictureArea());

    /// <summary>Whether <paramref name="window"/> shows as the callout: over our Treasure screen, after a cell's click.</summary>
    private bool TreasureCalloutFor(MessageWindow? window) => window is not null && _treasure.Visible && _treasureAsked > 0;

    /// <summary>
    /// Shows our Treasure screen while <c>Sｱｲﾃﾑ</c> is open with our interface on; places it over the picture and
    /// fills it from GameState. While a message window is up (a refusal's key wait) the clicks pass through it to the
    /// window. Returns whether it shows.
    /// </summary>
    private bool UpdateTreasureScreen()
    {
        bool show = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Items } && !SkippingScene && _presenter.State is not null;
        if (show != _treasure.Visible)
        {
            _treasure.Visible = show;
            if (show)
            {
                _treasure.LoadCrops();                                  // the owner's squares from Debug → Portraits
                _treasure.ShowHover(0);                                 // touch: no selection left from a visit before
            }
            _treasureAsked = 0;
            _treasurePointer = new PointerScript();
            var (x, y) = TreasureRoster.Rest;
            _treasurePointer.Move(x, y);                                // then where the last click was, as the player's pointer
        }
        if (!show)
            return false;
        var frame = Frame();
        _treasure.MouseFilter = _presenter.MessageWindowShown ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        _treasure.Busy = TreasurePointer.Pending > 0;
        _treasure.Place(frame.Origin, frame.Pixels);
        _treasure.Refresh(_presenter.State!);
        return true;
    }

    /// <summary>The script reads the pointer over our screen: the screen's clicks, else at rest where the last one was.</summary>
    private MouseState TreasureRead() => _treasurePointer.Next(out _);

    /// <summary>A click of the screen for the picker, unless one is still on its way.</summary>
    private void TreasureGesture(Action<PointerScript> gesture)
    {
        if (TreasurePointer.Pending > 0 || _treasureConfirm is not null)
            return;                                                 // a click on its way, or our question up
        gesture(TreasurePointer);
    }

    // ---------------------------------------------------------------- unattended play

    private PickerView? _autoTreasureFrom;
    private Queue<Func<Task>> _autoTreasureSteps = new();

    /// <summary>
    /// Unattended play on our Treasure screen: <see cref="AutoTreasureSteps"/> one at a time while no click is on its way,
    /// then End. False when the screen does not show (the original picker is played by <see cref="AutoItemSteps"/>).
    /// </summary>
    private async Task<bool> AutoTreasureAsync()
    {
        if (_presenter.Picker is not { Kind: PickerKind.Items } picker || !UpdateTreasureScreen() || _presenter.State is not { } s)
            return false;
        if (_pointer.Pending > 0)
            return true;
        if (_autoTreasureFrom != picker)
        {
            _autoTreasureFrom = picker;
            _autoTreasureSteps = AutoTreasureSteps(s);
        }
        if (_autoTreasureSteps.TryDequeue(out var step))
            await step();
        else
            TreasureRoster.End(_pointer);
        return true;
    }

    /// <summary>
    /// What the unattended player does on our Treasure screen, leaving the items where they were: the first character at
    /// status 1 or 2 holding an item that moves is picked ("Which item?"), the pick dropped with Back; in the items' view his
    /// item is clicked with no pick (the script asks, the first answer, Yes, takes it off), then the item given back — picked
    /// first and offered to another holder of such an item, whose cell asks our question, answered No, then to him (with
    /// nobody else, he is picked and then the item); last he is picked again and given his own item, which the script
    /// refuses. In a window with screen dumps on: <c>treasure</c>, <c>treasure-hover</c>, <c>treasure-which-item</c>,
    /// <c>treasure-items</c>, <c>treasure-question</c> (the callout's Yes/No), <c>treasure-unequipped</c>,
    /// <c>treasure-replace</c> (our question), <c>treasure-equipped</c>, <c>treasure-refusal</c>. With nobody holding such
    /// an item, only the first dumps, then End.
    /// </summary>
    private Queue<Func<Task>> AutoTreasureSteps(GameState s)
    {
        bool dumps = DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null;
        var steps = new Queue<Func<Task>>();
        void Act(Action act) => steps.Enqueue(() =>
        {
            act();
            return Task.CompletedTask;
        });
        void Dump(string name) => steps.Enqueue(async () =>
        {
            if (!dumps)
                return;
            UpdateTreasureScreen();                                 // a click's result: the step runs once the script read it
            await AutoDumpAsync(name);
        });
        void TurnTo(int view) => Act(() =>
        {
            if (_presenter.State is { } now && TreasureRoster.View(now) != view)
                TreasureRoster.Turn(_pointer);
        });
        int unit = Enumerable.Range(1, 50).FirstOrDefault(n => s.Units[n].Status.Value is 1 or 2
            && s.Units[n].Item.Value is var item && item != 0 && !TreasureRoster.Fixed(item));
        int held = unit == 0 ? 0 : s.Units[unit].Item.Value;
        int other = Enumerable.Range(1, 50).FirstOrDefault(n => n != unit && s.Units[n].Status.Value is 1 or 2
            && s.Units[n].Item.Value is var item && item != 0 && !TreasureRoster.Fixed(item));
        GD.Print($"Kichikuou: --auto opens our Treasure screen (week {s.Week.Value}; unit {unit}, item {held}).");
        Dump("treasure");
        if (unit == 0)
            return steps;
        TurnTo(0);
        Act(() => _treasure.ShowHover(unit));
        Dump("treasure-hover");
        Act(() =>
        {
            _treasure.ShowHover(0);
            TreasurePick(unit, _pointer);
        });
        Dump("treasure-which-item");
        Act(() => TreasureRoster.Back(_pointer));                   // the pick dropped: the characters again
        TurnTo(1);
        Act(() => _treasure.ShowHover(held));
        Dump("treasure-items");
        Act(() =>
        {
            _treasure.ShowHover(0);
            TreasurePick(held, _pointer);                           // asked; the menu's first answer is Yes
        });
        Dump("treasure-unequipped");
        if (other != 0)
        {
            // Given back item first: the other holder's cell asks (our question), No, then his own cell.
            TurnTo(1);
            Act(() => TreasurePick(held, _pointer));
            Act(() => TreasureCellPressed(other));
            Dump("treasure-replace");
            Act(() => _treasureCallout.Choose(2));
            Act(() => TreasurePick(unit, _pointer));
        }
        else
        {
            TurnTo(0);
            Act(() => TreasurePick(unit, _pointer));
            Act(() => TreasurePick(held, _pointer));
        }
        Dump("treasure-equipped");
        Act(() =>
        {
            if (_presenter.State is { } now && now.Units[unit].Item.Value != held)
                GD.PushError($"Kichikuou: --auto did not give item {held} back to unit {unit}.");
        });
        TurnTo(0);
        Act(() => TreasurePick(unit, _pointer));
        if (TouchMode.On)
        {
            // The touch mode greys the button instead (the owner's X6): his own item selected, nothing sent.
            Act(() => _treasure.ShowHover(held));
            Dump("treasure-refusal");
            Act(() =>
            {
                if (_presenter.State is { } now && TreasureRoster.Button(now, held) is not { Refusal: not null })
                    GD.PushError($"Kichikuou: --auto found the button for unit {unit}'s own item {held} not greyed.");
                _treasure.ShowHover(0);
            });
        }
        else
            Act(() => TreasurePick(held, _pointer));                // refused: "Already equipped." (its key wait dumps)
        Act(() =>
        {
            if (_presenter.State is { } now && TreasureRoster.Prompt(now) is not null)
                TreasureRoster.Back(_pointer);                      // a pick the refusal left
        });
        return steps;
    }
}
