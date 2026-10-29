using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The title on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22, PE2): while ｽﾀMENU runs the
// picture stands in the middle over a finger row for each button it draws (Device.PictureTop; so a button's description,
// window 113, follows it), the rows shown while it polls the mouse (TitleRowsView), the picture's own targets kept; a row
// is a click in the middle of its button's box, through the title's own mouse reads.
public partial class GameScreen
{
    private const string TitleScene = "ｽﾀMENU.ADV";

    private TitleRowsView _titleRows = null!;
    private PointerScript _titlePointer = new();        // a row's click on its way to the title's poll

    private void ReadyTitleRows()
    {
        _titleRows = new TitleRowsView();
        _titleRows.Chosen += TitleRowChosen;
        _content.AddChild(_titleRows);                  // under the Menu sheet: the Menu cell stays its own
    }

    /// <summary>
    /// The buttons the title draws while it runs on the phone held upright with our interface, its boxes loaded; none
    /// otherwise (its Fortune Check, gallery and music room are files of their own, shown as they are).
    /// </summary>
    private IReadOnlyList<int> TitleButtons() =>
        PhoneUpright && InterfaceChoice.New && _presenter.State is { } s && _scenes?.RunningScenes is [.., TitleScene]
            && s.Buttons[(Sys3Int)PhoneTitleUpright.Start].X2 != 0
            ? PhoneTitleUpright.Buttons(s) : [];

    /// <summary>Where the title stands the picture held upright (over its rows), or null: the room's middle.</summary>
    private static Func<PhoneFrame, float?>? TitlePictureTop(IReadOnlyList<int> buttons) =>
        buttons.Count > 0 ? f => PhoneTitleUpright.PictureTop(f, buttons.Count) : null;

    /// <summary>Each frame: the rows while the title polls the mouse — not at a key wait, under a window or a menu.</summary>
    private void UpdateTitleRows()
    {
        var buttons = TitleButtons();
        if (buttons.Count > 0 && !_keyWaiting && !SkippingScene && _presenter.MessageWindow is null && _presenter.Menu is null)
        {
            _titleRows.ShowRows(buttons);
            _titleRows.Place();
        }
        else
            _titleRows.HideRows();
    }

    /// <summary>A row tapped: a click in the middle of its button's box, as the title loaded it.</summary>
    private void TitleRowChosen(int button)
    {
        if (_titlePointer.Pending > 0 || _presenter.State is not { } s)
            return;
        var (x, y) = PhoneTitleUpright.Centre(s, button);
        _titlePointer.Click(x, y);
    }
}
