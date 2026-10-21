using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Our dialogue panel on the battle screen (our interface; docs/ui/battle-screen.md, "The dialogue
/// panel"; owner, 2026-10-13, mock-up A after Sengoku Rance's battle dialogue boxes, docs/sengoku/04-screen.md §10):
/// the line of the open speech bubble (<see cref="BattleSpeech"/>) in a rounded balloon beside the speaker's
/// portrait, its tail towards it, across the scene's lower edge; the player's on the left, the enemy's on the
/// right; a thought (the original's dark bubble) dark with white text. A marker bobs in its corner while the line
/// waits for the click, which stays the script's. Points are layout pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class SpeechBalloon : Control
{
    private const float Width = 580, Bottom = 436, Side = 330, Pad = 22, MinHeight = 100;
    private const float TailDepth = 18, TailHalf = 13, TailFromTop = 44, Radius = 16;
    private const float TextSize = 30, LineGap = 8, MarkerSize = 9, BobPixels = 3, BobSeconds = 0.9f;
    private static readonly Color TalkFill = new(1, 1, 1, 0.93f), TalkEdge = new(1, 1, 1), TalkInk = new(0.094f, 0.094f, 0.11f);
    private static readonly Color ThinkFill = new(0.118f, 0.133f, 0.173f, 0.9f), ThinkEdge = new(0.78f, 0.8f, 0.84f), ThinkInk = new(1, 1, 1);
    private static readonly Color Shadow = new(0, 0, 0, 0.43f);
    private const TextServer.LineBreakFlag Breaks =
        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive;

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private readonly FontVariation _bold = new() { VariationEmbolden = 0.8f };
    private Font? _font;
    private float _scale = 1;
    private BattleSpeech? _speech;

    public SpeechBalloon()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 3;                                                     // over the strip and the cards (1), the spotlight and the acting frame (2)
    }

    /// <summary>Shows <paramref name="speech"/> (null: none).</summary>
    public void Show(BattleSpeech? speech)
    {
        if (speech == _speech)
            return;
        _speech = speech;
        QueueRedraw();
    }

    public void Layout(float scale)
    {
        _scale = scale;
        QueueRedraw();
    }

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        QueueRedraw();
    }

    /// <summary>Option → Fonts: the line in the screen's font, bold.</summary>
    public void SetFont(Font font)
    {
        _font = font;
        _bold.BaseFont = font;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_speech is not null)
            QueueRedraw();                                              // the marker bobs
    }

    public override void _Draw()
    {
        if (_speech is not { } speech || _font is null)
            return;
        float s = _scale;
        int size = Math.Max(8, (int)Math.Round(TextSize * s));
        _bold.SetSpacing(TextServer.SpacingType.Bottom, (int)Math.Round(LineGap * s));
        float textWidth = (Width - 2 * Pad) * s;
        var text = _bold.GetMultilineStringSize(speech.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
        float height = Math.Max(MinHeight * s, text.Y + 2 * Pad * s);
        bool left = speech.Side == BattleSide.Player;
        float x = (left ? Side : 1280 - Side - Width) * s;
        var box = new Rect2(x, Bottom * s - height, Width * s, height);
        var ink = DrawBalloon(this, box, left, s, speech.Thought);

        var origin = box.Position + new Vector2(Pad * s, Pad * s - 4 * s + _bold.GetAscent(size));
        DrawMultilineString(_bold, origin, speech.Text, HorizontalAlignment.Left, textWidth, size, -1, ink, Breaks);

        float bob = BobPixels * s * Mathf.Sin((float)(Time.GetTicksMsec() / 1000.0 * Mathf.Tau / BobSeconds));
        var tip = new Vector2(box.End.X - 26 * s, box.End.Y - 17 * s + bob);
        DrawColoredPolygon(
        [
            tip + new Vector2(-MarkerSize, -MarkerSize * 1.1f) * s,
            tip + new Vector2(MarkerSize, -MarkerSize * 1.1f) * s,
            tip,
        ], _colours.Accent);
    }

    /// <summary>
    /// The balloon's shape on <paramref name="canvas"/> at <paramref name="box"/> (window pixels, <paramref name="s"/> to a
    /// layout pixel): rounded, a soft shadow, the tail out of its left or right side <see cref="TailFromTop"/> below its
    /// top; white for talk, dark for a thought. Returns the colour of the text on it. The scout report's balloon is one too.
    /// </summary>
    internal static Color DrawBalloon(CanvasItem canvas, Rect2 box, bool tailLeft, float s, bool thought = false)
    {
        var (fill, edge, ink) = thought ? (ThinkFill, ThinkEdge, ThinkInk) : (TalkFill, TalkEdge, TalkInk);
        float tailY = box.Position.Y + TailFromTop * s;
        float rim = tailLeft ? box.Position.X : box.End.X, outward = tailLeft ? -1 : 1;
        Vector2[] tail =
        [
            new(rim - outward * 2 * s, tailY - TailHalf * s),
            new(rim + outward * TailDepth * s, tailY),
            new(rim - outward * 2 * s, tailY + TailHalf * s),
        ];
        var style = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = edge,
            ShadowColor = Shadow,
            ShadowSize = (int)Math.Round(8 * s),
            ShadowOffset = new Vector2(0, 4 * s),
            AntiAliasing = true,
        };
        style.SetCornerRadiusAll((int)Math.Round(Radius * s));
        style.SetBorderWidthAll(Math.Max(1, (int)Math.Round(2 * s)));
        canvas.DrawColoredPolygon([.. tail.Select(p => p + new Vector2(0, 4 * s))], new Color(Shadow, Shadow.A * 0.5f));
        canvas.DrawStyleBox(style, box);
        canvas.DrawColoredPolygon(tail, fill);
        canvas.DrawPolyline([tail[0], tail[1], tail[2]], edge, Math.Max(1, 2 * s), antialiased: true);
        return ink;
    }
}
