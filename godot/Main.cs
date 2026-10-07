using Godot;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Game;

/// <summary>Placeholder main scene: proves the Godot project runs against Kichikuou.Core.</summary>
public partial class Main : Control
{
    public override void _Ready()
    {
        var random = new XSystem35Random(1234);
        string text = $"Kichikuou.Core loaded. Y 4,100 with seed 1234 -> {random.Next(100)}";
        GetNode<Label>("Label").Text = text;
        GD.Print(text);
    }
}
