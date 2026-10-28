using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Where the engine's 1280×800 picture — and our layouts over it — stands in a screen, and the factors our views take
/// from it (docs/ui/phone-build.md, §3, "One frame helper"): fitted into the screen's room and centred on a PC and a
/// tablet, the phone's frame (<see cref="PhoneFrame"/>) on a phone held landscape. <see cref="Area"/> is in the room's
/// coordinates; <see cref="Scale"/> is its units a pixel of the 1280×800 picture, <see cref="Pixels"/> a script pixel.
/// </summary>
internal readonly record struct ScreenFrame(Rect2 Area, float Scale)
{
    /// <summary>The picture's size: the script's 640×400 at the engine's 2×.</summary>
    private static readonly Vector2 Picture = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale;

    /// <summary>The room's units a script pixel (of the 640×400 screen).</summary>
    public float Pixels => Scale * Surface.Scale;

    /// <summary>The picture's top left corner.</summary>
    public Vector2 Origin => Area.Position;

    /// <summary>The picture in <paramref name="room"/> for the device: the phone's frame held landscape, else fitted.</summary>
    public static ScreenFrame In(Rect2 room)
    {
        if (Device.Layout != DeviceLayout.PhoneLandscape)
            return Fit(room);
        var frame = Device.Frame(room.Size).Frame;
        return Fit(new Rect2(room.Position + new Vector2(frame.X, frame.Y), frame.W, frame.H));
    }

    /// <summary>The picture scaled to fit <paramref name="room"/>, centred in it.</summary>
    public static ScreenFrame Fit(Rect2 room)
    {
        var size = room.Size;
        float scale = Math.Min(size.X / Picture.X, size.Y / Picture.Y);
        var drawn = Picture * scale;
        return new ScreenFrame(new Rect2(room.Position + (size - drawn) / 2, drawn), scale);
    }

    /// <summary>The script point under <paramref name="point"/> (the room's coordinates), unrounded and unclamped.</summary>
    public Vector2 ToScript(Vector2 point) => (point - Area.Position) / Pixels;

    /// <summary>The centre of script pixel <paramref name="script"/> in the room's coordinates (the inverse of <see cref="ToScript"/>).</summary>
    public Vector2 FromScript(Vector2I script) => Area.Position + (new Vector2(script.X, script.Y) + new Vector2(0.5f, 0.5f)) * Pixels;
}
