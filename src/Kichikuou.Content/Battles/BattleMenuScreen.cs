using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Battles;

/// <summary>
/// A script menu of the battle (<c>$label$ 'item' $ … ]</c>, <see cref="BattleMenu"/>) in
/// selection window 1: <c>B1 1, 250, 50, 150, 100, 1</c> (<c>ｽﾀｰﾄ.ADV:860-861</c>,
/// docs/battle/screen-layout.md §4.6). A left release on an item chooses it, a right click
/// cancels (<see cref="Result"/> = <c>null</c>).
/// </summary>
public sealed class BattleMenuScreen
{
    private readonly BattleMenu _menu;
    private readonly SelectionBox _box;
    private bool _leftHeld;

    /// <summary>Opens the menu at once (the engine first waits for the left button to be released).</summary>
    public BattleMenuScreen(BattlePresenter presenter, BattleMenu menu)
    {
        _menu = menu;
        var labels = menu.Options.Select(o => presenter.Read(o.Label, []).Replace("\n", "")).ToList();
        _box = new SelectionBox(presenter.Surface, presenter.Glyphs, 250, 50, 150, labels);
        _box.Open();
    }

    /// <summary>Whether the player has answered.</summary>
    public bool IsDone { get; private set; }

    /// <summary>The chosen code, or <c>null</c> for a cancel.</summary>
    public Sys3Int? Result { get; private set; }

    /// <summary>The mouse moved (script coordinates).</summary>
    public void MouseMove(int x, int y)
    {
        if (!IsDone)
            _box.Hover(x, y);
    }

    /// <summary>A button went down: the right one cancels at once.</summary>
    public void MouseDown(MouseButton button, int x, int y)
    {
        MouseMove(x, y);
        if (IsDone)
            return;
        if (button == MouseButton.Right)
        {
            _box.Close();
            IsDone = true;
            return;
        }
        _leftHeld = true;
    }

    /// <summary>A button went up: the left one over an item chooses it.</summary>
    public void MouseUp(MouseButton button, int x, int y)
    {
        MouseMove(x, y);
        if (IsDone || button != MouseButton.Left || !_leftHeld)
            return;
        _leftHeld = false;
        int item = _box.HitTest(x, y);
        if (item < 0)
            return;
        _box.Close();
        Result = _menu.Options[item].Code;
        IsDone = true;
    }
}
