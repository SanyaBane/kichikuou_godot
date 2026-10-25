using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// A successful pre-battle strategy as our own cut-in (our interface; docs/ui/battle-screen.md, "The strategy's
/// cut-in"; owner, 2026-10-15, mock-up A): the original's strategy screen as it stood before its first box
/// (<see cref="BattlePresenter.StrategyBackdrop"/>: none of the boxes, nor an earlier dialogue's leftovers), dimmed and
/// blurred; a slanted band in the strategist's side's colour, strong at its side; the strategist's whole portrait sliding
/// in from that side, its top at the first line's; the script's announcement over a large title ("Success", "Great
/// Success"); the strategist's line in our balloon, word for word; the advantage it gives below ("Strategy +10%"); the
/// whole centred on the screen (owner, 2026-10-16). It decides nothing: the line's key wait is the script's, and
/// <see cref="GameScreen"/> answers the announcement's and the effect's at once. Points are layout pixels of the
/// 1280×800 screen.
/// </summary>
internal sealed partial class StrategyCutInView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    // The block: the portrait (400 high) from Top, centred on the screen; the text column beside it, its first line's
    // capitals from Top too.
    private const float Top = 200;
    private const float BandTop = Top + 176, BandBottom = Top + 298, BandSlant = 60, BandAlpha = 0.92f, EdgeWidth = 3;
    private static readonly Rect2 PlayerPortrait = new(70, Top, 300, 400), EnemyPortrait = new(910, Top, 300, 400);
    private const float TextEdge = 410, CaptionBase = Top + 18, CaptionSize = 26, TitleBase = Top + 94, TitleSize = 64;
    private const float BalloonTop = Top + 136, BalloonWidth = 600, BalloonPad = 22, BalloonMin = 100, LineSize = 30, LineGap = 8;
    private const float LabelBelow = 62, LabelSize = 38, GreatLabelSize = 46;
    private const float SlideIn = 400, TitleSlide = 60, AppearSeconds = 0.3f, BobPixels = 3, BobSeconds = 0.9f, MarkerSize = 9;
    private const float Dim = 0.69f;                                 // as the scout report's
    private static readonly Color PlayerColour = new(69 / 255f, 117 / 255f, 219 / 255f), EnemyColour = new(209 / 255f, 77 / 255f, 77 / 255f);
    private static readonly Color Ink = new(20 / 255f, 20 / 255f, 30 / 255f), PortraitShadow = new(0, 0, 0, 0.63f);
    private const string TitleFont = "res://fonts/Shojumaru-Regular.ttf", CaptionFont = "res://fonts/Cinzel-VariableFont_wght.ttf";
    private const TextServer.LineBreakFlag Breaks =
        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive;

    /// <summary>The backdrop's picture blurred (a mipmap level a step above the scout report's: plain mipmaps blur less than the screen's) and dimmed.</summary>
    private const string BlurShader = """
        shader_type canvas_item;
        uniform float dim = 0.69;
        void fragment() {
            COLOR = vec4(textureLod(TEXTURE, UV, 3.5).rgb * (1.0 - dim), 1.0);
        }
        """;

    private readonly FacePictures _faces;
    private readonly FontFile _title = Load(TitleFont);
    private readonly FontVariation _caption = new() { BaseFont = Load(CaptionFont) };
    private readonly FontVariation _bold = new() { VariationEmbolden = 0.8f };
    private readonly Control _sheet;
    private readonly TextureRect _backdrop;
    private byte[]? _backdropPixels;
    private BattleStrategy? _strategy;
    private ImageTexture? _portrait;
    private int _portraitCg;
    private double _appearing;
    private float _scale = 1;

    public StrategyCutInView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        _caption.VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 700,
        };
        var black = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };   // round the screen's area
        black.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(black);
        _backdrop = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.LinearWithMipmaps,
            Material = new ShaderMaterial { Shader = new Shader { Code = BlurShader } },
        };
        ((ShaderMaterial)_backdrop.Material).SetShaderParameter("dim", Dim);
        AddChild(_backdrop);
        _sheet = new Control { MouseFilter = MouseFilterEnum.Ignore, ClipContents = true };
        _sheet.Draw += DrawSheet;
        AddChild(_sheet);
        SetFont(font);
    }

    private static FontFile Load(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

    /// <summary>Option → Display: the strategist's line in the chosen font, bold.</summary>
    public void SetFont(GameFont font)
    {
        _bold.BaseFont = DeploymentTableView.LoadFont(font);
        _sheet.QueueRedraw();
    }

    /// <summary>Lays the cut-in out over the area whose layout pixel (0, 0) is at <paramref name="origin"/>, <paramref name="scale"/> window pixels to a layout pixel.</summary>
    public void Place(Vector2 origin, float scale)
    {
        if (origin == _sheet.Position && scale == _scale && _sheet.Size != Vector2.Zero)
            return;
        _scale = scale;
        _sheet.Position = _backdrop.Position = origin;
        _sheet.Size = _backdrop.Size = new Vector2(1280, 800) * scale;
        _sheet.QueueRedraw();
    }

    /// <summary>
    /// Shows <paramref name="strategy"/> over <paramref name="backdrop"/> (RGBA, <paramref name="width"/> × <paramref name="height"/>):
    /// a new strategist's slides in (at once when <paramref name="instant"/>); the same one is drawn again as the script fills it in.
    /// </summary>
    public void Show(BattleStrategy strategy, byte[]? backdrop, int width, int height, bool instant)
    {
        if (backdrop != _backdropPixels)
        {
            _backdropPixels = backdrop;
            var image = backdrop is null ? null : Image.CreateFromData(width, height, false, Image.Format.Rgba8, backdrop);
            image?.GenerateMipmaps();
            _backdrop.Texture = image is null ? null : ImageTexture.CreateFromImage(image);
        }
        if (strategy == _strategy)
            return;
        if (_strategy is null || strategy.Side != _strategy.Side || strategy.Record != _strategy.Record)
            _appearing = instant ? AppearSeconds : 0;
        _strategy = strategy;
        if (strategy.Portrait != _portraitCg)
        {
            _portraitCg = strategy.Portrait;
            _portrait = _faces.Picture(strategy.Portrait) is { } image ? ImageTexture.CreateFromImage(image) : null;
        }
        _sheet.QueueRedraw();
    }

    /// <summary>The cut-in is gone: the next strategy slides in again.</summary>
    public void Clear() => _strategy = null;

    public override void _Process(double delta)
    {
        if (!Visible || _strategy is null)
            return;
        _appearing += delta;
        _sheet.QueueRedraw();                                           // the slide, then the marker's bob
    }

    private void DrawSheet()
    {
        if (_strategy is not { } strategy || _bold.BaseFont is null)
            return;
        float s = _scale;
        float t = Math.Clamp((float)(_appearing / AppearSeconds), 0, 1);
        float a = 1 - (1 - t) * (1 - t) * (1 - t);                      // eased out
        bool player = strategy.Side == BattleSide.Player;
        float outward = player ? -1 : 1;
        var side = player ? PlayerColour : EnemyColour;

        // The band, strong at the strategist's side and fading across, its edges in the accent colour.
        Vector2 P(float x, float y) => new Vector2(x, y) * s;
        Vector2[] band = [P(0, BandTop + BandSlant), P(1280, BandTop - BandSlant), P(1280, BandBottom - BandSlant), P(0, BandBottom + BandSlant)];
        Color strong = side with { A = BandAlpha * a }, faint = side with { A = 0 };
        _sheet.DrawPolygon(band, player ? [strong, faint, faint, strong] : [faint, strong, strong, faint]);
        var edge = Ui.HoverLine with { A = a };
        _sheet.DrawLine(band[0], band[1], edge, Math.Max(1, EdgeWidth * s), true);
        _sheet.DrawLine(band[3], band[2], edge, Math.Max(1, EdgeWidth * s), true);

        // The portrait slides in from its side.
        var area = player ? PlayerPortrait : EnemyPortrait;
        area.Position += new Vector2(outward * SlideIn * (1 - a), 0);
        if (_portrait is not null)
        {
            var shadow = new StyleBoxFlat { BgColor = Colors.Transparent, ShadowColor = PortraitShadow, ShadowSize = Px(10), ShadowOffset = new Vector2(0, 4 * s) };
            _sheet.DrawStyleBox(shadow, new Rect2(area.Position * s, area.Size * s));
            _sheet.DrawTextureRect(_portrait, new Rect2(area.Position * s, area.Size * s), false, new Color(1, 1, 1, a));
        }

        // The announcement over the title, both from the portrait's side.
        var align = player ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        float textX = (player ? TextEdge : 1280 - TextEdge) + outward * TitleSlide * (1 - a);
        if (strategy.Announcement != "")
            Outlined(_caption, P(textX, CaptionBase), strategy.Announcement.ToUpperInvariant(), Px(CaptionSize),
                Ui.HoverLine with { A = a }, Ink with { A = a }, Px(5), align);
        if (strategy.Result != 0)
            Outlined(_title, P(textX, TitleBase), strategy.Title, Px(TitleSize),
                (strategy.Great ? Ui.HoverLine : Colors.White) with { A = a }, Ink with { A = a }, Px(10), align);

        if (strategy.Line is not { } line)
            return;
        // The line in our balloon, its tail at the portrait.
        int size = Px(LineSize);
        _bold.SetSpacing(TextServer.SpacingType.Bottom, Px(LineGap));
        float textWidth = (BalloonWidth - 2 * BalloonPad) * s;
        var text = _bold.GetMultilineStringSize(line.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
        float height = Math.Max(BalloonMin * s, text.Y + 2 * BalloonPad * s);
        float x = player ? TextEdge - 10 : 1280 - TextEdge + 10 - BalloonWidth;
        var box = new Rect2(P(x, BalloonTop), new Vector2(BalloonWidth * s, height));
        var ink = SpeechBalloon.DrawBalloon(_sheet, box, tailLeft: player, s, line.Thought);
        var origin = box.Position + new Vector2(BalloonPad * s, BalloonPad * s - 4 * s + _bold.GetAscent(size));
        _sheet.DrawMultilineString(_bold, origin, line.Text, HorizontalAlignment.Left, textWidth, size, -1, ink, Breaks);
        float bob = BobPixels * s * Mathf.Sin((float)(UiClock.Msec / 1000.0 * Mathf.Tau / BobSeconds));
        var tip = new Vector2(box.End.X - 26 * s, box.End.Y - 17 * s + bob);
        _sheet.DrawColoredPolygon([tip + new Vector2(-MarkerSize, -MarkerSize * 1.1f) * s, tip + new Vector2(MarkerSize, -MarkerSize * 1.1f) * s, tip],
            Ui.HoverLine);

        // The advantage it gives, as the opening and the armies' labels go on to show it.
        if (strategy.Label != "")
            Outlined(_title, new Vector2(player ? box.Position.X + 4 * s : box.End.X - 4 * s, box.End.Y + LabelBelow * s), strategy.Label,
                Px(strategy.Great ? GreatLabelSize : LabelSize), Ui.HoverLine, Ink, Px(7), align);
    }

    /// <summary>Text with a dark outline at a baseline (the outline first, the text over it).</summary>
    private void Outlined(Font font, Vector2 at, string text, int size, Color colour, Color ink, int outline, HorizontalAlignment align)
    {
        var pos = align == HorizontalAlignment.Right ? at - new Vector2(font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X, 0) : at;
        _sheet.DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, size, outline, ink);
        _sheet.DrawString(font, pos, text, HorizontalAlignment.Left, -1, size, colour);
    }

    private int Px(float size) => Math.Max(1, (int)Math.Round(size * _scale));
}
