using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// The input of a whole game: the battle's decisions (<see cref="IBattleInput"/>) and the scenes'
/// and strategic screens' menus, pointer and clock (<see cref="ISceneInput"/>). Each part keeps
/// asking for the narrow input it uses; only what drives the whole game — <see cref="StrategyRules"/>,
/// which runs the battle next to the pickers and scenes it calls, the Godot input, the golden
/// replay — needs this one (<c>docs/strategy/README.md</c>, decision 7).
/// </summary>
public interface IGameInput : IBattleInput, ISceneInput
{
}
