using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Battles;

/// <summary>
/// A scouted commander's card on the phone, as the enemy attack (docs/ui/phone.md, §14, LA2) draws it (<c>attacker_card</c>):
/// the place on a gold diamond and the name beside it, the troop type under
/// them, "TROOPS" and the figure over a rule at the foot, the face at the right. Drawn in a <see cref="PhoneUnits"/> drawing at
/// <see cref="PhoneBattleStates"/>' card figures.
/// </summary>
internal static class PhoneCommanderCard
{
    /// <summary>
    /// Draws <paramref name="commander"/> in place <paramref name="slot"/>'s <paramref name="card"/> (layout pixels), its face
    /// at most <paramref name="faceMax"/> reference pixels; <paramref name="font"/> the game's, <paramref name="cinzel"/> the
    /// captions'.
    /// </summary>
    public static void Draw(CanvasItem c, PhoneUnits u, UiPalette ui, Rect2 card, int slot, ScoutedCommander commander, Font font,
        Font cinzel, FacePictures faces, FaceCrops crops, float faceMax)
    {
        c.DrawRect(card, ui.Row);
        c.DrawRect(card, ui.DimGold, false, Lacquer.Line(1, 1));
        float face = Math.Min(card.Size.Y - 2 * u.Px(PhoneBattleStates.FaceIn), u.Px(faceMax));
        var faceArea = new Rect2(card.End.X - u.Px(PhoneBattleStates.FaceIn) - face, card.Position.Y + u.Px(PhoneBattleStates.FaceIn), face, face);
        c.DrawRect(faceArea, ui.FaceBack);
        if (commander.Picture != 0
            && faces.SquareTexture(commander.Picture, faces.CropOf(commander.Picture, crops), (int)Math.Round(face * u.K)) is { } square)
            c.DrawTextureRect(square, faceArea, false);
        c.DrawRect(faceArea, ui.DimGold, false, Lacquer.Line(1, 1));

        // The name runs to 12 short of the face and the troop type to 16 (the mock-up's), each shrunk to fit a narrow card.
        float x0 = card.Position.X, y0 = card.Position.Y, right = faceArea.Position.X - u.Px(30);
        ui.DrawBadge(c, cinzel, new Vector2(x0 + u.Px(40), y0 + u.Px(44)), u.Px(PhoneBattleStates.Badge), slot);
        float nameRoom = faceArea.Position.X - u.Px(12) - x0 - u.Px(80), kindRoom = faceArea.Position.X - u.Px(16) - x0 - u.Px(22);
        string type = commander.TroopType == "" ? "—" : commander.TroopType;
        int name = SideCells.Fit(font, commander.Name, u.Size(PhoneBattleStates.CardName), nameRoom);
        int kind = SideCells.Fit(font, type, u.Size(PhoneBattleStates.CardKind), kindRoom);
        c.DrawString(font, new Vector2(x0 + u.Px(80), y0 + u.Px(44) + (font.GetAscent(name) - font.GetDescent(name)) / 2),
            commander.Name, HorizontalAlignment.Left, nameRoom, name, ui.Name);
        c.DrawString(font, new Vector2(x0 + u.Px(22), y0 + u.Px(92) + font.GetAscent(kind)), type, HorizontalAlignment.Left, kindRoom,
            kind, ui.Quiet);
        float rule = card.End.Y - u.Px(22);
        c.DrawLine(new Vector2(x0 + u.Px(20), rule), new Vector2(right, rule), ui.DimGold, Lacquer.Line(1, 1));
        int label = u.Size(PhoneBattleStates.CardLabel), troops = u.Size(PhoneBattleStates.CardTroops);
        c.DrawString(cinzel, new Vector2(x0 + u.Px(22), card.End.Y - u.Px(30) - cinzel.GetDescent(label)), "TROOPS",
            fontSize: label, modulate: ui.Subtitle);
        string count = commander.Troops == 0 ? "none" : Figures.Number(commander.Troops);
        c.DrawString(font, new Vector2(right - font.GetStringSize(count, fontSize: troops).X, card.End.Y - u.Px(28) - font.GetDescent(troops)),
            count, fontSize: troops, modulate: ui.Value);
    }
}
