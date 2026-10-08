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
public sealed record MessageEvent(ScriptRef Text) : BattleEvent;
