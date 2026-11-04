using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The strategy's cut-in on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11, PB4c): stacked
// round the room's middle over the whole screen — the announcement and the title centred on top, the band across the
// screen with the strategist's portrait at his side and what the strategy does beside it (4 mm), his line in our balloon
// under it (2.6 mm), its tail up at him; the backdrop stays at the picture's place under it. Drawn in the frame's layout
// pixels (PhoneUnits), the places PhoneBattleStatesUpright's.
internal sealed partial class StrategyCutInView
{
    private PhoneFrame? _upright;

    /// <summary>The band's far side, held upright, keeps this much of its strength: the band reads across the screen (PB4c).</summary>
    private const float FarBand = 0.45f;

    private void DrawUpright(BattleStrategy strategy, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        float k = 1 / u.K;                                              // the canvas's units to layout pixels
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        float t = Math.Clamp((float)(_appearing / AppearSeconds), 0, 1);
        float a = 1 - (1 - t) * (1 - t) * (1 - t);                      // eased out
        bool player = strategy.Side == BattleSide.Player;
        float outward = player ? -1 : 1;
        var side = player ? PlayerColour : EnemyColour;

        // The band across the whole screen, strong at the strategist's side, its edges in the accent colour.
        var (topLeft, topRight, footLeft, footRight) = PhoneBattleStatesUpright.CutInBand(f);
        float right = f.Screen.W * k;
        Vector2[] band = [new(0, topLeft * k), new(right, topRight * k), new(right, footRight * k), new(0, footLeft * k)];
        Color strong = side with { A = BandAlpha * a }, far = side with { A = BandAlpha * FarBand * a };
        _sheet.DrawPolygon(band, player ? [strong, far, far, strong] : [far, strong, strong, far]);
        var edge = Ui.HoverLine with { A = a };
        _sheet.DrawLine(band[0], band[1], edge, u.Px(EdgeWidth), true);
        _sheet.DrawLine(band[3], band[2], edge, u.Px(EdgeWidth), true);

        // The portrait slides in from its side.
        var area = u.Rect(PhoneBattleStatesUpright.Strategist(f, player));
        area.Position += new Vector2(outward * u.Px(PhoneBattleStatesUpright.SlideIn) * (1 - a), 0);
        if (_portrait is not null)
        {
            var shadow = new StyleBoxFlat
            {
                BgColor = Colors.Transparent, ShadowColor = PortraitShadow, ShadowSize = (int)Math.Round(u.Px(16)),
                ShadowOffset = new Vector2(0, u.Px(6)),
            };
            _sheet.DrawStyleBox(shadow, area);
            _sheet.DrawTextureRect(_portrait, area, false, new Color(1, 1, 1, a));
            _sheet.DrawRect(area, Ui.DimGold with { A = a }, false, Lacquer.Line(1, 1));
        }

        // The announcement over the title, centred on the room, sliding in from the portrait's side.
        float cx = (f.Room.X + f.Room.W / 2) * k + outward * u.Px(PhoneBattleStatesUpright.TitleSlide) * (1 - a);
        float middle = PhoneBattleStatesUpright.CutInMiddle(f) * k;
        if (strategy.Announcement != "")
            Centred(_caption, new Vector2(cx, middle + u.Px(PhoneBattleStatesUpright.AnnouncementMiddle)), strategy.Announcement.ToUpperInvariant(),
                u.Size(PhoneBattleStatesUpright.AnnouncementText), Ui.HoverLine with { A = a }, Ink with { A = a }, u.Size(5));
        if (strategy.Result != 0)
            Centred(_title, new Vector2(cx, middle + u.Px(PhoneBattleStatesUpright.CutInTitleMiddle)), strategy.Title,
                u.Size(PhoneBattleStatesUpright.CutInTitleText), (strategy.Great ? Ui.HoverLine : Colors.White) with { A = a }, Ink with { A = a },
                u.Size(10));

        // What it does, on the band beside the portrait.
        if (strategy.Label != "")
        {
            var (lx, ly) = PhoneBattleStatesUpright.CutInLabel(f, player);
            int size = u.Size(strategy.Great ? PhoneBattleStatesUpright.GreatLabelText : PhoneBattleStatesUpright.LabelText);
            float room = PhoneBattleStatesUpright.CutInLabelWidth(f) * k, wide = _title.GetStringSize(strategy.Label, HorizontalAlignment.Left, -1, size).X;
            if (wide > room)                                            // set smaller to fit beside the portrait
                size = Math.Max(1, (int)(size * room / wide));
            Centred(_title, new Vector2(lx * k, ly * k), strategy.Label, size, Ui.HoverLine, Ink, u.Size(7));
        }

        // The line in our balloon under the band, its tail up at the portrait.
        if (strategy.Line is { } line)
        {
            int size = u.Size(PhoneBattleStatesUpright.CutInLine);
            _bold.SetSpacing(TextServer.SpacingType.Bottom, u.Size(PhoneBattleStatesUpright.CutInGap));
            float pad = u.Px(PhoneBattleStatesUpright.TextSide);
            float textWidth = (f.Room.W - f.Px(2 * PhoneBattleStatesUpright.LineEdge)) * k - 2 * pad;
            var text = _bold.GetMultilineStringSize(line.Text, HorizontalAlignment.Left, textWidth, size, -1, Breaks);
            var box = u.Rect(PhoneBattleStatesUpright.CutInBalloon(f, text.Y * u.K));
            float s = u.Px(PhoneBattleStatesUpright.TailDepth) / 18;   // the PC's balloon at the tail's depth
            var portrait = u.Rect(PhoneBattleStatesUpright.Strategist(f, player));
            var ink = SpeechBalloon.DrawBalloonTailUp(_sheet, box, portrait.GetCenter().X, s, line.Thought);
            var origin = box.Position + new Vector2(pad, u.Px(PhoneBattleStatesUpright.TextTop) - 4 * s + _bold.GetAscent(size));
            _sheet.DrawMultilineString(_bold, origin, line.Text, HorizontalAlignment.Left, textWidth, size, -1, SpeechBalloon.InkOf(line, ink), Breaks);
            float bob = BobPixels * s * Mathf.Sin((float)(UiClock.Msec / 1000.0 * Mathf.Tau / BobSeconds));
            var tip = new Vector2(box.End.X - 26 * s, box.End.Y - 17 * s + bob);
            _sheet.DrawColoredPolygon([tip + new Vector2(-MarkerSize, -MarkerSize * 1.1f) * s, tip + new Vector2(MarkerSize, -MarkerSize * 1.1f) * s, tip],
                Ui.HoverLine);
        }
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Text with a dark outline centred on <paramref name="at"/> both ways.</summary>
    private void Centred(Font font, Vector2 at, string text, int size, Color colour, Color ink, int outline)
    {
        float w = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        Outlined(font, new Vector2(at.X - w / 2, at.Y + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, size, colour, ink, outline,
            HorizontalAlignment.Left);
    }
}
