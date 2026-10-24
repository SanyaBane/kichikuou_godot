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

    private bool _advisorSpeaks;

    /// <summary>The player answered <c>FIGT_B</c>'s menu (<c>:46-64</c>) with <paramref name="code"/>: Intercept (1) and Abandon City (3) close the screen.</summary>
    public void EnemyAttackAnswered(Sys3Int code)
    {
        if (code == 1 || code == 3)
            Attack = null;
    }

    private void ObserveEnemyAttack(ScriptRef text)
    {
        if (text.File != AttackAnnounced.File)
            return;
        if (text == AttackAnnounced && CoverEnemyAttack && State is { } s)
            Attack = EnemyAttack.Of(s, line => Read(line, []));
        if (Attack is null)
        {
            if (CoverEnemyAttack && UnseenLines.Contains(text.Line))
                HidesLine = true;
            return;
        }
        _advisorSpeaks = text.Line is >= AdvisorFirst and <= AdvisorLast;
        if (Scout is { } report)
            Attack = Attack with { Report = report };
    }

    /// <summary>A page of Maris's line is complete at its key wait: the screen shows it as her window prints it.</summary>
    private void NoteAdvisorPage()
    {
        if (!_advisorSpeaks || Attack is not { } attack || WindowModel.Of(_winFrame, _windowText, Surface.Color) is not { } page)
            return;
        Attack = attack with { AdvisorLines = [.. attack.AdvisorLines, .. page.Lines] };
    }
}
