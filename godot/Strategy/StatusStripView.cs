using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our status strip along the top of the map (docs/ui/command-screen.md, "The status strip"; the owner's
/// mock-up C with groups d, 2026-10-16): the first row is the status window <c>COMMAND.ADV</c> prints at a
/// right click — the nation with its flag, its money and troops, the date with LP; the second the week's
/// commands as tags, lit while free, grouped under brackets with their captions (Castle, Subordinates, Map).
/// A left click on it reaches nothing under it. Laid out in the 1280×800 screen's pixels, scaled
/// with the picture.
/// </summary>
internal sealed partial class StatusStripView : Control
{
    private const float Width = 980, Height = 92, Top = 6;

    /// <summary>The flags' CGs: <c>Prov.Owner + 819</c>, Leazas 820 (the map's markers).</summary>
    private const int LeazasFlag = 820;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private FontFile _gothic;
    private StatusStrip? _strip;
    private ImageTexture? _flag;
    private float _k = 1;                           // window pixels to a screen pixel

    public StatusStripView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>Shows <paramref name="strip"/>, drawn again only when what it shows changed.</summary>
    public void Show(StatusStrip strip)
    {
        Visible = true;
        if (_strip?.Key == strip.Key)
            return;
        _strip = strip;
        _flag ??= _pictures.Picture(LeazasFlag) is { } image ? ImageTexture.CreateFromImage(image) : null;
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        _strip = null;
    }

    /// <summary>Where the events on the strip go but a left click's: the screen's input (a right click opens the menu as anywhere).</summary>
    public Action<InputEvent>? Forward { get; set; }

    public override bool _HasPoint(Vector2 point) => Visible && new Rect2(Left, Top, Width, Height).HasPoint(point / _k);

    /// <summary>A left click on the strip reaches no province or plate under it; the rest is the screen's.</summary>
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left })
            Forward?.Invoke(e);
        AcceptEvent();
    }

    /// <summary>Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    private static float Left => (1280 - Width) / 2;

    public override void _Draw()
    {
        if (_strip is not { } s)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float x = Left, y = Top;
        Lacquer.DrawPanel(this, new Rect2(x, y, Width, Height), _k);
        // The first row: the nation with its flag, the status window's figures, the date.
        var row = new Rect2(x, y + 4, Width, 34);
        float cx = x + 16;
        if (_flag is not null && !s.Thieves)
        {
            DrawTextureRect(_flag, new Rect2(cx, row.GetCenter().Y - 9.5f, 24, 19), false);
            cx += 32;
        }
        DrawString(_brush, new Vector2(cx, Lacquer.Baseline(row, _brush, 18)), s.Nation, fontSize: 18, modulate: Lacquer.Name);
        cx += _brush.GetStringSize(s.Nation, fontSize: 18).X + 26;
        foreach (var (label, value) in s.Status)
        {
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, 13)), label, fontSize: 13, modulate: Lacquer.Label);
            cx += _gothic.GetStringSize(label, fontSize: 13).X + 8;
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, 15)), value, fontSize: 15, modulate: Lacquer.Value);
            cx += _gothic.GetStringSize(value, fontSize: 15).X + 22;
        }
        DrawString(_cinzel, new Vector2(cx, Lacquer.Baseline(row, _cinzel, 13)), s.Date, fontSize: 13, modulate: Lacquer.Subtitle);
        DrawLine(new Vector2(x + 14, y + 40), new Vector2(x + Width - 14, y + 40), Lacquer.DimGold, Lacquer.Line(1, _k));
        // The second row: the week's commands, grouped under brackets with their captions.
        cx = x + 20;
        foreach (var group in s.Groups)
        {
            float start = cx;
            foreach (var use in group.Uses)
            {
                float cw = _gothic.GetStringSize(use.Text, fontSize: 13).X + 18;
                Lacquer.DrawTag(this, _gothic, new Rect2(cx, y + 46, cw, 20), use.Text, ColourOf(use.Action), use.Free, _k);
                cx += cw + 5;
            }
            float end = cx - 5, by = y + 76, mid = (start + end) / 2;
            DrawPolyline([new(start, by - 3), new(start, by), new(end, by), new(end, by - 3)], Lacquer.DimGold, Lacquer.Line(1, _k));
            float tw = _cinzel.GetStringSize(group.Caption, fontSize: 11).X;
            DrawRect(new Rect2(mid - tw / 2 - 6, by - 2, tw + 12, 4), Lacquer.Panel with { A = 1 });
            DrawString(_cinzel, new Vector2(mid - tw / 2, by + (_cinzel.GetAscent(11) - _cinzel.GetDescent(11)) / 2), group.Caption,
                fontSize: 11, modulate: Lacquer.Subtitle);
            cx += 30;
        }
    }

    /// <summary>A command's tag colour (ours; the same as the command panel's and the province menu's tags).</summary>
    private static Color ColourOf(WeekAction action) => action switch
    {
        WeekAction.Build => Color.Color8(46, 62, 104),
        WeekAction.Harem => Color.Color8(128, 44, 70),
        WeekAction.Trade => Color.Color8(36, 92, 96),
        WeekAction.City => Color.Color8(150, 52, 36),
        WeekAction.Attack => Color.Color8(128, 30, 30),
        WeekAction.Dungeon => Color.Color8(36, 92, 96),
        _ => Color.Color8(70, 96, 52),                                  // Subordinates, Troops, Talk
    };
}
