using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Content.Screen;

/// <summary>A palette fade (<c>PF</c>, <c>PW</c>) for the presentation engine to animate: from one amount to another at the command's <paramref name="Rate"/>.</summary>
public sealed record FadeRequest(byte From, byte To, bool ToWhite, int Rate)
{
    /// <summary>
    /// xsystem35's <c>ags_fadeIn</c>/<c>ags_fadeOut</c>: <c>rate * 16 * 1000 / 60</c> ms (≈267 ms a
    /// unit; the scenes' <c>FadeSpeed</c> 4 is 1066 ms), not the 16 ms tick of <c>Y 3</c>. The
    /// message skip does not shorten it (<c>ags_runEffect</c> waits without <c>KEYWAIT_SKIPPABLE</c>).
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMilliseconds(Rate * 16 * 1000 / 60);
}

/// <summary>What the owner of an <see cref="EngineStage"/> learns of its drawing (the screens it draws over the stage).</summary>
public interface IStageObserver
{
    /// <summary>A copy is about to read the stage from <paramref name="sourceX"/>, <paramref name="width"/> wide.</summary>
    void BeforeCopy(int sourceX, int width);

    /// <summary>A command drew into the rectangle; an <paramref name="opaque"/> one hides whatever was there.</summary>
    void Drawn(int x, int y, int width, int height, bool opaque);

    /// <summary><c>G</c> loaded CG <paramref name="number"/> (drawn or not, as <c>PC</c> says).</summary>
    void CgLoaded(int number);
}

/// <summary>
/// The engine's drawing commands (<see cref="Engine.Script.EngineEvent"/>) run on its surface as
/// xsystem35 runs them: <c>G</c> with <c>J0</c>/<c>J1</c>/<c>PC</c>, <c>CC</c>, <c>CE</c>,
/// <c>CS</c>, <c>CF</c>, <c>CM</c>, <c>WV</c>, <c>CB</c>, <c>CL</c>, <c>EC</c> and the fades
/// <c>PF</c>/<c>PW</c> (<see cref="Draw"/>), and the state the text commands keep: <c>ZM</c>,
/// <c>ZC</c>, the windows <c>B1</c>-<c>B3</c>, <c>SL</c> (<see cref="SetState"/>). A variant's
/// presenter owns one, places the text itself (<c>B4</c>, <c>T</c>) and draws its own screens over
/// the stage, told of the drawing by its <see cref="IStageObserver"/>.
/// </summary>
public sealed class EngineStage(Surface stage, CgLibrary cgs, IStageObserver observer)
{
    /// <summary>Message windows <c>B3 n, x, y, w, h</c> by number.</summary>
    private readonly Dictionary<int, (int X, int Y, int W, int H)> _messageWindows = [];

    /// <summary>Selection windows <c>B1 n, x, y, w</c> by number.</summary>
    private readonly Dictionary<int, (int X, int Y, int W)> _selectionWindows = [];

    private (int X, int Y)? _jump;
    private (int X, int Y)? _jumpRelative;
    private int _cgFlags = 7;
    private int _nextMusicLoops;
    private FadeRequest? _fade;

    /// <summary>The engine's surface the commands draw on.</summary>
    public Surface Stage => stage;

    /// <summary>The text size (<c>ZM</c>).</summary>
    public int TextSize { get; set; } = 24;

    /// <summary>The text colour (<c>ZC 1</c>).</summary>
    public int TextColor { get; set; } = 15;

    /// <summary>How far the screen is faded (0 not at all, 255 all black or white).</summary>
    public byte FadeAmount { get; set; }

    /// <summary>Whether the fade goes to white (<c>PW</c>) rather than black (<c>PF</c>).</summary>
    public bool FadeToWhite { get; private set; }

    /// <summary>The selection window <c>B2</c> chose, or window 1 when that one is not set; null when neither is.</summary>
    public (int X, int Y, int W)? SelectionWindow =>
        _selectionWindows.TryGetValue(_selectionWindow, out var window) ? window
        : _selectionWindows.TryGetValue(1, out var first) ? first : null;

    private int _selectionWindow = 1;

    /// <summary>Message window <paramref name="number"/> (<c>B3</c>), or null when it is not set.</summary>
    public (int X, int Y, int W, int H)? MessageWindow(int number) =>
        _messageWindows.TryGetValue(number, out var window) ? window : null;

    /// <summary>The fade the last command started, once (the engine animates it; <see cref="FadeAmount"/> is already the end).</summary>
    public FadeRequest? TakeFade()
    {
        var fade = _fade;
        _fade = null;
        return fade;
    }

    /// <summary>The loop count <c>SL</c> set for the next <c>SS</c>, which takes it (<c>cmds.c</c>: every <c>SS</c> resets it).</summary>
    public int TakeMusicLoops()
    {
        int loops = _nextMusicLoops;
        _nextMusicLoops = 0;
        return loops;
    }

    /// <summary>A command that only sets engine state (<c>ZM</c>, <c>ZC</c>, <c>SL</c>, <c>B1</c>-<c>B3</c>); false for any other.</summary>
    public bool SetState(string command, IReadOnlyList<Sys3Int> args)
    {
        int A(int i) => i < args.Count ? args[i].Value : 0;
        switch (command)
        {
            case "ZM": TextSize = A(0); return true;
            case "ZC":
                if (A(0) == 1)
                    TextColor = A(1);
                return true;
            case "SL": _nextMusicLoops = A(0); return true;              // the next SS's loop count
            case "B1": _selectionWindows[A(0)] = (A(1), A(2), A(3)); return true;
            case "B2": _selectionWindow = A(0); return true;
            case "B3": _messageWindows[A(0)] = (A(1), A(2), A(3), A(4)); return true;
            default: return false;
        }
    }

    /// <summary>A drawing command on the stage; any other command is ignored.</summary>
    public void Draw(string command, IReadOnlyList<Sys3Int> args)
    {
        int A(int i) => i < args.Count ? args[i].Value : 0;
        switch (command)
        {
            case "J0": _jump = (A(0), A(1)); break;
            case "J1": _jumpRelative = (A(0), A(1)); break;
            case "PC": _cgFlags = A(0); break;
            case "G": DrawCg(A(0), args.Count > 1 ? A(1) : null); break;
            case "CC" or "CE":                                          // CE: the copy at once, without its effect (the logo)
                observer.BeforeCopy(A(0), A(2));
                stage.Copy(A(0), A(1), A(2), A(3), A(4), A(5));
                observer.Drawn(A(4), A(5), A(2), A(3), opaque: true);
                break;
            case "CS":
                observer.BeforeCopy(A(0), A(2));
                stage.CopyKeyed(A(0), A(1), A(2), A(3), A(4), A(5));
                observer.Drawn(A(4), A(5), A(2), A(3), opaque: false);
                break;
            case "CF":
                stage.Fill(A(0), A(1), A(2), A(3), A(4));
                observer.Drawn(A(0), A(1), A(2), A(3), opaque: true);
                break;
            case "CM":
                observer.BeforeCopy(A(0), A(2));
                stage.ScaledCopy(A(0), A(1), A(2), A(3), A(4), A(5), A(6), A(7), A(8));
                observer.Drawn(A(4), A(5), A(6), A(7), opaque: true);
                break;
            case "WV":
                stage.SetView(A(0), A(1));
                break;
            case "CB":
                stage.DrawBox(A(0), A(1), A(2), A(3), A(4));
                observer.Drawn(A(0), A(1), A(2), A(3), opaque: false);
                break;
            case "CL":
                stage.DrawLine(A(0), A(1), A(2), A(3), A(4));
                observer.Drawn(Math.Min(A(0), A(2)), Math.Min(A(1), A(3)), Math.Abs(A(2) - A(0)) + 1, Math.Abs(A(3) - A(1)) + 1, opaque: false);
                break;
            case "EC":
                if (A(0) == 0)
                {
                    stage.Fill(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0);
                    observer.Drawn(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, opaque: false);
                }
                break;
            case "PF0" or "PF2": Fade(0, toWhite: false, A(0)); break;
            case "PF1" or "PF3": Fade(255, toWhite: false, A(0)); break;
            case "PW0" or "PW2": Fade(0, toWhite: true, A(0)); break;
            case "PW1" or "PW3": Fade(255, toWhite: true, A(0)); break;
        }
    }

    /// <summary>
    /// <c>G n</c> / <c>G n, key</c>: at the <c>J0</c> position, or the CG's own moved by <c>J1</c>,
    /// or its own. <c>PC</c> (<c>cg_fflg</c>, <c>cg.c:63-65</c>): bit 4 loads the CG's palette and
    /// bit 2 shows it — the scenario sets both or neither, so bit 4 alone decides here — and bit 1
    /// draws the CG; <c>PC 1</c> draws without the palette, <c>PC 6</c> loads it without drawing
    /// (<c>ｵｰﾌﾟﾆﾝｸﾞ.ADV:5-41</c>, <c>ﾌﾟﾛﾛｰｸﾞ.ADV:134-139</c>). The stage keeps palette entries, so
    /// the load recolours what is drawn, as the engine's 8-bit screen.
    /// </summary>
    private void DrawCg(int number, int? key)
    {
        var jump = _jump;
        var relative = _jumpRelative;
        _jump = null;
        _jumpRelative = null;
        if (cgs.Get(number) is not { } cg)
            return;
        observer.CgLoaded(number);
        var (x, y) = jump ?? (relative is var (dx, dy) ? (cg.X + dx, cg.Y + dy) : (cg.X, cg.Y));
        if ((_cgFlags & 4) != 0)
            stage.Palette = cg.PaletteOver(stage.Palette);
        if ((_cgFlags & 1) == 0)
            return;
        stage.DrawCgRegion(cg, 0, 0, cg.Width, cg.Height, x, y, key);
        observer.Drawn(x, y, cg.Width, cg.Height, opaque: key is null);
    }

    private void Fade(byte to, bool toWhite, int ticks)
    {
        FadeToWhite = toWhite;
        _fade = new FadeRequest(FadeAmount, to, FadeToWhite, ticks);
        FadeAmount = to;
    }
}
