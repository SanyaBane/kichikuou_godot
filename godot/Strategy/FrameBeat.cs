using Godot;
using Kichikuou.Engine.Diagnostics;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// One of the frame watch's two nodes on the root (<see cref="HangWatch"/>): the <see cref="First"/>, processed before every
/// other node, gives the frame's beat and says when the application's frames stop on purpose; the other, after every
/// other node, marks the end of the nodes' processing, so a stall between that and the drawing lies in a node's <c>_Draw</c>.
/// </summary>
internal sealed partial class FrameBeat : Node
{
    public FrameWatch Watch { get; init; } = null!;
    public bool First { get; init; }

    public override void _Process(double delta)
    {
        if (First)
        {
            Watch.Beat();
            HangWatch.TestWhenDue();
        }
        else
        {
            Watch.At("nodes processed");
        }
    }

    public override void _Notification(int what)
    {
        if (!First)
            return;
        if (what == NotificationApplicationPaused)
            Watch.Pause();
        else if (what == NotificationApplicationResumed)
            Watch.Resume();
    }
}
