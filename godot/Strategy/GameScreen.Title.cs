using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The title on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §44-§45, PE6): while ｽﾀMENU polls
// the mouse our own screen covers the engine's picture (TitlePortraitView: the stone, the logo, a row for each button it
// draws); a row is a click in the middle of its button's box, a long press there the right click, through the title's own
// mouse reads. While it shows a button's description the screen stays, the rows hidden and the presses the key wait's, the
// picture under it centred on the logo (Device.PictureTop), so window 113 comes under the logo; at the requests slip it
// gives way to the picture in the room's middle.
public partial class GameScreen
{
    private const string TitleScene = "ｽﾀMENU.ADV";

    private TitlePortraitView _titleView = null!;
    private PointerScript _titlePointer = new();        // a row's click on its way to the title's poll

    private void ReadyTitleView(FacePictures faces)
    {
        _titleView = new TitlePortraitView(faces.Cgs);
        _titleView.Chosen += TitleRowChosen;
        _titleView.Described += TitleRowDescribed;
        _content.AddChild(_titleView);
        _content.MoveChild(_titleView, _messageView.GetIndex());     // under the message windows: a description over it
    }

    /// <summary>
    /// The buttons the title draws while it runs on the phone held upright with our interface, its boxes loaded; none
    /// otherwise (its Fortune Check, gallery and music room are files of their own, shown as they are).
    /// </summary>
    private IReadOnlyList<int> TitleButtons() =>
        PhonePortrait && InterfaceChoice.New && _presenter.State is { } s && _scenes?.RunningScenes is [.., TitleScene]
            && s.Buttons[(Sys3Int)PhoneTitlePortrait.Start].X2 != 0
            ? PhoneTitlePortrait.Buttons(s) : [];

    /// <summary>The title polls the mouse: not at a key wait, under a window or a menu.</summary>
    private bool TitlePolls(IReadOnlyList<int> buttons) =>
        buttons.Count > 0 && !_keyWaiting && !SkippingScene && _presenter.MessageWindow is null && _presenter.Menu is null;

    /// <summary>The title shows a button's description (window 113).</summary>
    private bool TitleDescribes(IReadOnlyList<int> buttons) => buttons.Count > 0 && _presenter.MessageWindow is not null;

    /// <summary>Where the title stands the picture held upright (on the logo, under a description), or null: the room's middle.</summary>
    private Func<PhoneFrame, float?>? TitlePictureTop(IReadOnlyList<int> buttons) =>
        TitleDescribes(buttons) && _titleView.LogoHeight is int logo
            ? f => PhoneTitlePortrait.DescriptionPictureTop(f, buttons.Count, logo) : null;

    /// <summary>Each frame: our screen with its rows while the title polls the mouse, without them under a description.</summary>
    private void UpdateTitleView()
    {
        var buttons = TitleButtons();
        bool polls = TitlePolls(buttons);
        if (polls || TitleDescribes(buttons))
        {
            _titleView.ShowTitle(buttons, polls);
            _titleView.Place();
        }
        else
            _titleView.HideTitle();
    }

    /// <summary>A row tapped: a click in the middle of its button's box, as the title loaded it.</summary>
    private void TitleRowChosen(int button)
    {
        if (_titlePointer.Pending > 0 || _presenter.State is not { } s)
            return;
        var (x, y) = PhoneTitlePortrait.Centre(s, button);
        _titlePointer.Click(x, y);
    }

    /// <summary>A row held: the right click in the middle of its button's box, its description.</summary>
    private void TitleRowDescribed(int button)
    {
        if (_titlePointer.Pending > 0 || _presenter.State is not { } s)
            return;
        var (x, y) = PhoneTitlePortrait.Centre(s, button);
        _titlePointer.RightClick(x, y);
    }
}
