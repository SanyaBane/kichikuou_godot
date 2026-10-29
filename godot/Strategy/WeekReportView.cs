using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our week report over the map (docs/ui/turn-start.md, "The week report"; the owner's mock-up C, 2026-10-18): the
/// banner, the budget as an account (last week's, plus the revenue, minus each expense, the new one), the troops
/// replenished, in the lacquer and gold of our map panels; an animated triangle at the bottom right asks for the
/// click (no caption, owner). The thieves' week has the banner alone. It takes no input: the screen's own click
/// answers it (<see cref="GameScreen"/>). On the phone held landscape the card is drawn larger over the map dimmed
/// (docs/ui/phone.md, §14, LW1). Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class WeekReportView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private const float CardWidth = 600, Pad = 34, Top = 26, LinePitch = 42, TotalHeight = 52, RowHeight = 46;
    private const int TitleSize = 32, DateSize = 17, CaptionSize = 13, LabelSize = 16, SubSize = 12, ValueSize = 20, UnitSize = 13,
        TotalSize = 30;

    /// <summary>Leazas's flag (the map's markers' CG 820).</summary>
    private const int LeazasFlag = 820;

    private static readonly Color Gain = Color.Color8(159, 201, 138), Loss = Color.Color8(217, 118, 94), Dim = new(0, 0, 0, 0.43f);

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontVariation _bold = Lacquer.Cinzel(700);
    private readonly FontVariation _heavy = Lacquer.Cinzel(800);
    private WeekReport? _report;
    private ImageTexture? _flag;
    private float _k = 1;                           // window pixels to a screen pixel

    public WeekReportView(FacePictures pictures)
    {
        _pictures = pictures;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
    }

    public void Open(WeekReport report)
    {
        _report = report;
        _flag ??= _pictures.Picture(LeazasFlag) is { } image ? ImageTexture.CreateFromImage(image) : null;
        Visible = true;
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        _report = null;
    }

    /// <summary>Lays the report over the picture (<paramref name="area"/>, in its parent's coordinates).</summary>
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

    /// <summary>The triangle moves: drawn again every frame while shown.</summary>
    public override void _Process(double delta)
    {
        if (Visible)
            QueueRedraw();
    }

    private float CardHeight(WeekReport r)
    {
        float h = Top + 92;                                             // the header and its divider
        if (r.Revenue is not null)
            h += 26 + LinePitch * (2 + r.Expenses.Count) + 10 + TotalHeight;
        if (r.Troops is not null)
            h += 22 + 18 + 10 + RowHeight;
        return h + 46;                                                  // the triangle's foot
    }

    public override void _Draw()
    {
        if (_report is not { } r)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        float height = CardHeight(r);
        if (Device.Held(this) is { } phone)
        {
            // The phone (docs/ui/phone.md, §14, LW1; upright §8, PW1): the card larger in the frame's middle, the map dimmed.
            DrawRect(new Rect2(0, 0, 1280, 800), Dim);
            float s = PhoneScenes.ReportScale(phone, CardWidth, height);
            DrawSetTransform(new Vector2(640, 400) * _k * (1 - s), 0, new Vector2(_k * s, _k * s));
        }
        var card = new Rect2((1280 - CardWidth) / 2, (800 - height) / 2, CardWidth, height);
        Ui.DrawPanel(this, card, _k);
        float x0 = card.Position.X + Pad, x1 = card.End.X - Pad, y = card.Position.Y + Top;

        // The banner: Leazas's flag (none for the thieves), the title, the date.
        float tx = x0;
        if (_flag is not null && !r.Thieves)
        {
            DrawTextureRect(_flag, new Rect2(x0, y + 8, 48, 38), false);
            tx += 66;
        }
        DrawString(_brush, new Vector2(tx + 1, y + 34 + 2), r.Title, fontSize: TitleSize, modulate: Colors.Black);
        DrawString(_brush, new Vector2(tx, y + 34), r.Title, fontSize: TitleSize, modulate: Ui.Name);
        DrawString(_bold, new Vector2(tx, y + 60), r.Date, fontSize: DateSize, modulate: Ui.Name);
        y += 88;
        Ui.DrawDivider(this, x0, x1, y, _k);
        y += 4;

        if (r.Revenue is { } revenue)
        {
            // The treasury as an account (ours): last week's budget, the revenue, each expense, the new budget.
            y += 26;
            Caption("TREASURY", x0, y);
            LedgerLine(x0, x1, ref y, "", "Budget", "last week", Gold(r.BudgetBefore), Ui.Value);
            LedgerLine(x0, x1, ref y, "+", revenue.Label, "provinces", "+" + Gold(revenue.Amount), Gain);
            foreach (var expense in r.Expenses)
                LedgerLine(x0, x1, ref y, Figures.Minus, expense.Label, "special expense", Figures.Minus + Gold(-expense.Amount), Loss);
            y += 10;
            var total = new Rect2(x0, y, x1 - x0, TotalHeight);
            DrawRect(total, Ui.Row);
            DrawRect(total, Ui.Gold, false, Lacquer.Line(1, _k));
            DrawString(_bold, new Vector2(x0 + 16, Lacquer.Baseline(total, _bold, 18)), "Budget", fontSize: 18, modulate: Ui.Name);
            Amount(x1 - 16, Lacquer.Baseline(total, _heavy, TotalSize), Gold(r.BudgetAfter ?? r.BudgetBefore), _heavy, TotalSize, Ui.Name, "K GOLD");
            y += TotalHeight;
        }
        if (r.Troops is { } troops)
        {
            y += 22 + 18;
            Caption("TROOPS", x0, y);
            var row = new Rect2(x0, y + 10, x1 - x0, RowHeight);
            Ui.DrawRowBox(this, row, false, _k);
            DrawString(_cinzel, new Vector2(x0 + 16, Lacquer.Baseline(row, _cinzel, LabelSize)), troops.Label, fontSize: LabelSize, modulate: Ui.Text);
            Amount(x1 - 16, Lacquer.Baseline(row, _bold, 22), "+" + Figures.Number(troops.Amount), _bold, 22, Gain, null);
        }
        Ui.DrawTriangle(this, new Vector2(card.End.X - Pad - 2, card.End.Y - 26), _k);
    }

    private void Caption(string text, float x, float y) =>
        DrawString(_bold, new Vector2(x, y), text, fontSize: CaptionSize, modulate: Ui.Subtitle);

    /// <summary>One line of the account: its sign, label and small sub-label at the left, its amount at the right, a dotted rule under it.</summary>
    private void LedgerLine(float x0, float x1, ref float y, string sign, string label, string sub, string amount, Color colour)
    {
        var line = new Rect2(x0, y + 6, x1 - x0, LinePitch);
        float baseline = Lacquer.Baseline(line, _bold, ValueSize);
        DrawString(_bold, new Vector2(x0 + 4, baseline), sign, fontSize: LabelSize, modulate: Ui.Subtitle);
        DrawString(_cinzel, new Vector2(x0 + 24, baseline), label, fontSize: LabelSize, modulate: Ui.Text);
        float lw = _cinzel.GetStringSize(label, fontSize: LabelSize).X;
        DrawString(_cinzel, new Vector2(x0 + 24 + lw + 8, baseline), sub, fontSize: SubSize, modulate: Ui.Label);
        Amount(x1 - 4, baseline, amount, _bold, ValueSize, colour, "K GOLD");
        for (float x = x0; x < x1; x += 5)
            DrawLine(new Vector2(x, line.End.Y), new Vector2(Math.Min(x + 2, x1), line.End.Y), Ui.DimGold, Lacquer.Line(1, _k));
        y += LinePitch;
    }

    /// <summary>An amount ending at <paramref name="right"/>, with its unit after it in a small, quiet face.</summary>
    private void Amount(float right, float baseline, string amount, Font font, int size, Color colour, string? unit)
    {
        float uw = unit is null ? 0 : _cinzel.GetStringSize(unit, fontSize: UnitSize).X;
        if (unit is not null)
            DrawString(_cinzel, new Vector2(right - uw, baseline), unit, fontSize: UnitSize, modulate: Ui.Quiet);
        float aw = font.GetStringSize(amount, fontSize: size).X;
        DrawString(font, new Vector2(right - uw - (unit is null ? 0 : 6) - aw, baseline), amount, fontSize: size, modulate: colour);
    }

    /// <summary>An amount in K GOLD, its thousands separated as Option chose: 20200 → "20,200".</summary>
    private static string Gold(int amount) => Figures.Number(amount);
}
