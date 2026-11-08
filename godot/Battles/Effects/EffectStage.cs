using Godot;
using Kichikuou.Content.Graphics;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// An effect over a battlefield the size of our scene (1024×410), for Debug → Effects and the screen gallery's Combat VFX
/// tab: a viewport with black, the battlefield (<see cref="EffectCatalog.Battlefield"/>, scaled up) and the effect at the
/// scene's foot (y 8: 1024×402), as the battle screen lays it. Its picture is <see cref="Viewport.GetTexture"/>.
/// </summary>
internal sealed partial class EffectStage : SubViewport
{
    public const int StageWidth = 1024, StageHeight = 410;

    public EffectStage()
    {
        Size = new Vector2I(StageWidth, StageHeight);
        RenderTargetUpdateMode = UpdateMode.Always;
        AddChild(new ColorRect { Color = Colors.Black, Size = new Vector2(StageWidth, StageHeight) });
        Field = new TextureRect
        {
            Size = new Vector2(StageWidth, StageHeight),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        AddChild(Field);
        Player = new EffectPlayer { Position = new Vector2(0, StageHeight - EffectPlayer.Height) };
        AddChild(Player);
    }

    /// <summary>The battlefield under the effect (hidden: the effect on black).</summary>
    public TextureRect Field { get; }

    public EffectPlayer Player { get; }

    /// <summary>The battlefield with <paramref name="player"/>'s and <paramref name="enemy"/>'s armies.</summary>
    public void ShowField(CgLibrary cgs, EffectArmy player, EffectArmy enemy)
    {
        var field = EffectCatalog.Battlefield(cgs, player.Cg, enemy.Cg);
        Field.Texture = field is null ? null : ImageTexture.CreateFromImage(field);
    }

    /// <summary>
    /// <paramref name="effect"/> built from <paramref name="seed"/> over its default armies (<see cref="EffectShow.User"/>: the
    /// commander among the player's troops), standing at <paramref name="frame"/>, mirrored when the enemy casts it. The
    /// stage must be in the tree (the player draws only while it is shown).
    /// </summary>
    public void Show(CgLibrary cgs, EffectShow effect, int frame, int seed = 1)
    {
        ShowField(cgs, effect.User, effect.EnemyArmy);
        Player.Start(effect.Build(seed));
        Player.Dark = effect.Dark;
        Player.Mirrored = effect.EnemyCasts;
        Player.Frame = frame;
    }
}
