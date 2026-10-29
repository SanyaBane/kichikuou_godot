using Godot;
using Kichikuou.Content;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game;

// The import screen on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23: LI, LIp, LIr, the
// owner's choice as drawn): our paper sheet in the frame at the phone's type, the sides empty — the steps in two columns,
// the first with the folder's field at the left, the check and Play at the right; Import red at the sheet's right foot
// (the right thumb), Back to the title at its left foot. Importing, the folder ticked at the left, the stage and its bar
// at the right; refused, GameFiles' message across the sheet, its hashes in two lines under it, the field at the left,
// the stamp at the right, Choose another folder at the right thumb with Back left of it. A folder is its name over its
// place, a path its folders with Android's storage named (PhoneImport.FolderLines, Readable). Drawn in the frame's layout
// pixels over the whole screen (PhoneUnits), the places PhoneImport's; held upright as PI, PIp, PIr (ImportScreen.Upright.cs).
public partial class ImportScreen
{
    private PhoneFrame? _phone;

    private PhoneUnits Phone => new(_phone!);

    /// <summary>
    /// On the phone held either way: the screen over its parent's whole area (the screen's room, the gallery's stage), drawn
    /// from its corner in the frame's layout pixels; false on any other device.
    /// </summary>
    private bool PlacePhone()
    {
        _phone = Device.Held(this);
        if (_phone is not { } phone)
            return false;
        Position = Vector2.Zero;
        var whole = GetParentAreaSize();
        if (Size != whole)
            Size = whole;                       // Resized lays it out again, the same
        _k = phone.Scale;
        _origin = Vector2.Zero;
        QueueRedraw();
        return true;
    }

    /// <summary>A length of the canvas's units in layout pixels.</summary>
    private float L(float canvas) => canvas / _k;

    private void DrawPhone(PhoneUnits u)
    {
        var f = u.Frame;
        if (f.IsUpright)
        {
            DrawUpright(u);
            return;
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawPanel(this, u.Rect(PhoneImport.Panel(f)), u.K);
        var paper = u.Rect(PhoneImport.Paper(f));
        DrawRect(paper, Ui.Paper);
        DrawRect(paper, Ui.PaperHead, false, Lacquer.Line(1, u.K));
        int size = SideCells.Fit(_brush, Heading, u.Size(PhoneImport.HeadingText), paper.Size.X - u.Px(PhoneImport.HeadingRoom));
        Middle(_brush, Heading, size, paper.Position.X, L(PhoneImport.HeadingMiddle(f)), Ui.HeadInk, paper.Size.X);
        Middle(_cinzel, GameFiles.EnglishTranslation10.Name, u.Size(PhoneImport.VersionText), paper.Position.X,
            L(PhoneImport.VersionMiddle(f)), Ui.InkQuiet, paper.Size.X);
        var (left, right, y) = PhoneImport.Rule(f);
        DrawLine(new Vector2(L(left), L(y)), new Vector2(L(right), L(y)), Ui.PaperRule, u.Px(PhoneImport.RuleWidth));
        switch (_phase)
        {
            case Phase.Choose:
                PhoneChoose(u);
                break;
            case Phase.Importing:
            case Phase.Done:
                PhoneImporting(u);
                break;
            case Phase.Refused:
                PhoneRefused(u);
                break;
        }
        if (BackLive)
            PhoneBack(u);
    }

    /// <summary>LI: the folder's step and its field at the left, the check and Play at the right, Import at the right thumb.</summary>
    private void PhoneChoose(PhoneUnits u)
    {
        var f = u.Frame;
        float top = L(PhoneImport.StepsTop(f));
        float y = PhoneStep(u, 0, 0, top, L(PhoneImport.Column(f, 0).Right), "Choose the game folder",
            "The folder of your copy, with system39.exe, KICHIKUSA.ALD and System39.ain in it.", StepState.Now);
        PhoneField(u, y);
        float right = L(PhoneImport.Column(f, 1).Right);
        y = PhoneStep(u, 1, 1, top, right, "The files are checked and the scenario read", "Once for this folder, a second or so.",
            StepState.Next);
        PhoneStep(u, 1, 2, y + u.Px(PhoneImport.PlayGap), right, "Play", null, StepState.Next);
        PhoneButton(u, "Import", PhoneImport.ImportText);
    }

    /// <summary>LIp: the folder ticked at the left, the stage and its bar at the right, Play under it (lit once read).</summary>
    private void PhoneImporting(PhoneUnits u)
    {
        var f = u.Frame;
        ImportProgress progress;
        lock (_lock)
            progress = _progress;
        bool done = _phase == Phase.Done;
        float top = L(PhoneImport.StepsTop(f));
        PhoneStep(u, 0, 0, top, L(PhoneImport.TickedRight(f, 0)), "Game folder", PhoneImport.Readable(_folder), StepState.Done);
        float x = L(PhoneImport.TextLeft(f, 1)), right = L(PhoneImport.Column(f, 1).Right);
        float y = PhoneStep(u, 1, 1, top, done ? L(PhoneImport.TickedRight(f, 1)) : right, done ? "The scenario is read" : progress.Stage,
            null, done ? StepState.Done : StepState.Now);
        ImportBar(u, x, y, right, done ? 1 : (float)Math.Clamp(progress.Fraction, 0, 1));
        PhoneStep(u, 1, 2, y + u.Px(PhoneImport.PlayUnderBar), right, "Play", null, done ? StepState.Now : StepState.Next);
        ProgressShot(progress, done);
    }

    /// <summary>The decompile's bar from (<paramref name="x"/>, <paramref name="y"/>) to <paramref name="right"/>, its share under its right end.</summary>
    private void ImportBar(PhoneUnits u, float x, float y, float right, float fraction)
    {
        var bar = new Rect2(x, y, right - x, u.Px(PhoneImport.BarHeight));
        DrawRect(bar, FieldFill);
        DrawRect(bar, Ui.InkGrey, false, u.Px(2));
        float inset = u.Px(3);
        DrawRect(new Rect2(bar.Position + new Vector2(inset, inset), new Vector2((bar.Size.X - 2 * inset) * fraction, bar.Size.Y - 2 * inset)),
            Ui.VermilionInk);
        int share = u.Size(PhoneImport.ShareText);
        string percent = $"{Math.Round(fraction * 100)}%";
        Middle(_gothic, percent, share, right - _gothic.GetStringSize(percent, fontSize: share).X, y + u.Px(PhoneImport.ShareDown), Ui.Ink);
    }

    /// <summary>LIr: the refusal across the sheet, its hashes in two lines, the field at the left and the stamp at the right.</summary>
    private void PhoneRefused(PhoneUnits u)
    {
        var f = u.Frame;
        float top = L(PhoneImport.StepsTop(f)), x = L(PhoneImport.TextLeft(f, 0)), right = L(PhoneImport.Column(f, 1).Right);
        float y = PhoneStep(u, 0, 0, top, right, "Game folder", PhoneImport.Readable(_refusal, _folder), StepState.Failed);
        y = HashLines(u, x, y, right);
        var field = PhoneField(u, y);
        Stamp(u, new Vector2(L(PhoneImport.StampX(f)), field.GetCenter().Y));
        PhoneButton(u, "Choose another folder", PhoneImport.ChooseText);
    }

    /// <summary>The refusal's hashes from <paramref name="y"/>, each line shrunk to the room; returns where the field starts.</summary>
    private float HashLines(PhoneUnits u, float x, float y, float right)
    {
        var hashes = PhoneImport.HashLines(_hashes);
        foreach (string line in hashes)
        {
            Middle(_gothic, line, SideCells.Fit(_gothic, line, u.Size(PhoneImport.HashText), right - x), x, y, Ui.InkGrey);
            y += u.Px(PhoneImport.HashLine);
        }
        if (hashes.Count > 0)
            y += u.Px(PhoneImport.HashGap);
        return y;
    }

    /// <summary>The stamp "NOT v1.0" round <paramref name="centre"/>.</summary>
    private void Stamp(PhoneUnits u, Vector2 centre)
    {
        DrawArc(centre, u.Px(PhoneImport.StampRadius), 0, Mathf.Tau, 96, Ui.Vermilion, u.Px(PhoneImport.StampLine), true);
        float r = u.Px(PhoneImport.StampRadius);
        Middle(_cinzelBold, "NOT", u.Size(PhoneImport.StampRadius * 0.46f), centre.X - r, centre.Y - u.Px(18), Ui.Vermilion, 2 * r);
        Middle(_cinzelBold, "v1.0", u.Size(PhoneImport.StampRadius * 0.3f), centre.X - r, centre.Y + u.Px(40), Ui.Vermilion, 2 * r);
    }

    /// <summary>
    /// A numbered step (一 二 三, <paramref name="numeral"/>) in column <paramref name="column"/>, its title's first line's
    /// middle at <paramref name="middle"/>, its title and its lines wrapped to <paramref name="right"/>; returns where the next
    /// part starts, under its last line (the mock-ups' <c>step_text</c>).
    /// </summary>
    private float PhoneStep(PhoneUnits u, int column, int numeral, float middle, float right, string title, string? body, StepState state)
    {
        var f = u.Frame;
        var colour = StepColour(state);
        var seal = new Vector2(L(MarkX(f, column)), middle);
        float radius = u.Px(PhoneImport.MarkRadius);
        bool sealed_ = state is StepState.Now or StepState.Failed;
        if (sealed_)
            DrawCircle(seal, radius, Ui.Vermilion);
        DrawArc(seal, radius - u.Px(1), 0, Mathf.Tau, 64, state == StepState.Next ? Ui.InkGrey : Ui.VermilionInk,
            u.Px(PhoneImport.MarkLine), true);
        int mark = u.Size(PhoneImport.MarkText);
        Middle(_gothic, "一二三"[numeral].ToString(), mark, seal.X - radius, seal.Y, sealed_ ? Ui.SealText : colour, 2 * radius);
        if (state == StepState.Done)
        {
            var tick = new Vector2(L(TickX(f, column)), middle);
            DrawPolyline([tick + new Vector2(-u.Px(26), 0), tick + new Vector2(-u.Px(8), u.Px(18)), tick + new Vector2(u.Px(24), -u.Px(20))],
                Good, u.Px(7), true);
        }
        float x = L(TextLeft(f, column)), y = middle;
        int titleSize = u.Size(PhoneImport.TitleText);
        foreach (string line in Lines(title, titleSize, right - x))
        {
            Middle(_gothic, line, titleSize, x, y, colour);
            y += u.Px(PhoneImport.TitleLine);
        }
        if (body is not null)
        {
            int bodySize = u.Size(PhoneImport.BodyText);
            var ink = state == StepState.Failed ? Ui.VermilionInk : Ui.InkQuiet;
            foreach (string line in Lines(body, bodySize, right - x))
            {
                Middle(_gothic, line, bodySize, x, y, ink);
                y += u.Px(PhoneImport.BodyLine);
            }
        }
        return y + u.Px(PhoneImport.StepGap);
    }

    /// <summary>The folder's field from <paramref name="top"/> in the left column: its icon, the folder's name over its place, "Choose…".</summary>
    private Rect2 PhoneField(PhoneUnits u, float top)
    {
        var box = u.Rect(FieldBox(u.Frame));
        box.Position = new Vector2(box.Position.X, top);
        _field = box;
        bool live = FieldLive;
        DrawRect(box, FieldFill);
        DrawRect(box, _hoverField ? Ui.Vermilion : Ui.InkGrey, false, u.Px(2));
        float mid = box.GetCenter().Y, ix = box.Position.X + u.Px(PhoneImport.FieldIconIn), line = u.Px(3);
        DrawRect(new Rect2(ix, mid - u.Px(16), u.Px(52), u.Px(38)), Ui.InkQuiet, false, line);
        DrawRect(new Rect2(ix, mid - u.Px(26), u.Px(22), u.Px(10)), Ui.InkQuiet, false, line);
        float x = box.Position.X + u.Px(PhoneImport.FieldTextIn), chooseRight = box.End.X - u.Px(PhoneImport.FieldChooseIn);
        int choose = u.Size(PhoneImport.FieldChoose);
        float room = (live ? chooseRight - _gothic.GetStringSize("Choose…", fontSize: choose).X - u.Px(20) : chooseRight) - x;
        if (_folder.Length > 0)
        {
            var (name, place) = PhoneImport.FolderLines(_folder);
            int nameSize = u.Size(PhoneImport.FieldName), placeSize = u.Size(PhoneImport.FieldPlace);
            bool two = place.Length > 0;
            Middle(_gothic, Ellipsis(name, room, nameSize), nameSize, x, two ? mid - u.Px(PhoneImport.FieldNameUp) : mid, Ui.Ink);
            if (two)
                Middle(_gothic, Ellipsis(place, room, placeSize), placeSize, x, mid + u.Px(PhoneImport.FieldPlaceDown), Ui.InkQuiet);
        }
        else
        {
            Middle(_gothic, "not chosen yet", choose, x, mid, Ui.InkGrey);
        }
        if (live)
            Middle(_gothic, "Choose…", choose, chooseRight - _gothic.GetStringSize("Choose…", fontSize: choose).X, mid,
                _hoverField ? Ui.Vermilion : Ui.VermilionInk);
        return box;
    }

    /// <summary>The red button at the right thumb (the brush face), greyed while it cannot act.</summary>
    private void PhoneButton(PhoneUnits u, string word, float text)
    {
        var r = Button;
        bool live = ButtonLive;
        if (live)
        {
            Ui.DrawRedButton(this, r, _hoverButton, _pressedButton, u.K);
        }
        else
        {
            DrawRect(r, Ui.OffButton);
            DrawRect(r, Ui.OffLine, false, u.Px(3));
        }
        int size = SideCells.Fit(_brush, word, u.Size(text), r.Size.X - u.Px(30));
        Middle(_brush, word, size, r.Position.X, r.GetCenter().Y, live ? Ui.Name : Ui.OffText, r.Size.X);
    }

    /// <summary>Back: its arrow and its word (Cinzel) at the box's left, level with the red button.</summary>
    private void PhoneBack(PhoneUnits u)
    {
        var r = BackBox;
        var colour = _hoverBack ? Ui.Vermilion : Ui.VermilionInk;
        float cy = r.GetCenter().Y, x = r.Position.X, half = u.Px(PhoneImport.ArrowHalf);
        DrawColoredPolygon([new(x + u.Px(PhoneImport.ArrowWidth), cy - half), new(x, cy), new(x + u.Px(PhoneImport.ArrowWidth), cy + half)],
            colour);
        Middle(_cinzelBold, PhoneImport.BackWord(_phase == Phase.Refused), u.Size(PhoneImport.BackText), x + u.Px(PhoneImport.BackTextIn),
            cy, colour);
    }

    /// <summary><paramref name="text"/> with its middle at <paramref name="middle"/> (the mock-ups' anchor "lm"), centred in <paramref name="width"/> when given.</summary>
    private void Middle(Font font, string text, int size, float x, float middle, Color colour, float width = -1) =>
        DrawString(font, new Vector2(x, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), text,
            width < 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center, width, size, colour);

    /// <summary><paramref name="text"/> wrapped at spaces into <paramref name="width"/> (the mock-ups' <c>wrap</c>).</summary>
    private List<string> Lines(string text, int size, float width)
    {
        var lines = new List<string>();
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string tried = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && _gothic.GetStringSize(tried, fontSize: size).X > width)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = tried;
            }
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }
}
