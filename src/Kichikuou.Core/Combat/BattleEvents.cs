using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Events shared by every part of the battle. Part-specific events live in
// BattleEvents.<Part>.cs next to the code that raises them.

/// <summary>
/// <c>Y 3, n:</c> — a timed pause of n ticks starts; the pause itself (and the key that ends it)
/// is <see cref="IBattleInput.PauseAsync"/>.
/// </summary>
public sealed record PauseEvent(Sys3Int Ticks) : BattleEvent;

/// <summary>A message from the script, identified by where its text is in the scenario.</summary>
/// <param name="Text">Where the text run starts.</param>
/// <param name="StringVariables">
/// The string variables printed by the run's <c>X expr:</c> commands whose number the script
/// computes (in order); the presentation cannot evaluate script expressions.
/// </param>
public sealed record MessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int>? StringVariables = null) : BattleEvent;
