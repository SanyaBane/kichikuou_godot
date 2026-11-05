using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

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
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    private const float Width = 580, Bottom = 436, Side = 330, Pad = 22, MinHeight = 100;
    private const float TailDepth = 18, TailHalf = 13, TailFromTop = 44, Radius = 16;
    private const float TextSize = 30, LineGap = 8, MarkerSize = 9, BobPixels = 3, BobSeconds = 0.9f;
    private static readonly Color TalkFill = new(1, 1, 1, 0.93f), TalkEdge = new(1, 1, 1), TalkInk = new(0.094f, 0.094f, 0.11f);
    private static readonly Color ThinkFill = new(0.118f, 0.133f, 0.173f, 0.9f), ThinkEdge = new(0.78f, 0.8f, 0.84f), ThinkInk = new(1, 1, 1);
    private static readonly Color Shadow = new(0, 0, 0, 0.43f);
    private const TextServer.LineBreakFlag Breaks =
        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive;

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

    /// <summary>Option → Display: the line in the screen's font, bold.</summary>
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
        bool left = speech.Side == BattleSide.Player;
        if (Device.Layout == DeviceLayout.PhoneLandscape)
        {
            DrawPhone(speech, left, s);
            return;
        }
        if (Device.Layout == DeviceLayout.PhonePortrait)
        {
            DrawPortrait(speech, left, s);
            return;
        }
        int size = Math.Max(8, (int)Math.Round(TextSize * s));
        _bold.SetSpacing(TextServer.SpacingType.Bottom, (int)Math.Round(LineGap * s));
        float textWidth = (Width - 2 * Pad) * s;
        var text = _bold.GetMultilineStringSize(speech.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
        float height = Math.Max(MinHeight * s, text.Y + 2 * Pad * s);
        float x = (left ? Side : 1280 - Side - Width) * s;
        var box = new Rect2(x, Bottom * s - height, Width * s, height);
        var ink = DrawBalloon(this, box, left, s, speech.Thought);
        DrawSaying(speech, box, ink, size, textWidth, Pad * s, Pad * s - 4 * s, s);
    }

    /// <summary>
    /// The phone's (docs/ui/phone-build.md, step L4; LB3b): the balloon over the scene's foot beside the speaker's portrait
    /// in the blurred side, at the phone's type, smaller down to <see cref="PhoneBattleStates.LineMin"/> to keep it to two lines.
    /// </summary>
    private void DrawPhone(BattleSpeech speech, bool left, float s)
    {
        static float L(float reference) => PhoneBattle.Layout(reference);
        float textWidth = (L(PhoneBattleStates.BalloonWidth) - 2 * L(PhoneBattleStates.BalloonSide)) * s;
        int size = 8;
        Vector2 text = default;
        for (float em = PhoneBattleStates.LineText; em >= PhoneBattleStates.LineMin; em -= 2)
        {
            size = Math.Max(8, (int)Math.Round(L(em) * s));
            _bold.SetSpacing(TextServer.SpacingType.Bottom, (int)Math.Round(L(PhoneBattleStates.LineGap) * s));
            text = _bold.GetMultilineStringSize(speech.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
            if (text.Y <= PhoneBattleStates.MaxLines * (_bold.GetHeight(size) + (int)Math.Round(L(PhoneBattleStates.LineGap) * s)) + 1)
                break;
        }
        var b = PhoneBattleStates.Balloon(left, text.Y / s);
        var box = new Rect2(b.X * s, b.Y * s, b.W * s, b.H * s);
        var ink = DrawBalloon(this, box, left, L(PhoneBattleStates.TailDepth) / TailDepth * s, speech.Thought,
            L(PhoneBattleStates.TailFromTop) * s);
        DrawSaying(speech, box, ink, size, textWidth, L(PhoneBattleStates.BalloonSide) * s, L(PhoneBattleStates.BalloonTop) * s - 4 * s,
            L(PhoneBattleStates.LineText) / TextSize * s);
    }

    /// <summary>
    /// Held upright (docs/ui/phone-build.md, step P4; PB4b): the balloon over the scene's foot beside the speaker's portrait at
    /// the scene's top corner, at the phone's type, smaller down to <see cref="PhoneBattleStatesPortrait.LineMin"/> to keep it
    /// to two lines; in the board's layout pixels.
    /// </summary>
    private void DrawPortrait(BattleSpeech speech, bool left, float s)
    {
        static float L(float reference) => PhoneBattlePortrait.Layout(reference);
        float textWidth = (L(PhoneBattleStatesPortrait.BalloonWidth) - 2 * L(PhoneBattleStatesPortrait.BalloonSide)) * s;
        int size = 8;
        Vector2 text = default;
        for (float em = PhoneBattleStatesPortrait.LineText; em >= PhoneBattleStatesPortrait.LineMin; em -= 2)
        {
            size = Math.Max(8, (int)Math.Round(L(em) * s));
            _bold.SetSpacing(TextServer.SpacingType.Bottom, (int)Math.Round(L(PhoneBattleStatesPortrait.LineGap) * s));
            text = _bold.GetMultilineStringSize(speech.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
            if (text.Y <= PhoneBattleStatesPortrait.MaxLines * (_bold.GetHeight(size) + (int)Math.Round(L(PhoneBattleStatesPortrait.LineGap) * s)) + 1)
                break;
        }
        var b = PhoneBattleStatesPortrait.Balloon(left, text.Y / s);
        var box = new Rect2(b.X * s, b.Y * s, b.W * s, b.H * s);
        var ink = DrawBalloon(this, box, left, L(PhoneBattleStatesPortrait.TailDepth) / TailDepth * s, speech.Thought,
            L(PhoneBattleStatesPortrait.TailFromTop) * s);
        DrawSaying(speech, box, ink, size, textWidth, L(PhoneBattleStatesPortrait.BalloonSide) * s,
            L(PhoneBattleStatesPortrait.BalloonTop) * s - 4 * s, L(PhoneBattleStatesPortrait.LineText) / TextSize * s);
    }

    /// <summary>The line in the balloon from (<paramref name="side"/>, <paramref name="top"/>) in, the marker bobbing in its corner (<paramref name="k"/> its scale).</summary>
    private void DrawSaying(BattleSpeech speech, Rect2 box, Color ink, int size, float textWidth, float side, float top, float k)
    {
        var origin = box.Position + new Vector2(side, top + _bold.GetAscent(size));
        DrawMultilineString(_bold, origin, speech.Text, HorizontalAlignment.Left, textWidth, size, -1, InkOf(speech, ink), Breaks);

        float bob = BobPixels * k * Mathf.Sin((float)(UiClock.Msec / 1000.0 * Mathf.Tau / BobSeconds));
        var tip = new Vector2(box.End.X - 26 * k, box.End.Y - 17 * k + bob);
        DrawColoredPolygon(
        [
            tip + new Vector2(-MarkerSize, -MarkerSize * 1.1f) * k,
            tip + new Vector2(MarkerSize, -MarkerSize * 1.1f) * k,
            tip,
        ], Ui.HoverLine);
    }

    /// <summary>
    /// The ink of <paramref name="speech"/>'s line on its balloon, <paramref name="plain"/> the balloon's own: when the player read
    /// it before, the already-read text's colour (Option → Messages; docs/ui/read-text.md, §7, the owner's B2) — on the white
    /// balloon darkened until it reads, on a thought's dark one as it is.
    /// </summary>
    internal static Color InkOf(BattleSpeech speech, Color plain) =>
        !speech.Read || ReadTextChoice.Colour is not { } colour ? plain
        : speech.Thought ? colour
        : ReadTextChoice.OnPaper(colour, Paper);

    /// <summary>The talk balloon's paper as it shows: <see cref="TalkFill"/> over the scene.</summary>
    private static readonly Color Paper = Color.Color8(245, 245, 245);

    /// <summary>
    /// The balloon's shape on <paramref name="canvas"/> at <paramref name="box"/> (window pixels, <paramref name="s"/> to a
    /// layout pixel): rounded, a soft shadow, the tail out of its left or right side <see cref="TailFromTop"/> below its
    /// top (or <paramref name="tailFromTop"/> canvas units); white for talk, dark for a thought. Returns the colour of the text
    /// on it.
    /// </summary>
    internal static Color DrawBalloon(CanvasItem canvas, Rect2 box, bool tailLeft, float s, bool thought = false, float tailFromTop = -1)
    {
        var (fill, edge, ink) = thought ? (ThinkFill, ThinkEdge, ThinkInk) : (TalkFill, TalkEdge, TalkInk);
        float tailY = box.Position.Y + (tailFromTop >= 0 ? tailFromTop : TailFromTop * s);
        float rim = tailLeft ? box.Position.X : box.End.X, outward = tailLeft ? -1 : 1;
        Vector2[] tail =
        [
            new(rim - outward * 2 * s, tailY - TailHalf * s),
            new(rim + outward * TailDepth * s, tailY),
            new(rim - outward * 2 * s, tailY + TailHalf * s),
        ];
        canvas.DrawColoredPolygon([.. tail.Select(p => p + new Vector2(0, 4 * s))], new Color(Shadow, Shadow.A * 0.5f));
        canvas.DrawStyleBox(Style(fill, edge, s), box);
        canvas.DrawColoredPolygon(tail, fill);
        canvas.DrawPolyline([tail[0], tail[1], tail[2]], edge, Math.Max(1, 2 * s), antialiased: true);
        return ink;
    }

    /// <summary>
    /// The balloon as <see cref="DrawBalloon"/> draws it, its tail out of its top at <paramref name="tailX"/>, pointing up at
    /// the speaker over it (the strategy's cut-in held upright, docs/ui/phone.md, §11, PB4c). Returns the colour of the text on it.
    /// </summary>
    internal static Color DrawBalloonTailUp(CanvasItem canvas, Rect2 box, float tailX, float s, bool thought = false)
    {
        var (fill, edge, ink) = thought ? (ThinkFill, ThinkEdge, ThinkInk) : (TalkFill, TalkEdge, TalkInk);
        float rim = box.Position.Y;
        Vector2[] tail =
        [
            new(tailX - TailHalf * s, rim + 2 * s),
            new(tailX, rim - TailDepth * s),
            new(tailX + TailHalf * s, rim + 2 * s),
        ];
        canvas.DrawStyleBox(Style(fill, edge, s), box);
        canvas.DrawColoredPolygon(tail, fill);
        canvas.DrawPolyline([tail[0], tail[1], tail[2]], edge, Math.Max(1, 2 * s), antialiased: true);
        return ink;
    }

    /// <summary>The balloon's box: rounded, its rim, a soft shadow (<paramref name="s"/> window pixels to a layout pixel).</summary>
    private static StyleBoxFlat Style(Color fill, Color edge, float s)
    {
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
        return style;
    }
}
