using System.Reflection;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>The game's input proxy hands every request to its target.</summary>
public class GameInputProxyTests
{
    /// <summary>
    /// Every method of <see cref="IGameInput"/> and the interfaces it extends is implemented by the proxy
    /// itself: a method with a default body that the proxy left out would answer with the default and never
    /// reach the UI (as the battle's release loops did, 2026-10-13).
    /// </summary>
    [Fact]
    public void The_proxy_implements_every_method_of_the_game_s_input()
    {
        var proxy = typeof(GameInputProxy);
        var missing = new List<string>();
        foreach (var face in typeof(IGameInput).GetInterfaces().Append(typeof(IGameInput)))
        {
            var map = proxy.GetInterfaceMap(face);
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.TargetMethods[i].DeclaringType != proxy)
                    missing.Add($"{face.Name}.{map.InterfaceMethods[i].Name}");
            }
        }
        Assert.Empty(missing);
    }
}
