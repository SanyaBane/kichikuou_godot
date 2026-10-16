using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our status strip above the map, as wide as it (docs/ui/command-screen.md, "The status strip"; the owner's
/// mock-up C with groups d, 2026-10-16; the band <see cref="GameScreen"/> keeps for it), in one row (owner, 2026-10-16):
/// the status window <c>COMMAND.ADV</c> prints at a right click — the nation's flag (not its name), its money and
/// troops; the week's commands as tags, lit while free, grouped under brackets with their captions (Castle,
/// Subordinates, Map); the date with LP at the right, large and bright.
/// Laid out in a 1280-wide band of the screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class StatusStripView : Control
{
    private const float Width = 1280;

    /// <summary>The band's height in the screen's pixels (1280 wide).</summary>
    public const float Height = 50;

    private const int CaptionSize = 10, DateSize = 18, LabelSize = 14, ValueSize = 17;

    /// <summary>The flags' CGs: <c>Prov.Owner + 819</c>, Leazas 820 (the map's markers).</summary>
    private const int LeazasFlag = 820;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private readonly FontVariation _date = Lacquer.Cinzel(700);
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

    public override bool _HasPoint(Vector2 point) => Visible && new Rect2(0, 0, Width, Height).HasPoint(point / _k);

    /// <summary>A left click on the strip reaches no province or plate under it; the rest is the screen's.</summary>
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left })
            Forward?.Invoke(e);
        AcceptEvent();
    }

    /// <summary>Lays the strip in its band above the picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / Width;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_strip is not { } s)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        Lacquer.DrawPanel(this, new Rect2(0, 0, Width, Height), _k);
        // One row (owner, 2026-10-16): the flag, the status window's figures, the week's commands, the date.
        var row = new Rect2(0, 10, Width, 22);
        float cx = 18;
        if (_flag is not null && !s.Thieves)
        {
            DrawTextureRect(_flag, new Rect2(cx, row.GetCenter().Y - 9.5f, 24, 19), false);
            cx += 34;
        }
        foreach (var (label, value) in s.Status)
        {
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, LabelSize)), label, fontSize: LabelSize, modulate: Lacquer.Label);
            cx += _gothic.GetStringSize(label, fontSize: LabelSize).X + 8;
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, ValueSize)), value, fontSize: ValueSize, modulate: Lacquer.Value);
            cx += _gothic.GetStringSize(value, fontSize: ValueSize).X + 20;
        }
        // The week's commands as tags, grouped under brackets with their captions.
        cx += 6;
        foreach (var group in s.Groups)
        {
            float start = cx;
            foreach (var use in group.Uses)
            {
                float cw = _gothic.GetStringSize(use.Text, fontSize: 13).X + 16;
                Lacquer.DrawTag(this, _gothic, new Rect2(cx, row.Position.Y, cw, row.Size.Y), use.Text, ColourOf(use.Action), use.Free, _k);
                cx += cw + 4;
            }
            float end = cx - 4, by = row.End.Y + 6, mid = (start + end) / 2;
            DrawPolyline([new(start, by - 3), new(start, by), new(end, by), new(end, by - 3)], Lacquer.DimGold, Lacquer.Line(1, _k));
            float tw = _cinzel.GetStringSize(group.Caption, fontSize: CaptionSize).X;
            DrawRect(new Rect2(mid - tw / 2 - 5, by - 2, tw + 10, 4), Lacquer.Panel with { A = 1 });
            DrawString(_cinzel, new Vector2(mid - tw / 2, by + (_cinzel.GetAscent(CaptionSize) - _cinzel.GetDescent(CaptionSize)) / 2),
                group.Caption, fontSize: CaptionSize, modulate: Lacquer.Subtitle);
            cx += 22;
        }
        // The date at the right, large and bright (owner, 2026-10-16: the small dim one was lost on the lacquer);
        // smaller only when the row would not leave it room.
        int size = DateSize;
        float dw = _date.GetStringSize(s.Date, fontSize: size).X;
        while (size > 12 && Width - 20 - dw < cx)
            dw = _date.GetStringSize(s.Date, fontSize: --size).X;
        DrawString(_date, new Vector2(Width - 20 - dw, Lacquer.Baseline(new Rect2(0, 0, Width, Height), _date, size)), s.Date,
            fontSize: size, modulate: Lacquer.Name);
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
