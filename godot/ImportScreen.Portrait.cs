using Godot;
using Kichikuou.Content;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game;

// The import screen on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22: PI, PIp, PIr, the
// owner's choice as drawn): the wave backdrop, our paper sheet as a card in the room's middle at held landscape's type, the
// steps 一 二 三 one under another — the folder's field under the first; importing, the folder ticked, the stage and its
// bar, Play under it; refused, GameFiles' message, its hashes, the field and the stamp under it — and the red button | Back
// in the bar, Back at the thumb (Import, Choose another folder; greyed while it cannot act; at a first launch Back left
// out and the red button alone at the thumb).
// Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places PhoneImportPortrait's; a step, the
// field and the button as held landscape draws them (ImportScreen.Phone.cs).
public partial class ImportScreen
{
    // Where held landscape's parts stand held upright: one column, its seals, words and tick at PhoneImportPortrait's places.
    private static float MarkX(PhoneFrame f, int column) => f.IsPortrait ? PhoneImportPortrait.MarkX(f) : PhoneImport.MarkX(f, column);

    private static float TickX(PhoneFrame f, int column) => f.IsPortrait ? PhoneImportPortrait.TickX(f) : PhoneImport.TickX(f, column);

    private static float TextLeft(PhoneFrame f, int column) => f.IsPortrait ? PhoneImportPortrait.TextLeft(f) : PhoneImport.TextLeft(f, column);

    private static PhoneFrame.Box FieldBox(PhoneFrame f) => f.IsPortrait ? PhoneImportPortrait.Field(f, 0) : PhoneImport.Field(f, 0);

    private void DrawPortrait(PhoneUnits u)
    {
        var f = u.Frame;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawWaveBackdrop(this, u.K, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        Ui.DrawPanel(this, u.Rect(PhoneImportPortrait.Panel(f)), u.K);
        var paper = u.Rect(PhoneImportPortrait.Paper(f));
        DrawRect(paper, Ui.Paper);
        DrawRect(paper, Ui.PaperHead, false, Lacquer.Line(1, u.K));
        int size = SideCells.Fit(_brush, Heading, u.Size(PhoneImport.HeadingText), paper.Size.X - u.Px(PhoneImport.HeadingRoom));
        Middle(_brush, Heading, size, paper.Position.X, L(PhoneImportPortrait.HeadingMiddle(f)), Ui.HeadInk, paper.Size.X);
        Middle(_cinzel, GameFiles.EnglishTranslation10.Name, u.Size(PhoneImport.VersionText), paper.Position.X,
            L(PhoneImportPortrait.VersionMiddle(f)), Ui.InkQuiet, paper.Size.X);
        var (left, right, y) = PhoneImportPortrait.Rule(f);
        DrawLine(new Vector2(L(left), L(y)), new Vector2(L(right), L(y)), Ui.PaperRule, u.Px(PhoneImport.RuleWidth));
        switch (_phase)
        {
            case Phase.Choose:
                PortraitChoose(u);
                break;
            case Phase.Importing:
            case Phase.Done:
                PortraitImporting(u);
                break;
            case Phase.Refused:
                PortraitRefused(u);
                break;
        }
        if (BackLive)
            PortraitBack(u);
    }

    /// <summary>PI: the folder's step and its field, the check and Play under it, Import in the bar.</summary>
    private void PortraitChoose(PhoneUnits u)
    {
        var f = u.Frame;
        float right = L(PhoneImportPortrait.Right(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportPortrait.StepsTop(f)), right, "Choose the game folder",
            "The folder of your copy, with system39.exe, KICHIKUSA.ALD and System39.ain in it.", StepState.Now);
        var field = PhoneField(u, y);
        y = PhoneStep(u, 0, 1, field.End.Y + u.Px(PhoneImportPortrait.FieldUnder), right, "The files are checked and the scenario read",
            "Once for this folder, a second or so.", StepState.Next);
        PhoneStep(u, 0, 2, y + u.Px(PhoneImportPortrait.StepRoom), right, "Play", null, StepState.Next);
        PhoneButton(u, "Import");
    }

    /// <summary>PIp: the folder ticked, the stage and its bar under it, Play under the bar (lit once read).</summary>
    private void PortraitImporting(PhoneUnits u)
    {
        var f = u.Frame;
        ImportProgress progress;
        lock (_lock)
            progress = _progress;
        bool done = _phase == Phase.Done;
        float right = L(PhoneImportPortrait.Right(f)), ticked = L(PhoneImportPortrait.TickedRight(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportPortrait.StepsTop(f)), ticked, "Game folder", PhoneImport.Readable(_folder), StepState.Done);
        y = PhoneStep(u, 0, 1, y + u.Px(PhoneImportPortrait.StepRoom), done ? ticked : right, done ? "The scenario is read" : progress.Stage,
            null, done ? StepState.Done : StepState.Now);
        ImportBar(u, L(PhoneImportPortrait.TextLeft(f)), y, right, done ? 1 : (float)Math.Clamp(progress.Fraction, 0, 1));
        PhoneStep(u, 0, 2, y + u.Px(PhoneImportPortrait.PlayUnderBar), right, "Play", null, done ? StepState.Now : StepState.Next);
        ProgressShot(progress, done);
    }

    /// <summary>PIr: the refusal, its hashes each on a line, the field and the stamp under it, Choose another folder in the bar.</summary>
    private void PortraitRefused(PhoneUnits u)
    {
        var f = u.Frame;
        float x = L(PhoneImportPortrait.TextLeft(f)), right = L(PhoneImportPortrait.Right(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportPortrait.StepsTop(f)), right, "Game folder", PhoneImport.Readable(_refusal, _folder),
            StepState.Failed);
        y = HashLines(u, x, y, right);
        var field = PhoneField(u, y);
        Stamp(u, new Vector2(L(PhoneImportPortrait.StampX(f)), field.Position.Y + u.Px(PhoneImportPortrait.StampDown)));
        PhoneButton(u, "Choose another folder");
    }

    /// <summary>Back in the bar (<c>back_and</c>): a panel, the gold triangle, its word at the red button's size (Cinzel 700).</summary>
    private void PortraitBack(PhoneUnits u)
    {
        var r = BackBox;
        Ui.DrawPanel(this, r, u.K, _hoverBack ? Ui.RowHover : null);
        float cy = r.GetCenter().Y, ax = r.Position.X + u.Px(62);
        DrawColoredPolygon([new(ax + u.Px(32), cy - u.Px(18)), new(ax, cy), new(ax + u.Px(32), cy + u.Px(18))], Ui.Gold);
        float left = ax + u.Px(44);
        Middle(SideCells.Cinzel, "Back", ButtonSize(u), left, cy, _hoverBack ? Ui.Name : Ui.Text, r.End.X - left);
    }
}
