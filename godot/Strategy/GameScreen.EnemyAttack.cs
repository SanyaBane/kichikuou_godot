using Godot;
using Kichikuou.Engine.Sys3;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Combat;

namespace Kichikuou.Game.Strategy;

// Our screen for an enemy attack (docs/ui/enemy-attack.md, §6): FIGT_B's menu and its ninja menu answered by it.
public partial class GameScreen
{
    private EnemyAttackView _attackView = null!;

    /// <summary>The ninja our screen's list chose: the answer to the ninja menu that follows Ninja Scout (0: none).</summary>
    private int _attackNinja;

    private bool _attackDumped, _attackExercised, _attackBackDumped;

    /// <summary>When unattended play with <c>--scout</c> left the deployment screen by Back (<c>Time.GetTicksMsec</c>), once a launch.</summary>
    private ulong? _autoBackAt;

    private void ReadyEnemyAttack(FacePictures faces)
    {
        _attackView = new EnemyAttackView(GodotGlyphRenderer.Chosen, faces);
        _attackView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _content.AddChild(_attackView);
    }

    /// <summary>Our screen shows while the battle presenter holds an attack (<see cref="Kichikuou.Original.Content.Battles.BattlePresenter.Attack"/>), in our interface.</summary>
    private bool EnemyAttackShown => InterfaceChoice.New && _presenter.Battle.Attack is not null && _presenter.State is not null;

    private void UpdateEnemyAttack()
    {
        if (!EnemyAttackShown)
        {
            if (_attackView.Visible)
                _attackView.Close();
            return;
        }
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _attackView.Show(_presenter.Battle.Attack!);
        _attackView.Place(_view.Position + (size - drawn) / 2, scale);
    }

    /// <summary>
    /// The key waits our screen answers at once, as by a click (RetVal is kept): every one while it shows (the
    /// announcement, the ninjas' lead-in, the way to the report and the report, whose army fills the screen's places) and
    /// those of the lines it hides after it (Rance's at Intercept, Plains and City, the six of Abandon City).
    /// </summary>
    private bool EnemyAttackAnswersKey =>
        InterfaceChoice.New && _presenter.State is not null && (_presenter.Battle.Attack is not null || _presenter.Battle.HidesLine);

    /// <summary>Whether our screen answers <paramref name="menu"/>: FIGT_B's while it shows, the ninja menu after its list chose one.</summary>
    private bool EnemyAttackAnswers(BattleMenu menu) => menu.Kind switch
    {
        BattleMenuKind.EnemyAttackResponse => EnemyAttackShown,
        BattleMenuKind.NinjaScout => InterfaceChoice.New && _attackNinja != 0 && menu.Allows((Sys3Int)_attackNinja),
        _ => false,
    };

    private async Task<Sys3Int?> EnemyAttackChooseAsync(BattleMenu menu)
    {
        if (menu.Kind == BattleMenuKind.NinjaScout)
        {
            var ninja = (Sys3Int)_attackNinja;
            _attackNinja = 0;
            return ninja;
        }
        UpdateEnemyAttack();
        if (_autoBackAt is { } backAt && !_attackBackDumped)
            GD.Print($"Kichikuou: --auto is back on the attack screen {Time.GetTicksMsec() - backAt} ms after Back.");
        var choosing = _attackView.ChooseAsync();
        if (_options.Auto)
            _ = AutoEnemyAttackAsync();
        EnemyAttackView.Answer answer;
        try
        {
            answer = await choosing;
        }
        catch (TaskCanceledException)
        {
            return null;                                                 // the screen closed: the script shows the menu again
        }
        if (answer.Code == 2)
            _attackNinja = answer.Ninja;
        _presenter.Battle.EnemyAttackAnswered((Sys3Int)answer.Code);
        UpdateEnemyAttack();
        return (Sys3Int)answer.Code;
    }

    /// <summary>
    /// Unattended play: dumps the screen once a launch (<c>enemy-attack</c>, a window with <c>--shots</c>); with
    /// <c>--scout</c> opens the list (<c>enemy-attack-ninjas</c>) and sends the first free ninja, then, back from the report
    /// (<c>enemy-attack-scouted</c>), asks Abandon City (<c>enemy-attack-abandon</c>), answers Keep and intercepts; after a
    /// stopped deployment picker abandons the city; else intercepts.
    /// </summary>
    private async Task AutoEnemyAttackAsync()
    {
        await DelayAsync(_options.Fast ? 0 : 0.5);
        await NextFrameAsync();
        var view = _attackView;
        if (!view.Choosing || view.Attack is not { } attack)
            return;
        bool dumps = DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null;
        if (dumps && !_attackDumped)
        {
            _attackDumped = true;
            await AutoDumpAsync("enemy-attack");
        }
        if (_autoBackAt is not null && !_attackBackDumped)
        {
            _attackBackDumped = true;                                   // back from the deployment screen, the report kept
            if (dumps)
                await AutoDumpAsync("enemy-attack-back");
        }
        if (_autoAbandon)
        {
            _autoAbandon = false;
            view.Choose(3);
            await NextFrameAsync();
            view.AnswerAbandon(true);
            return;
        }
        if (_options.AutoScout && !_autoScouted && attack.CanScout)
        {
            _autoScouted = true;
            view.SetList(true);
            if (dumps)
                await AutoDumpAsync("enemy-attack-ninjas");
            view.Choose(2, attack.Ninjas.First(n => n.Free).Code);
            return;
        }
        if (attack.Report is not null && !_attackExercised)
        {
            _attackExercised = true;
            if (dumps)
                await AutoDumpAsync("enemy-attack-scouted");
            view.Choose(3);
            await NextFrameAsync();
            if (dumps)
                await AutoDumpAsync("enemy-attack-abandon");
            view.AnswerAbandon(false);
            await NextFrameAsync();
        }
        view.Choose(1);
    }
}
