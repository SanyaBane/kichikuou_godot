using System.Globalization;
using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The ninja's scout report as our own overlay (Option → Interface → New; docs/ui/battle-screen.md, "The scout
/// report"; owner, 2026-10-14, mock-up C): the original's screen dimmed and blurred under it; the ninja's whole
/// portrait on the left; our balloon beside it at the top with the army's name and the totals; the approaching
/// commanders as cards in the places the battle will give them, the front row (places 1-2) in the left column, the
/// back row (3-4) in the right one. A card tells only what the ninja tells (<see cref="ScoutedCommander"/>): name,
/// troop type and troops — no maximum, no HP. At its foot Maris's comment on it, which the script has her say after
/// Rance's line (owner, 2026-10-16). It decides nothing: the report's key waits stay the script's, and
/// <see cref="GameScreen"/> answers all but the first at once, and her window's too. Points are layout pixels of the
/// 1280×800 screen.
/// </summary>
internal sealed partial class ScoutReportView : Control
{
    private static readonly Rect2 PortraitArea = new(40, 40, 300, 400);       // its top with the balloon's: the tail points at it
    private static readonly Rect2 BalloonArea = new(370, 40, 870, 110);
    private static readonly Rect2 AdvisorArea = new(1086, 572, 154, 205);      // Maris's portrait at the foot, right; 192×256 at 0.8
    private const float CommentLeft = 380, CommentGap = 30, CommentTop = 24;  // her balloon from the cards' left to her, its tail at her
    private static readonly Vector2 CardSize = new(282, 150);
    private static readonly float[] Columns = [380, 680];
    private static readonly float[] Rows = [190, 350];
    private const float CaptionY = 520, CaptionText = 16;
    private const float FaceSize = 118, TextSize = 17, NumberSize = 26, BalloonText = 24, BalloonPad = 22;
    private const float Dim = 0.69f;
    private static readonly Color CardShadow = new(0, 0, 0, 0.47f);

    /// <summary>The place of a slot's card: the front row (1-2) left, the back row (3-4) right, slot 1 and 3 on top.</summary>
    private static Rect2 CardArea(int slot) => new(Columns[(slot - 1) / 2], Rows[(slot - 1) % 2], CardSize.X, CardSize.Y);

    private const string BlurShader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
        uniform float dim = 0.69;
        void fragment() {
            COLOR = vec4(textureLod(screen, SCREEN_UV, 2.5).rgb * (1.0 - dim), 1.0);
        }
        """;

    private readonly FacePictures _faces;
    private FaceCrops _crops = new();
    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private Font? _font;
    private readonly FontVariation _bold = new() { VariationEmbolden = 0.8f };
    private readonly ColorRect _backdrop;
    private readonly Control _sheet;
    private ScoutReport? _report;
    private ImageTexture? _portrait;
    private ImageTexture? _advisor;
    private int _portraitCg;
    private Vector2 _origin;
    private float _scale = 1;

    public ScoutReportView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        _backdrop = new ColorRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new ShaderMaterial
            {
                Shader = new Shader { Code = BlurShader },
            },
        };
        ((ShaderMaterial)_backdrop.Material).SetShaderParameter("dim", Dim);
        _backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_backdrop);
        _sheet = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _sheet.Draw += DrawSheet;
        AddChild(_sheet);
        SetFont(font);
    }

    /// <summary>Option → Palette: the cards in <paramref name="palette"/>'s colours.</summary>
    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        _sheet.QueueRedraw();
    }

    /// <summary>Option → Fonts: the overlay's text in the chosen font.</summary>
    public void SetFont(GameFont font)
    {
        _font = DeploymentTableView.LoadFont(font);
        _bold.BaseFont = _font;
        _sheet.QueueRedraw();
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits (read as the overlay opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _sheet.QueueRedraw();
    }

    /// <summary>Lays the overlay out over the area whose layout pixel (0, 0) is at <paramref name="origin"/>, <paramref name="scale"/> window pixels to a layout pixel.</summary>
    public void Place(Vector2 origin, float scale)
    {
        if (origin == _origin && scale == _scale)
            return;
        (_origin, _scale) = (origin, scale);
        _sheet.Position = origin;
        _sheet.Size = new Vector2(1280, 800) * scale;
        _sheet.QueueRedraw();
    }

    /// <summary>Shows <paramref name="report"/>, drawn again only when it changed.</summary>
    public void Show(ScoutReport report)
    {
        if (report == _report)
            return;
        _report = report;
        _advisor ??= _faces.Picture(ScoutReport.AdvisorCg) is { } maris ? ImageTexture.CreateFromImage(maris) : null;
        if (report.NinjaCg != _portraitCg)
        {
            _portraitCg = report.NinjaCg;
            _portrait = _faces.Picture(report.NinjaCg) is { } image ? ImageTexture.CreateFromImage(image) : null;
        }
        _sheet.QueueRedraw();
    }

    private void DrawSheet()
    {
        if (_report is not { } report || _font is null)
            return;
        float s = _scale;
        Rect2 At(Rect2 area) => new(area.Position * s, area.Size * s);

        if (_portrait is not null)
        {
            var area = At(PortraitArea);
            DrawShadow(area, s);
            _sheet.DrawTextureRect(_portrait, area, false);
        }

        var balloon = At(BalloonArea);
        var ink = SpeechBalloon.DrawBalloon(_sheet, balloon, tailLeft: true, s);
        int size = Px(BalloonText);
        var line = balloon.Position + new Vector2(BalloonPad * s, 18 * s);
        _sheet.DrawString(_bold, line + new Vector2(0, _bold.GetAscent(size)), $"The Approaching Enemy  [ {report.Army} ]",
            HorizontalAlignment.Left, balloon.Size.X - 2 * BalloonPad * s, size, ink);
        line.Y += size * 1.4f;
        _sheet.DrawString(_font, line + new Vector2(0, _font.GetAscent(size)),
            $"Commanders {report.Commanders}      Total troops {Number(report.Troops)}",
            HorizontalAlignment.Left, balloon.Size.X - 2 * BalloonPad * s, size, ink);

        for (int slot = 1; slot <= 4; slot++)
            DrawCard(At(CardArea(slot)), slot, report.Places[slot - 1], s);
        int caption = Px(CaptionText);
        _sheet.DrawString(_font, new Vector2(Columns[0], CaptionY) * s + new Vector2(0, _font.GetAscent(caption)), "front row",
            HorizontalAlignment.Left, -1, caption, _colours.Muted);
        _sheet.DrawString(_font, new Vector2(Columns[1], CaptionY) * s + new Vector2(0, _font.GetAscent(caption)), "back row",
            HorizontalAlignment.Left, -1, caption, _colours.Muted);

        DrawComment(report.Comment, size, s);
    }

    /// <summary>
    /// Maris's comment at the report's foot (owner, 2026-10-16, variant A of four): her portrait in the bottom right corner,
    /// her balloon from the cards' left edge to her with its tail at her, as the ninja's at the top, one line per line of
    /// the script's.
    /// </summary>
    private void DrawComment(string comment, int size, float s)
    {
        if (comment == "")
            return;
        var portrait = new Rect2(AdvisorArea.Position * s, AdvisorArea.Size * s);
        if (_advisor is not null)
        {
            DrawShadow(portrait, s);
            _sheet.DrawTextureRect(_advisor, portrait, false);
        }
        string[] lines = comment.Split('\n');
        float lineHeight = size * 1.4f;
        float height = Math.Max(70 * s, 2 * 18 * s + (lines.Length - 1) * lineHeight + size);
        var box = new Rect2(CommentLeft * s, portrait.Position.Y + CommentTop * s,
            portrait.Position.X - CommentGap * s - CommentLeft * s, height);
        var ink = SpeechBalloon.DrawBalloon(_sheet, box, tailLeft: false, s);
        var line = box.Position + new Vector2(BalloonPad * s, 18 * s);
        foreach (string text in lines)
        {
            _sheet.DrawString(_font!, line + new Vector2(0, _font!.GetAscent(size)), text,
                HorizontalAlignment.Left, box.Size.X - 2 * BalloonPad * s, size, ink);
            line.Y += lineHeight;
        }
    }

    /// <summary>A card as our battle cards look (the face on the right, as the enemy's): place and name, troop type, "Troops" and the number large; an empty place "n  empty".</summary>
    private void DrawCard(Rect2 card, int slot, ScoutedCommander? commander, float s)
    {
        DrawShadow(card, s);
        var box = new StyleBoxFlat { BgColor = commander is null ? _colours.Back : _colours.Card, BorderColor = _colours.Border };
        box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(2 * s)));
        _sheet.DrawStyleBox(box, card);
        int text = Px(TextSize);
        if (commander is null)
        {
            string empty = $"{slot}  empty";
            var emptyWidth = _font!.GetStringSize(empty, HorizontalAlignment.Left, -1, text).X;
            _sheet.DrawString(_font, card.GetCenter() + new Vector2(-emptyWidth / 2, _font.GetAscent(text) - text / 2f), empty,
                HorizontalAlignment.Left, -1, text, _colours.Muted);
            return;
        }
        float pad = 4 * s;
        int face = (int)Math.Round(FaceSize * s);
        var faceAt = new Vector2(card.End.X - pad * 2 - face, card.Position.Y + (card.Size.Y - face) / 2);
        if (commander.Picture != 0 && _faces.SquareTexture(commander.Picture, _faces.CropOf(commander.Picture, _crops), face) is { } square)
            _sheet.DrawTextureRect(square, new Rect2(faceAt, new Vector2(face, face)), false);

        float left = card.Position.X + 3 * pad, width = faceAt.X - left - 2 * pad;
        float top = card.Position.Y + 2 * pad;
        void Text(string value, float y, Color colour, Font font, int fontSize, float x = 0) =>
            _sheet.DrawString(font, new Vector2(left + x, top + y * s + font.GetAscent(fontSize)), value,
                HorizontalAlignment.Left, width - x, fontSize, colour, TextServer.JustificationFlag.None);
        Text(slot.ToString(CultureInfo.InvariantCulture), 0, _colours.Muted, _font!, text);
        Text(commander.Name, 0, _colours.Text, _font!, text, 22 * s);
        Text(commander.TroopType == "" ? "—" : commander.TroopType, 22, _colours.Muted, _font!, text);
        Text("Troops", 52, _colours.Muted, _font!, text);
        Text(commander.Troops == 0 ? "none" : Number(commander.Troops), 72, _colours.Text, _bold, Px(NumberSize));
    }

    private void DrawShadow(Rect2 area, float s)
    {
        var shadow = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            ShadowColor = CardShadow,
            ShadowSize = (int)Math.Round(8 * s),
            ShadowOffset = new Vector2(0, 4 * s),
        };
        _sheet.DrawStyleBox(shadow, area);
    }

    private int Px(float size) => Math.Max(8, (int)Math.Round(size * _scale));

    private static string Number(int n) => n.ToString("N0", CultureInfo.InvariantCulture);
}
