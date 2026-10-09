using Godot;

namespace Kichikuou.Game;

/// <summary>Switches between the program's screens (each a node made in code, the title a scene).</summary>
public static class Screens
{
    /// <summary>Makes <paramref name="next"/> the current scene in place of <paramref name="current"/>.</summary>
    public static void Open(Node current, Node next)
    {
        var tree = current.GetTree();
        tree.Root.AddChild(next);
        tree.CurrentScene = next;
        current.QueueFree();
    }

    /// <summary>Back to the title.</summary>
    public static void ToTitle(Node current) => current.GetTree().ChangeSceneToFile("res://Main.tscn");
}
