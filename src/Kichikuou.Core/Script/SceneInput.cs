using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

/// <summary>
/// The engine inputs the scene runtime (<see cref="SceneRunner"/>) needs beyond
/// <see cref="IScriptInput"/>: menus, the mouse, and the engine's clock and devices. Each method
/// models one command and answers with what the script receives; the golden harness records
/// every one of them (<c>menu</c>, <c>mouse</c>, <c>date</c>, <c>timer</c>, <c>cdpos</c>).
/// </summary>
public interface ISceneInput : IScriptInput
{
    /// <summary>
    /// <c>]</c> — shows the registered items and returns the chosen one, 1-based, or 0 when the
    /// player cancels (right click), as the harness records it (<c>golden_on_menu</c>).
    /// </summary>
    Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items);

    /// <summary><c>IM x, y:</c> — the button state (the script's <c>RetVal</c>) and the cursor position.</summary>
    Task<MouseState> GetMouseAsync();

    /// <summary>
    /// <c>IC shape, var:</c> — sets the mouse cursor to <paramref name="shape"/> and returns the
    /// shape set before (an engine static that starts at 1 and is not in the memory; the harness
    /// records it as <c>cursor</c>).
    /// </summary>
    Task<Sys3Int> SetCursorAsync(Sys3Int shape);

    /// <summary><c>ZT0 var:</c> — year, month, day, hour, minute, second, weekday (1 = Sunday).</summary>
    Task<IReadOnlyList<Sys3Int>> GetDateAsync();

    /// <summary><c>ZT1 n:</c> — restarts the engine timer at <paramref name="value"/>.</summary>
    Task ResetTimerAsync(Sys3Int value);

    /// <summary><c>ZTn var:</c>, n = 2..5 — the timer in the unit of <paramref name="mode"/> (2: 1/10 s, 3: 1/30 s, 4: 1/60 s, 5: 1/100 s).</summary>
    Task<Sys3Int> GetTimerAsync(int mode);

    /// <summary><c>SC var:</c> — the CD play position: track, minute, second, frame (999 each when nothing plays).</summary>
    Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync();

    /// <summary><c>SR 1, var:</c> — the MIDI track playing (0 when stopped).</summary>
    Task<Sys3Int> GetMidiTrackAsync();

    /// <summary>
    /// <c>ZZ3 var:</c> — the display's width, height and bits per pixel (<c>cmdz.c</c>: the
    /// desktop's current mode as SDL reports it). Only the title asks (<c>ｽﾀMENU.ADV:717</c>), for a
    /// 256-colour warning it shows only with <c>SpecGraphics ≥ 210</c>, which the port never has
    /// (<c>XSPEC</c> is not run, QUIRK: DAT-04); the values stay in <c>Tmp0001-0003</c>. By default
    /// a common desktop: 1920 × 1080 at 24 bits (SDL's XRGB8888).
    /// </summary>
    Task<IReadOnlyList<Sys3Int>> GetDisplayAsync() =>
        Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)1920, (Sys3Int)1080, (Sys3Int)24]);
}

/// <summary>What <c>IM</c> reads: <paramref name="Button"/> goes to <c>RetVal</c> (0 = released).</summary>
public readonly record struct MouseState(Sys3Int Button, Sys3Int X, Sys3Int Y);

/// <summary>
/// One registered menu item (<c>$label$</c> … <c>$</c>): the texts printed into it, captured when
/// they were printed — the engine copies the text into the item, so later changes to the string
/// variables do not reach it (<c>sel_addElement</c>).
/// </summary>
public sealed record SceneMenuItem(ScriptRef At, IReadOnlyList<CapturedText> Texts);

/// <summary>
/// A text run as printed at one moment: its location, the numbers of its <c>H</c>/<c>HH</c> and
/// the contents of the string variables of its <c>X</c>/<c>MP</c>, in order.
/// </summary>
public sealed record CapturedText(ScriptRef Text, IReadOnlyList<Sys3Int> Numbers, IReadOnlyList<string> Strings);
