using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The bracket holding an army's front row, which points the way the army faces: a gold line along the row's outer side,
/// its ends turned to the cards, its arms bending to a head in the middle in the name's gold. The PC's and the tablet's
/// (<see cref="DrawPc"/>: the owner's F5 on the deployment table, docs/ui/deployment-table.md, "The facing mark"; the enemy
/// attack, docs/ui/enemy-attack.md, §8; the scout report, Cf1, docs/ui/phone.md, §47) holds a column of cards from its
/// side. On the phone (<see cref="PhoneBracket"/>, the PC's scaled with the cards) upright (<see cref="Draw"/>; §31-§33) it
/// holds a row from above or below — up at the province on the enemy attack (PA1f2a) and on the ninja's scout report
/// (PB4sf2m2), down as our army in the battle on the deployment table (PD2f2) —, held landscape (<see cref="DrawSide"/>;
/// §47) a column from its side as on the PC: right on the deployment table (LD2f1), left at the province on the enemy
/// attack (LA2f1) and on the scout report (LB3sf1).
/// </summary>
internal static class FacingBracket
{
    /// <summary>The PC's figures, layout pixels: the line off the cards, its ends off them, the head's tip off them; the arms, the head, the width.</summary>
    private const float PcLine = 7, PcEnds = 2, PcTip = 40, PcArm = 16, PcBendIn = 6, PcBendOut = 10, PcHeadBase = 6, PcHeadHalf = 14,
        PcWidth = 2.5f;

    /// <summary>
    /// The phone held upright: the bracket from <paramref name="x0"/> to <paramref name="x1"/> at <paramref name="line"/>, its
    /// ends to the cards' edge at <paramref name="cards"/>; <paramref name="down"/> points the head down, else up. Drawn in a
    /// <see cref="PhoneUnits"/> drawing; the positions in the canvas's units, as the layouts give them.
    /// </summary>
    public static void Draw(CanvasItem c, PhoneUnits u, UiPalette ui, float k, float x0, float x1, float middle, float line, float cards,
        bool down) =>
        DrawPhone(c, u, ui, k, x0, x1, middle, line, cards, down ? 1 : -1, across: false);

    /// <summary>
    /// The phone held landscape: the bracket from <paramref name="y0"/> to <paramref name="y1"/> at <paramref name="line"/>, its
    /// ends to the cards' edge at <paramref name="cards"/>; <paramref name="right"/> points the head right, else left. Drawn in a
    /// <see cref="PhoneUnits"/> drawing; the positions in the canvas's units, as the layouts give them.
    /// </summary>
    public static void DrawSide(CanvasItem c, PhoneUnits u, UiPalette ui, float k, float y0, float y1, float middle, float line, float cards,
        bool right) =>
        DrawPhone(c, u, ui, k, y0, y1, middle, line, cards, right ? 1 : -1, across: true);

    /// <summary>
    /// The PC and the tablet: the bracket holding the column of cards whose outer edge is <paramref name="edge"/>, from
    /// <paramref name="y0"/> to <paramref name="y1"/>, in layout pixels times <paramref name="scale"/>; <paramref name="right"/>
    /// points the head right, else left; <paramref name="k"/> the canvas's scale.
    /// </summary>
    public static void DrawPc(CanvasItem c, UiPalette ui, float k, float edge, float y0, float y1, bool right, float scale = 1)
    {
        float way = right ? 1 : -1;
        DrawCore(c, ui, k, y0, y1, (y0 + y1) / 2, edge + way * PcLine * scale, edge + way * PcEnds * scale, way, true, PcArm * scale,
            PcBendIn * scale, PcBendOut * scale, PcHeadBase * scale, (PcTip - PcLine) * scale, PcHeadHalf * scale, Lacquer.Line(PcWidth * scale, k));
    }

    private static void DrawPhone(CanvasItem c, PhoneUnits u, UiPalette ui, float k, float a0, float a1, float middle, float line, float cards,
        float way, bool across) =>
        DrawCore(c, ui, k, a0 / u.K, a1 / u.K, middle / u.K, line / u.K, cards / u.K + way * u.Px(PhoneBracket.EndsShort), way, across,
            u.Px(PhoneBracket.ArmEnd), u.Px(PhoneBracket.BendIn), u.Px(PhoneBracket.BendOut), u.Px(PhoneBracket.HeadBase),
            u.Px(PhoneBracket.HeadTip), u.Px(PhoneBracket.HeadHalf), Lacquer.Line(u.Px(PhoneBracket.Width), k));

    /// <summary>
    /// The bracket along the row from <paramref name="a0"/> to <paramref name="a1"/>, its line at <paramref name="line"/>
    /// across it, its ends at <paramref name="ends"/>; <paramref name="way"/> the head's side of the line; <paramref name="across"/>
    /// runs the row down the screen (the line upright), else across it.
    /// </summary>
    private static void DrawCore(CanvasItem c, UiPalette ui, float k, float a0, float a1, float mid, float line, float ends, float way,
        bool across, float arm, float bendIn, float bendOut, float headBase, float headTip, float half, float width)
    {
        Vector2 P(float along, float off) => across ? new Vector2(off, along) : new Vector2(along, off);
        float bend = line + way * bendOut, baseAt = line + way * headBase;
        c.DrawPolyline([P(a0, ends), P(a0, line), P(mid - arm, line), P(mid - bendIn, bend)], ui.Gold, width, true);
        c.DrawPolyline([P(a1, ends), P(a1, line), P(mid + arm, line), P(mid + bendIn, bend)], ui.Gold, width, true);
        Lacquer.DrawSmoothPolygon(c, [P(mid - half, baseAt), P(mid, line + way * headTip), P(mid + half, baseAt)], ui.Name, k);
    }
}
