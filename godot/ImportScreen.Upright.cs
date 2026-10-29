using Godot;
using Kichikuou.Content;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game;

// The import screen on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22: PI, PIp, PIr, the
// owner's choice as drawn): the wave backdrop, our paper sheet as a card in the room's middle at held landscape's type, the
// steps 一 二 三 one under another — the folder's field under the first; importing, the folder ticked, the stage and its
// bar, Play under it; refused, GameFiles' message, its hashes, the field and the stamp under it — and Back | the red button
// in the bar at the thumb (Import, Choose another folder; greyed while it cannot act, Back left out at a first launch).
// Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places PhoneImportUpright's; a step, the
// field and the button as held landscape draws them (ImportScreen.Phone.cs).
public partial class ImportScreen
{
    // Where held landscape's parts stand held upright: one column, its seals, words and tick at PhoneImportUpright's places.
    private static float MarkX(PhoneFrame f, int column) => f.IsUpright ? PhoneImportUpright.MarkX(f) : PhoneImport.MarkX(f, column);

    private static float TickX(PhoneFrame f, int column) => f.IsUpright ? PhoneImportUpright.TickX(f) : PhoneImport.TickX(f, column);

    private static float TextLeft(PhoneFrame f, int column) => f.IsUpright ? PhoneImportUpright.TextLeft(f) : PhoneImport.TextLeft(f, column);

    private static PhoneFrame.Box FieldBox(PhoneFrame f) => f.IsUpright ? PhoneImportUpright.Field(f, 0) : PhoneImport.Field(f, 0);

    private void DrawUpright(PhoneUnits u)
    {
        var f = u.Frame;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawWaveBackdrop(this, u.K, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        Ui.DrawPanel(this, u.Rect(PhoneImportUpright.Panel(f)), u.K);
        var paper = u.Rect(PhoneImportUpright.Paper(f));
        DrawRect(paper, Ui.Paper);
        DrawRect(paper, Ui.PaperHead, false, Lacquer.Line(1, u.K));
        int size = SideCells.Fit(_brush, Heading, u.Size(PhoneImport.HeadingText), paper.Size.X - u.Px(PhoneImport.HeadingRoom));
        Middle(_brush, Heading, size, paper.Position.X, L(PhoneImportUpright.HeadingMiddle(f)), Ui.HeadInk, paper.Size.X);
        Middle(_cinzel, GameFiles.EnglishTranslation10.Name, u.Size(PhoneImport.VersionText), paper.Position.X,
            L(PhoneImportUpright.VersionMiddle(f)), Ui.InkQuiet, paper.Size.X);
        var (left, right, y) = PhoneImportUpright.Rule(f);
        DrawLine(new Vector2(L(left), L(y)), new Vector2(L(right), L(y)), Ui.PaperRule, u.Px(PhoneImport.RuleWidth));
        switch (_phase)
        {
            case Phase.Choose:
                UprightChoose(u);
                break;
            case Phase.Importing:
            case Phase.Done:
                UprightImporting(u);
                break;
            case Phase.Refused:
                UprightRefused(u);
                break;
        }
        if (BackLive)
            UprightBack(u);
    }

    /// <summary>PI: the folder's step and its field, the check and Play under it, Import in the bar.</summary>
    private void UprightChoose(PhoneUnits u)
    {
        var f = u.Frame;
        float right = L(PhoneImportUpright.Right(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportUpright.StepsTop(f)), right, "Choose the game folder",
            "The folder of your copy, with system39.exe, KICHIKUSA.ALD and System39.ain in it.", StepState.Now);
        var field = PhoneField(u, y);
        y = PhoneStep(u, 0, 1, field.End.Y + u.Px(PhoneImportUpright.FieldUnder), right, "The files are checked and the scenario read",
            "Once for this folder, a second or so.", StepState.Next);
        PhoneStep(u, 0, 2, y + u.Px(PhoneImportUpright.StepRoom), right, "Play", null, StepState.Next);
        PhoneButton(u, "Import", PhoneImportUpright.ButtonText);
    }

    /// <summary>PIp: the folder ticked, the stage and its bar under it, Play under the bar (lit once read).</summary>
    private void UprightImporting(PhoneUnits u)
    {
        var f = u.Frame;
        ImportProgress progress;
        lock (_lock)
            progress = _progress;
        bool done = _phase == Phase.Done;
        float right = L(PhoneImportUpright.Right(f)), ticked = L(PhoneImportUpright.TickedRight(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportUpright.StepsTop(f)), ticked, "Game folder", PhoneImport.Readable(_folder), StepState.Done);
        y = PhoneStep(u, 0, 1, y + u.Px(PhoneImportUpright.StepRoom), done ? ticked : right, done ? "The scenario is read" : progress.Stage,
            null, done ? StepState.Done : StepState.Now);
        ImportBar(u, L(PhoneImportUpright.TextLeft(f)), y, right, done ? 1 : (float)Math.Clamp(progress.Fraction, 0, 1));
        PhoneStep(u, 0, 2, y + u.Px(PhoneImportUpright.PlayUnderBar), right, "Play", null, done ? StepState.Now : StepState.Next);
        ProgressShot(progress, done);
    }

    /// <summary>PIr: the refusal, its hashes each on a line, the field and the stamp under it, Choose another folder in the bar.</summary>
    private void UprightRefused(PhoneUnits u)
    {
        var f = u.Frame;
        float x = L(PhoneImportUpright.TextLeft(f)), right = L(PhoneImportUpright.Right(f));
        float y = PhoneStep(u, 0, 0, L(PhoneImportUpright.StepsTop(f)), right, "Game folder", PhoneImport.Readable(_refusal, _folder),
            StepState.Failed);
        y = HashLines(u, x, y, right);
        var field = PhoneField(u, y);
        Stamp(u, new Vector2(L(PhoneImportUpright.StampX(f)), field.Position.Y + u.Px(PhoneImportUpright.StampDown)));
        PhoneButton(u, "Choose another folder", PhoneImportUpright.ButtonText);
    }

    /// <summary>Back in the bar (<c>back_and</c>): a panel, the gold triangle, its word in the brush face.</summary>
    private void UprightBack(PhoneUnits u)
    {
        var r = BackBox;
        Ui.DrawPanel(this, r, u.K, _hoverBack ? Ui.RowHover : null);
        float cy = r.GetCenter().Y, ax = r.Position.X + u.Px(62);
        DrawColoredPolygon([new(ax + u.Px(32), cy - u.Px(18)), new(ax, cy), new(ax + u.Px(32), cy + u.Px(18))], Ui.Gold);
        int size = SideCells.Fit(_brush, "Back", u.Size(PhoneImportUpright.BackText), r.End.X - ax - u.Px(70));
        float left = ax + u.Px(44);
        Middle(_brush, "Back", size, left, cy, _hoverBack ? Ui.Name : Ui.Text, r.End.X - left);
    }
}
