using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The bracket holding an army's front row on the phone held upright (<see cref="PhoneBracket"/>; docs/ui/phone.md, §31-§32):
/// a gold line across the row's outer side, its ends turned to the cards, its arms bending to the head in the middle, in
/// the name's gold, which points the way the army faces — up at the province on the enemy attack (PA1f2a) and on the
/// ninja's scout report (PB4sf2m2), down as our army in the battle on the deployment table (PD2f2). The PC's <see cref="EnemyAttackView"/> bracket turned. Drawn in a
/// <see cref="PhoneUnits"/> drawing; the positions in the canvas's units, as the layouts give them.
/// </summary>
internal static class FacingBracket
{
    /// <summary>Draws the bracket from <paramref name="x0"/> to <paramref name="x1"/> at <paramref name="line"/>, its ends to the cards' edge at <paramref name="cards"/>; <paramref name="down"/> points the head down, else up.</summary>
    public static void Draw(CanvasItem c, PhoneUnits u, UiPalette ui, float k, float x0, float x1, float middle, float line, float cards,
        bool down)
    {
        float way = down ? 1 : -1;
        float left = x0 / u.K, right = x1 / u.K, mid = middle / u.K, y = line / u.K;
        float ends = cards / u.K + way * u.Px(PhoneBracket.EndsShort);
        float arm = u.Px(PhoneBracket.ArmEnd), bendIn = u.Px(PhoneBracket.BendIn), bend = y + way * u.Px(PhoneBracket.BendOut);
        float width = Lacquer.Line(u.Px(PhoneBracket.Width), k);
        c.DrawPolyline([new(left, ends), new(left, y), new(mid - arm, y), new(mid - bendIn, bend)], ui.Gold, width, true);
        c.DrawPolyline([new(right, ends), new(right, y), new(mid + arm, y), new(mid + bendIn, bend)], ui.Gold, width, true);
        float half = u.Px(PhoneBracket.HeadHalf), headBase = y + way * u.Px(PhoneBracket.HeadBase);
        Lacquer.DrawSmoothPolygon(c, [new(mid - half, headBase), new(mid, y + way * u.Px(PhoneBracket.HeadTip)), new(mid + half, headBase)],
            ui.Name, k);
    }
}
