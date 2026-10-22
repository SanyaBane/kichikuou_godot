using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Harem screen (docs/ui/pickers.md, mock-up H2) in the game: shown over <c>Sﾊｰﾚﾑ</c> (our
/// interface); Summon is a click on the girl's cell and End a click on the bar's End, through the screen's pointer
/// (<see cref="HaremRoster"/>). Unattended play keeps its own pointer and only sees the screen.
/// </summary>
public partial class GameScreen
{
    private HaremView _harem = null!;
    private PointerScript _haremPointer = new();        // the script's pointer while our Harem screen shows

    private void ReadyHarem(FacePictures faces)
    {
        _harem = new HaremView(GodotGlyphRenderer.Chosen, faces);
        _harem.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _harem.SummonPressed += HaremSummonPressed;
        _harem.EndPressed += HaremEndPressed;
        _content.AddChild(_harem);
    }

    /// <summary>
    /// Shows our Harem screen while <c>Sﾊｰﾚﾑ</c> is open with our interface on (not under a message window);
    /// places it over the picture and fills it from GameState. Returns whether it shows.
    /// </summary>
    private bool UpdateHaremScreen()
    {
        bool show = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Harem } && !_presenter.MessageWindowShown
            && !SkippingScene && _presenter.State is not null;
        if (show != _harem.Visible)
        {
            _harem.Visible = show;
            _haremPointer = new PointerScript();
        }
        if (!show)
            return false;
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _harem.Busy = _haremPointer.Pending > 0;
        _harem.Place(_view.Position + (size - drawn) / 2, scale * Surface.Scale);
        _harem.Refresh(_presenter.State!, _presenter.HaremGirls(_pickerMarks?.Visits));
        return true;
    }

    /// <summary>The script reads the pointer over our screen: a summon's or End's click, else at rest on the empty 51st cell (the picker hovers nobody).</summary>
    private MouseState HaremRead()
    {
        if (_haremPointer.Pending == 0)
        {
            var (x, y) = HaremRoster.Rest;
            _haremPointer.Move(x, y);
        }
        return _haremPointer.Next(out _);
    }

    private void HaremSummonPressed(int girl)
    {
        if (_haremPointer.Pending > 0 || girl == 0)
            return;
        GD.Print($"Kichikuou: Harem summons girl {girl}.");
        HaremRoster.Summon(girl, _haremPointer);
    }

    private void HaremEndPressed()
    {
        if (_haremPointer.Pending > 0)
            return;
        HaremRoster.End(_haremPointer);
    }
}
