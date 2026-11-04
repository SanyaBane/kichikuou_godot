using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

/// <summary>
/// A battle card's portrait with the unit's stats, always shown (owner, 2026-10-15, after a Sengoku Rance fan mod;
/// docs/ui/battle-screen.md, "The cards' stats"): the face; the player's Battle LV in its bottom-right corner on a
/// see-through dark plate (the face shows through, the text stays readable); "Surging with power" as a badge with an up
/// arrow on its top-right corner, grey while it gives nothing (no troops); the focus a Wait has built as "◆ n" in its
/// top-left corner on the same plate, in the accent colour (owner, 2026-10-15, mock-up 5B); and under it the four numbers of
/// <see cref="BattleCardStats"/> — troop attack, troop defense, magic, attacks — a modifier tinting its number (green
/// up, red down), the commander's own attack and defense (no troops) in light blue. Sizes are layout pixels.
/// </summary>
internal sealed partial class CardPortrait : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    // The touch mode's cards are narrower (BattleBoardView: the board between the rails low, owner's A, 2026-10-25): the
    // face gives way so that the card's text keeps its size. Read live (docs/ui/phone-build.md, §3).
    // On the phone held landscape the face is the PC's again (160 reference pixels, docs/ui/phone.md, §12, LB2), the four
    // figures in a row of 52 under it at 32.
    // Portrait (docs/ui/phone.md, §6, PB3) the same at the slim card's sizes: the face 174, the figures' row 40 at 28.
    private static bool Phone => Device.Layout == DeviceLayout.PhoneLandscape;
    private static bool Portrait => Device.Layout == DeviceLayout.PhonePortrait;
    public static float Width => Phone || Portrait ? Face : Device.TouchLayout ? 70 : 118;
    public static float Face => Phone ? PhoneBattle.Layout(PhoneBattle.Face) : Portrait ? PhoneBattlePortrait.Layout(PhoneBattlePortrait.Face)
        : Device.TouchLayout ? 66 : 112;
    public static float Height => Face + (Phone ? PhoneBattle.Layout(PhoneBattle.StatsRow)
        : Portrait ? PhoneBattlePortrait.Layout(PhoneBattlePortrait.StatsRow) : 28);
    private static float StatsText => Phone ? PhoneBattle.Layout(PhoneBattle.StatsText)
        : Portrait ? PhoneBattlePortrait.Layout(PhoneBattlePortrait.StatsText) : Device.TouchLayout ? 14 : 17;
    private static float StatsSpan => Device.TouchLayout ? Width : Face;  // the touch mode's numbers across the whole width
    private const float FaceTop = 2, LevelText = 14, BadgeRadius = 10;
    private static readonly Color Up = new(120 / 255f, 210 / 255f, 120 / 255f), Down = new(235 / 255f, 110 / 255f, 100 / 255f);
    private static readonly Color Own = new(170 / 255f, 200 / 255f, 1f);                    // the commander's own values
    private static readonly Color Plate = new(0, 0, 0, 0.55f), Shadow = new(0, 0, 0, 0.85f);
    private static readonly Color BadgeInk = new(40 / 255f, 32 / 255f, 12 / 255f);
    private static readonly Color BadgeOff = new(70 / 255f, 72 / 255f, 78 / 255f), BadgeOffInk = new(150 / 255f, 152 / 255f, 156 / 255f);

    private readonly Texture2D? _face;
    private readonly BattleCardStats? _stats;
    private readonly int _focus;
    private readonly float _scale;

    public CardPortrait(Texture2D? face, BattleCardStats? stats, int focus, float scale)
    {
        (_face, _stats, _focus, _scale) = (face, stats, focus, scale);
        CustomMinimumSize = new Vector2(Width, Height) * scale;
        // The touch mode's smaller face at the card's top, level with its name (owner, 2026-10-25: centred it floated).
        SizeFlagsVertical = TouchMode.On ? SizeFlags.ShrinkBegin : SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        float s = _scale;
        var face = new Rect2(new Vector2((Width - Face) / 2, FaceTop) * s, new Vector2(Face, Face) * s);
        if (_face is not null)
            DrawTextureRect(_face, face, false);
        if (_stats is not { } stats || GetThemeDefaultFont() is not { } font)
            return;

        if (stats.Level is { } level)
        {
            string text = $"Lv {level}";
            int px = Math.Max(6, (int)Math.Round(LevelText * s));
            var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, px);
            var plate = new Rect2(face.End - size - new Vector2(6, 4) * s - new Vector2(3, 3) * s, size + new Vector2(6, 4) * s);
            var box = new StyleBoxFlat { BgColor = Plate };
            box.SetCornerRadiusAll((int)Math.Round(3 * s));
            DrawStyleBox(box, plate);
            DrawString(font, new Vector2(plate.Position.X + 3 * s, plate.Position.Y + 2 * s + font.GetAscent(px)), text,
                HorizontalAlignment.Left, -1, px, Ui.Text);
        }

        if (_focus > 0)
        {
            // A diamond (drawn, so any font will do) and the number, on the Lv plate's see-through dark.
            string text = _focus.ToString();
            int px = Math.Max(6, (int)Math.Round(LevelText * s));
            var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, px);
            float diamond = 10 * s, gap = 4 * s;
            var plate = new Rect2(face.Position + new Vector2(3, 3) * s,
                new Vector2(diamond + gap + size.X, size.Y) + new Vector2(10, 4) * s);
            var box = new StyleBoxFlat { BgColor = Plate };
            box.SetCornerRadiusAll((int)Math.Round(3 * s));
            DrawStyleBox(box, plate);
            var centre = new Vector2(plate.Position.X + 5 * s + diamond / 2, plate.Position.Y + plate.Size.Y / 2);
            DrawColoredPolygon([centre + new Vector2(0, -diamond / 2), centre + new Vector2(diamond / 2, 0),
                centre + new Vector2(0, diamond / 2), centre + new Vector2(-diamond / 2, 0)], Ui.HoverLine);
            DrawString(font, new Vector2(centre.X + diamond / 2 + gap, plate.Position.Y + 2 * s + font.GetAscent(px)), text,
                HorizontalAlignment.Left, -1, px, Ui.HoverLine);
        }

        if (stats.Surging)
        {
            bool on = stats.SurgingApplies;
            var centre = new Vector2(face.End.X - 12 * s, face.Position.Y + 12 * s);
            float r = BadgeRadius * s;
            DrawCircle(centre, r + Math.Max(1, s), Colors.Black);
            DrawCircle(centre, r, on ? Ui.HoverLine : BadgeOff);
            var ink = on ? BadgeInk : BadgeOffInk;
            DrawColoredPolygon([centre + new Vector2(0, -6) * s, centre + new Vector2(-6, 2) * s, centre + new Vector2(6, 2) * s], ink);
            DrawRect(new Rect2(centre + new Vector2(-2, 2) * s, new Vector2(4, 4) * s), ink);
        }

        int statsPx = Math.Max(6, (int)Math.Round(StatsText * s));
        float cell = StatsSpan * s / 4, left = (Width - StatsSpan) / 2 * s, rowTop = face.End.Y + 2 * s, rowHeight = (Height - FaceTop - Face - 2) * s;
        float baseline = rowTop + (rowHeight + font.GetAscent(statsPx) - font.GetDescent(statsPx)) / 2;
        var values = new (int Value, int Modifier, bool Own)[]
        {
            (stats.Attack, stats.AttackModifier, stats.Commander),
            (stats.Defense, stats.DefenseModifier, stats.Commander),
            (stats.Magic, 0, false),
            (stats.Attacks, 0, false),
        };
        for (int i = 0; i < values.Length; i++)
        {
            var (value, modifier, own) = values[i];
            var colour = own ? Own : modifier > 0 ? Up : modifier < 0 ? Down : Ui.Text;
            string text = value.ToString();
            float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, statsPx).X;
            var at = new Vector2(left + cell * (i + 0.5f) - width / 2, baseline);
            DrawString(font, at + new Vector2(1, 1) * Math.Max(1, s), text, HorizontalAlignment.Left, -1, statsPx, Shadow);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, statsPx, colour);
        }
    }
}
