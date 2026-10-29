using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// COMMAND's map on the phone held upright (docs/ui/phone-build.md, step P2; docs/ui/phone.md, §3, PM1): while the screen
// shows the map (the map's room, _mapStrip), the status is a band on top, the map's whole height lies under it down to the
// bar (UprightMapView, our plates over it), Commands at the bar's right; the command panel and a province's menu are
// sheets over the map's foot. A tap on a plate, or near a marker, is the marker's click, which the script reads after
// the drag a player would make to bring it into its own view (MapMarkers.PressAnywhere).
public partial class GameScreen
{
    /// <summary>The status band and the map held upright, each frame: shown while the screen shows the map, else gone.</summary>
    private void UpdateUprightMap()
    {
        if (_mapStrip && _presenter.StatusStrip is { } shown && _presenter.State is { } state
            && _presenter.MapViewShown is { } corner && Device.Upright(_upMap) is { } f)
        {
            _stripClip.Position = Vector2.Zero;
            _stripClip.Size = _content.Size;
            _strip.Show(shown);
            _strip.Out = 0;
            _strip.Place(new Rect2(Vector2.Zero, _content.Size));          // its parent's whole area
            var band = _strip.UprightBand(f);
            _upMap.Show(state, corner);
            _upMap.Place(band);
            _commandView.UprightBand = band;
            _provinceView.UprightBand = band;
            return;
        }
        _strip.Close();
        if (_upMap.Visible)
            _upMap.Close();
    }

    /// <summary>A tap on the upright map: on a plate, its marker; else the nearest marker within 4 mm (PM1a); else nothing.</summary>
    private void UprightMapTapped(Vector2 at)
    {
        if (!_markers.Visible || _upMap.Phone is not { } f)
            return;
        var marker = _markers.PlateAt(_upMap.GetGlobalRect().Position + at)
            ?? PhoneMapUpright.MarkerAt(f, _upMap.View, _markers.Shown, at.X, at.Y);
        if (marker is not null)
            MarkerPressed(marker);
    }
}
