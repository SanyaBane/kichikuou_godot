using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// PCMPLAY.ADV (the SP sound-effect wrapper; it writes nothing but the CallArg its caller set)
// and the engine's music commands SS / SR. Playing is the presentation's job; the rules only
// see the variables the script sets around the call.
public sealed partial class ScriptContext
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

    /// <summary><c>SS n</c>: plays music track n (0 stops the music).</summary>
    public Task MusicAsync(Sys3Int track) => View.ShowAsync(new MusicEvent(track));

    /// <summary><c>SR 0, var</c>: the track that is playing (the caller stores it).</summary>
    public Task<Sys3Int> GetPlayingTrackAsync() => Input.GetPlayingTrackAsync();
}
