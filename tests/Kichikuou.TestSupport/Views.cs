using Kichikuou.Engine.Script;

namespace Kichikuou.TestSupport;

/// <summary>Shows nothing.</summary>
public sealed class NullView : IScriptView
{
    public Task ShowAsync(ScriptEvent scriptEvent) => Task.CompletedTask;
}

/// <summary>
/// Logs every event with its lists spelled out, but those <paramref name="ignore"/> picks (a port's
/// semantic events, which no statement of the script raises); <paramref name="note"/> may add to
/// an event's entry.
/// </summary>
public sealed class LoggingView(List<string> log, Func<ScriptEvent, bool>? ignore = null, Func<ScriptEvent, string?>? note = null)
    : IScriptView
{
    public Task ShowAsync(ScriptEvent e)
    {
        if (ignore?.Invoke(e) == true)
            return Task.CompletedTask;
        string entry = e switch
        {
            ValueMessageEvent v => $"ValueMessageEvent {v.Text} [{string.Join(",", v.Values)}] [{string.Join(",", v.StringVariables ?? [])}]",
            MessageEvent m => $"MessageEvent {m.Text} [{string.Join(",", m.StringVariables ?? [])}]",
            EngineEvent g => $"EngineEvent {g.Command} [{string.Join(",", g.Args)}]",
            _ => e.ToString(),
        };
        log.Add(entry + note?.Invoke(e));
        return Task.CompletedTask;
    }
}
