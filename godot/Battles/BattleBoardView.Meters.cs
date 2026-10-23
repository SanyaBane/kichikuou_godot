using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

// The cards' troops and HP run to a new value instead of jumping to it (owner, 2026-10-14; docs/ui/battle-screen.md,
// "The cards' counters"): the number and the bar together, eased out, in Option → Battle → "Cards: counters",
// from the moment the blow lands (BattleStrike) — an exchange's blow in the steps of the scene's counters (ExchangeCountersView,
// owner 2026-10-16); a loss leaves a light trail on the bar that follows the run and then
// drains; at once while skipping, with --fast, when another unit takes the place and when the screen is shown again.
internal sealed partial class BattleBoardView
{
    /// <summary>What a card's line shows: its troops or its HP.</summary>
    private enum Gauge { Troops, Hp }

    // The damage trail: it stays where the loss began while the counter runs, a little longer, then drains (ease in).
    private const double TrailHoldSeconds = 0.2, TrailDrainSeconds = 0.35;
    private const float TrailLight = 0.7f;                          // the bar's colour this far toward white

    /// <summary>
    /// One counter of a card: the value shown, running from <see cref="From"/> to <see cref="To"/> since
    /// <see cref="Start"/>, and the damage trail behind it; it outlives the card's controls, which are built again
    /// whenever the card changes.
    /// </summary>
    private sealed class Meter
    {
        public int Record;
        public float From, To, Shown;
        public float TrailFrom, Trail;
        public ulong Start;
        public double Delay, Seconds;               // the run waits Delay, then takes Seconds
        public bool Even;                           // in the scene's steps (an exchange's blow), else eased out
        public int Max;
        public string Prefix = "";
        public Label? Label;
        public Control? TrailBar, Fill;

        public bool Running => Shown != To || Trail != To;

        /// <summary>The value <paramref name="value"/> at once, no trail.</summary>
        public void Jump(float value) => From = To = Shown = TrailFrom = Trail = value;

        public void Apply()
        {
            float Share(float v) => Max > 0 ? Math.Clamp(v / Max, 0, 1) : 0;
            if (Label is not null && GodotObject.IsInstanceValid(Label))
                Label.Text = $"{Prefix} {Figures.Number((int)Math.Round(Shown))}/{Figures.Number(Max)}";
            if (Fill is not null && GodotObject.IsInstanceValid(Fill))
                Fill.AnchorRight = Share(Shown);
            if (TrailBar is not null && GodotObject.IsInstanceValid(TrailBar))
                TrailBar.AnchorRight = Share(Math.Max(Trail, Shown));
        }
    }

    private readonly Dictionary<(BattlePlace, Gauge), Meter> _meters = [];
    private BattleStrike? _strike;                  // the blow the cards show now
    private bool _metersInstant = true;

    /// <summary>How long a counter runs to its new value (Option → Battle, 0.6 s by default).</summary>
    private static double MeterSeconds => BattleScreenSettings.Get(BattleScreenSettings.MeterSeconds);

    /// <summary>
    /// The counter of <paramref name="gauge"/> on <paramref name="card"/>, bound to the line and the bar just built:
    /// a new value starts a run from the value shown now (a loss keeps the trail from where it stands, or starts it
    /// there); another unit in the place, or an instant refresh, shows it at once.
    /// </summary>
    private void Track(BattleCard card, Gauge gauge, Label label, Control trail, Control fill)
    {
        int value = gauge == Gauge.Troops ? card.Troops : card.Hp;
        int max = gauge == Gauge.Troops ? card.MaxTroops : card.MaxHp;
        var key = (card.Place, gauge);
        if (!_meters.TryGetValue(key, out var meter) || meter.Record != card.Record || _metersInstant || MeterSeconds <= 0)
        {
            meter = new Meter { Record = card.Record };
            meter.Jump(value);
            _meters[key] = meter;
        }
        else if (value != meter.To)
        {
            (meter.From, meter.To, meter.Start) = (meter.Shown, value, Time.GetTicksMsec());
            bool blow = _strike is { } k && card.Record == (card.Place.Side == BattleSide.Player ? k.Unit : k.Cmdr);
            (meter.Delay, meter.Seconds, meter.Even) = blow
                ? (ExchangeCountersView.Delay, ExchangeCountersView.RunSeconds, true)    // with the scene's counters
                : (0, MeterSeconds, false);
            meter.TrailFrom = meter.Trail = value < meter.Shown ? Math.Max(meter.Trail, meter.Shown) : meter.Shown;
        }
        (meter.Max, meter.Prefix, meter.Label, meter.TrailBar, meter.Fill) =
            (max, gauge == Gauge.Troops ? "Troops" : "HP", label, trail, fill);
        meter.Apply();
    }

    /// <summary>The counters that run go on; while skipping they reach their values at once.</summary>
    private void StepMeters()
    {
        ulong now = Time.GetTicksMsec();
        foreach (var meter in _meters.Values)
        {
            if (!meter.Running)
                continue;
            double seconds = meter.Seconds;
            if (_metersInstant || seconds <= 0)
            {
                meter.Jump(meter.To);
                meter.Apply();
                continue;
            }
            double elapsed = (now - meter.Start) / 1000.0;
            double t = meter.Even ? ExchangeCountersView.Share(elapsed) : Math.Min(1, elapsed / seconds);   // the scene's steps
            double eased = meter.Even ? t : 1 - (1 - t) * (1 - t);
            meter.Shown = t >= 1 ? meter.To : (float)(meter.From + (meter.To - meter.From) * eased);
            double drain = Math.Clamp((elapsed - meter.Delay - seconds - TrailHoldSeconds) / TrailDrainSeconds, 0, 1);
            meter.Trail = drain >= 1 ? meter.To : (float)(meter.TrailFrom + (meter.To - meter.TrailFrom) * drain * drain);
            meter.Apply();
        }
    }
}
