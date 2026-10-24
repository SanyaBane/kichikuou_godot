using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using WindowModel = Kichikuou.Original.Content.Strategy.MessageWindow;

namespace Kichikuou.Original.Content.Battles;

// An enemy attack on a province of ours as our screen shows it (docs/ui/enemy-attack.md, §6).
public sealed partial class BattlePresenter
{
    /// <summary>The announcement's first line (<c>FIGT_B.ADV:20</c>, "The &lt;army&gt; appeared around &lt;province&gt;.").</summary>
    private static readonly ScriptRef AttackAnnounced = new("FIGT_B.ADV", 20);

    /// <summary>Maris's lines of the announcement (<c>:35</c>, <c>:43</c>).</summary>
    private const int AdvisorFirst = 35, AdvisorLast = 45;

    /// <summary>
    /// The lines our interface plays unseen after the screen has closed (owner, 2026-10-24): Rance at Intercept (<c>:68</c>,
    /// <c>:73</c>), Plains (<c>:171</c>, <c>:174</c>) and City (<c>:196</c>, <c>:203</c>; Maris's "Understood.", <c>:214</c>),
    /// and the six of Abandon City (<c>:737-766</c>).
    /// </summary>
    private static readonly HashSet<int> UnseenLines = [68, 73, 171, 174, 196, 203, 214, 737, 742, 747, 754, 759, 766];

    /// <summary>Rance's first lines at Plains (<c>:171</c>) and City (<c>:196</c>): the party is on its way.</summary>
    private const int PlainsFirst = 171, CityFirst = 196;

    /// <summary>Whether our interface shows its own screen for an enemy attack (set by the UI from its interface).</summary>
    public bool CoverEnemyAttack { get; set; }

    /// <summary>
    /// The attack while our screen shows it: from the announcement (<c>:20</c>) to the menu's answer Intercept or Abandon
    /// City (<see cref="EnemyAttackAnswered"/>), through a scouting and back; null otherwise or without
    /// <see cref="CoverEnemyAttack"/>. While it is set, every key wait is the screen's to answer at once and no message
    /// window of ours shows.
    /// </summary>
    public EnemyAttack? Attack { get; private set; }

    /// <summary>Whether one of the <see cref="UnseenLines"/> is in its window, until the window closes: its key waits are answered at once.</summary>
    public bool HidesLine { get; private set; }

    /// <summary>
    /// Our screen is back from a deployment picker the player left by Back (Stop, 503; owner, 2026-10-24): from the map
    /// redrawn after the picker (<c>:44</c>) to the announcement's first line (<c>:20</c>) the script announces again
    /// under it — the map, its scroll and the province's blink (<c>:7-14</c>) take no time and the blink's sound is not played.
    /// </summary>
    public bool ReturnsToAttack { get; private set; }

    /// <summary>The attack the player intercepted, until the picker's answer: shown again at once after Back, its report kept after a refusal.</summary>
    private EnemyAttack? _intercepted;

    private bool _advisorSpeaks;

    /// <summary>The player answered <c>FIGT_B</c>'s menu (<c>:46-64</c>) with <paramref name="code"/>: Intercept (1) and Abandon City (3) close the screen.</summary>
    public void EnemyAttackAnswered(Sys3Int code)
    {
        if (code == 1)
            _intercepted = Attack;
        if (code == 1 || code == 3)
            Attack = null;
    }

    /// <summary>The map redrawn after the deployment picker (<c>FIGT_B.ADV:44</c>, <c>Tmp0621</c> its code): after Back our screen comes back at once.</summary>
    private void ObserveAttackMap(int mode)
    {
        if (mode != 0 || _intercepted is not { } intercepted || !CoverEnemyAttack || State is not { } s || s.Tmp0621 != 503)
            return;
        Attack = intercepted;
        ReturnsToAttack = true;
    }

    private void ObserveEnemyAttack(ScriptRef text)
    {
        if (text.File != AttackAnnounced.File)
            return;
        if (text == AttackAnnounced && CoverEnemyAttack && State is { } s)
        {
            // Back from the picker, or after a refusal: the army as the ninja reported it stays on the screen.
            var reported = (Attack ?? _intercepted)?.Report;
            Attack = EnemyAttack.Of(s, line => Read(line, [])) with { Report = reported };
            ReturnsToAttack = false;
            _intercepted = null;
        }
        if (text.Line is PlainsFirst or CityFirst)
            _intercepted = null;                                // the party is sent
        if (Attack is null)
        {
            if (CoverEnemyAttack && UnseenLines.Contains(text.Line))
                HidesLine = true;
            return;
        }
        _advisorSpeaks = text.Line is >= AdvisorFirst and <= AdvisorLast;
        // The report, and the ninja it spent (FIGT_B.ADV:394 sets her flag first): the menu comes back without an announcement.
        if (Scout is { } report && State is { } scouted)
            Attack = Attack with { Report = report, Ninjas = EnemyAttack.Of(scouted, line => Read(line, [])).Ninjas };
    }

    /// <summary>A page of Maris's line is complete at its key wait: the screen shows it as her window prints it.</summary>
    private void NoteAdvisorPage()
    {
        if (!_advisorSpeaks || Attack is not { } attack || WindowModel.Of(_winFrame, _windowText, Surface.Color) is not { } page)
            return;
        Attack = attack with { AdvisorLines = [.. attack.AdvisorLines, .. page.Lines] };
    }
}
