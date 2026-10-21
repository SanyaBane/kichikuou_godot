using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The plate after a troop order (the owner's R4/R5, docs/ui/subordinates.md, §6), in the middle of the screen with the
/// screen dimmed a little under it: the unit's face, the order's title, the unit and its troop type, the troops before
/// and after with the men gained; right of a rule the cost and the budget left. Gone after a few seconds or at a click,
/// which it takes. Our Subordinates screen and our deployment screen's Replenish (owner's decision 27) show it. Laid out
/// in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class OrderResultPlate : Control
{
    private static readonly Rect2 PlateArea = new(250, 315, 780, 150);

    /// <summary>How long the plate stays (seconds), unless a click closes it first.</summary>
    private const double Seconds = 2.5;

    private static readonly Color PlateFill = Color.Color8(22, 16, 12, 248);
    private static readonly Color PlateDim = new(0, 0, 0, 0.27f);

    private readonly FacePictures _faces;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private FaceCrops _crops = new();
    private SubordinateResult? _result;
    private double _left;
    private float _k = 1;                               // window pixels to a screen pixel
    private int _faceCg = -1, _facePixels;
    private ImageTexture? _face;

    public OrderResultPlate(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
    }

    /// <summary>Whether the plate shows.</summary>
    public bool Showing => _result is not null;

    /// <summary>Option → Display (the arrows are in the game's font).</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits from Debug → Portraits.</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _faceCg = -1;
    }

    /// <summary>Lays the plate's layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
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

    /// <summary>What an order did, on the plate for a few seconds (null takes it away).</summary>
    public void Show(SubordinateResult? result)
    {
        _result = result;
        _left = result is null ? 0 : Seconds;
        Visible = result is not null;
        MouseFilter = result is null ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_result is not null && (_left -= delta) <= 0)
            Show(null);
    }

    /// <summary>Any press closes it.</summary>
    public override void _GuiInput(InputEvent e)
    {
        if (_result is not null && e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right })
            Show(null);
        AcceptEvent();
    }

    private float Line(float width) => Lacquer.Line(width, _k);

    public override void _Draw()
    {
        if (_result is not { } r)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawRect(new Rect2(0, 0, 1280, 800), PlateDim);
        var box = PlateArea;
        Lacquer.DrawPanel(this, box, _k, PlateFill);
        float size = box.Size.Y - 52;
        var faceRect = new Rect2(box.Position + new Vector2(26, 26), new Vector2(size, size));
        if (Face(r.FaceCg, size * _k) is { } face)
            DrawTextureRect(face, faceRect, false);
        DrawRect(faceRect, Lacquer.Gold, false, Line(1));

        float x = faceRect.End.X + 24, y = box.Position.Y;
        Rect2 Band(float middle, float height = 30) => new(x, middle - height / 2, 400, height);
        DrawString(_brush, new Vector2(x, Lacquer.Baseline(Band(y + 42, 40), _brush, 28)), r.Title, fontSize: 28, modulate: Lacquer.Name);
        DrawString(_cinzel, new Vector2(x + 2, Lacquer.Baseline(Band(y + 76), _cinzel, 15)), r.Who, fontSize: 15, modulate: Lacquer.Subtitle);
        float w = DrawFigures(x + 2, Band(y + 112), r.TroopsLine, 22, Lacquer.Value);
        DrawString(_cinzel, new Vector2(x + 2 + w + 14, Lacquer.Baseline(Band(y + 113), _cinzel, 18)), r.Gain, fontSize: 18,
            modulate: r.Gain.StartsWith('−') ? Lacquer.Minus : Lacquer.Plus);

        float rx = box.End.X - 232, my = box.GetCenter().Y, right = box.End.X - 28;
        DrawLine(new Vector2(rx, my - 40), new Vector2(rx, my + 40), Lacquer.DimGold, Line(1));
        var cost = new Rect2(rx + 20, my - 33, right - rx - 20, 30);
        var budget = new Rect2(rx + 20, my + 3, right - rx - 20, 30);
        DrawString(_cinzel, new Vector2(cost.Position.X, Lacquer.Baseline(cost, _cinzel, 13)), "Cost", fontSize: 13, modulate: Lacquer.Label);
        RightString(r.CostLine, 18, Lacquer.Minus, right, cost);
        DrawString(_cinzel, new Vector2(budget.Position.X, Lacquer.Baseline(budget, _cinzel, 13)), "Budget", fontSize: 13, modulate: Lacquer.Label);
        RightString(r.BudgetLine, 18, Lacquer.Name, right, budget);
    }

    private void RightString(string text, int size, Color colour, float right, Rect2 band)
    {
        float w = _cinzel.GetStringSize(text, fontSize: size).X;
        DrawString(_cinzel, new Vector2(right - w, Lacquer.Baseline(band, _cinzel, size)), text, fontSize: size, modulate: colour);
    }

    /// <summary>Figures in Cinzel from <paramref name="x"/>, their arrows in the game's font (Cinzel has none); returns the width.</summary>
    private float DrawFigures(float x, Rect2 band, string text, int size, Color colour)
    {
        const float ArrowGap = 10;
        float start = x;
        string[] parts = text.Split('→', StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                x += ArrowGap;
                DrawString(_gothic, new Vector2(x, Lacquer.Baseline(band, _gothic, size - 2)), "→", fontSize: size - 2, modulate: Lacquer.Gold);
                x += _gothic.GetStringSize("→", fontSize: size - 2).X + ArrowGap;
            }
            DrawString(_cinzel, new Vector2(x, Lacquer.Baseline(band, _cinzel, size)), parts[i], fontSize: size, modulate: colour);
            x += _cinzel.GetStringSize(parts[i], fontSize: size).X;
        }
        return x - start;
    }

    /// <summary>The unit's face cut to its square, made again only when the face or its size in window pixels changes.</summary>
    private ImageTexture? Face(int faceCg, float windowSize)
    {
        if (faceCg == 0)
            return null;
        int pixels = Math.Max(16, (int)Math.Round(windowSize));
        if (faceCg != _faceCg || pixels != _facePixels)
        {
            (_faceCg, _facePixels) = (faceCg, pixels);
            _face = _faces.SquareTexture(faceCg, _faces.CropOf(faceCg, _crops), pixels);
        }
        return _face;
    }
}
