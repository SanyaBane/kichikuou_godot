using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

// All Data on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23, LG2m and LG2): our sides
// answer its menus (AllDataView), and while a graph waits for the click a tap of theirs becomes the original's clicks, the
// menu that follows answered unseen.
public partial class GameScreen
{
    private AllDataView _dataView = null!;
    private PointerScript _dataPointer = new();         // a tap of the sides' clicks on their way to the graph's wait
    private int? _dataAnswer;                           // and the answer they give the menu that follows

    private void ReadyData()
    {
        _dataView = new AllDataView(GodotGlyphRenderer.Chosen);
        _dataView.Tapped += DataTapped;
        _content.AddChild(_dataView);                   // under the Menu sheet: the Menu cell stays its own
    }

    /// <summary>Whether All Data's screen is up on the phone held landscape, with our interface.</summary>
    private bool PhoneData => PhoneLandscape && InterfaceChoice.New && _presenter.Data != DataView.Closed;

    /// <summary>
    /// Each frame: over a graph the sides show (its figures ours, the presenter's left off), over the map's view with no
    /// menu nothing; a menu of ours stays as it is until answered.
    /// </summary>
    private void UpdateDataView()
    {
        bool graph = PhoneData && _presenter.Data != DataView.Map;
        _presenter.DataFiguresCovered = graph;
        if (graph && !SkippingScene)
            _dataView.ShowGraph(AllDataView.Sides.Of(_presenter));
        else
            _dataView.HideSides();
        if (_dataView.Visible)
            _dataView.Place();
    }

    /// <summary>
    /// A tap of the sides while a graph waits: Back the left click that leaves it, a chip the right click that opens the
    /// scale menu and the chip's scale its answer, another graph's cell the left click and that graph the menu's answer.
    /// </summary>
    private void DataTapped(int? graph, int? scale)
    {
        if (_dataPointer.Pending > 0 || _dataAnswer is not null)
            return;
        var (x, y) = (Surface.ScreenWidth / 2, Surface.ScreenHeight / 2);
        if (scale is int chip)
        {
            _dataAnswer = chip + 1;
            _dataPointer.RightClick(x, y);
        }
        else
        {
            _dataAnswer = graph + 1;
            _dataPointer.Click(x, y);
        }
    }

    /// <summary>The answer a tap of the sides gave the All Data menu now open, or null.</summary>
    private int? DataMenuAnswer(IReadOnlyList<SceneMenuItem> items)
    {
        if (_dataAnswer is not int answer || _presenter.Data == DataView.Closed)
            return null;
        _dataAnswer = null;
        return answer <= items.Count ? answer : null;
    }

    /// <summary>The menu of graphs (over the map's view) or the scale menu (over a graph) our sides answer, or null.</summary>
    private AllDataView.Mode? DataMenuMode() =>
        !PhoneData ? null : _presenter.Data == DataView.Map ? AllDataView.Mode.Menu : AllDataView.Mode.Scale;

    /// <summary>Opens our sides over an All Data menu (the presenter's menu box not drawn).</summary>
    private void OpenDataMenu(AllDataView.Mode mode)
    {
        _presenter.SceneMenuCovered = true;
        _presenter.Invalidate();
        _dataPointer = new PointerScript();
        _dataAnswer = null;
        _dataView.OpenMenu(mode, AllDataView.Sides.Of(_presenter));
        _dataView.Place();
    }

    /// <summary>Our sides' answer to an All Data menu; a graph's cell over the scale menu answers the next menu too.</summary>
    private async Task<int> DataAnswerAsync()
    {
        var (choice, then) = await _dataView.ChooseAsync();
        _dataAnswer = then;
        return choice;
    }
}
