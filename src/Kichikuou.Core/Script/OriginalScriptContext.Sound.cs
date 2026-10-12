using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// PCMPLAY.ADV (the SP sound-effect wrapper; it writes nothing but the CallArg its caller set).
// Playing is the presentation's job; the rules only see the variables the script sets around the
// call. The engine's music commands SS / SR are the base context's.
public sealed partial class OriginalScriptContext
{
    /// <summary>
    /// <c>!CallArg : mode! (!RetVal : sound!) %#PCMPLAY.ADV</c> — PCMPLAY writes nothing else;
    /// the caller sets <c>RetVal</c> to the sound number first. What each mode plays is the
    /// presentation's business (mode 10 plays nothing, <c>PCMPLAY.ADV:17-19</c>).
    /// </summary>
    public Task SoundAsync(Sys3Int mode)
    {
        State.CallArg = mode;
        return View.ShowAsync(new SoundEvent(State.CallArg, State.RetVal));
    }
}
