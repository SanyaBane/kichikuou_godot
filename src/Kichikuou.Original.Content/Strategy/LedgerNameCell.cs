using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The deployment table's Name · Type cell off the upright phone (owner, 2026-11-04: "the same design" as upright, the
/// mock-ups of <c>tools/ui/mock_name_cell.py</c>): one cell in place of the Name, Type and Rows columns — the unit's face
/// from the name's top to the HP bar's foot, the name, the type and the rows under it from one left edge, the troops' and
/// the HP bars under them. Its places are the upright row's (<see cref="PhoneLedgerUpright.DeploymentRows"/>) scaled so
/// the ledger's text (<paramref name="Text"/>) stands for upright's (<see cref="PhoneLedgerUpright.Text"/>): every value
/// in the window's pixels, from the row's top or the cell's left.
/// </summary>
public sealed record LedgerNameCell(float Text, float Row, float NameText, float NameMiddle, float TypeText, float TypeMiddle,
    float RowsMiddle, float FaceTop, float Face, float FaceGap, float BarTop, float HpTop, float BarHeight, float Right)
{
    /// <summary>The cell's heading.</summary>
    public const string Header = "Name · Type";

    /// <summary>The columns the cell takes the place of (Name stays, drawn as the cell).</summary>
    public static IReadOnlyList<DeploymentColumn> Merged { get; } = [DeploymentColumn.TroopType, DeploymentColumn.Rows];

    /// <summary>The cell's places for a ledger whose text is <paramref name="text"/> pixels.</summary>
    public static LedgerNameCell For(float text)
    {
        var r = PhoneLedgerUpright.DeploymentRows;
        float s = text / PhoneLedgerUpright.Text;
        return new(text, r.Row * s, r.NameText * s, r.NameMiddle * s, PhoneLedgerUpright.TypeText * s, r.TypeMiddle * s,
            r.RowsMiddle * s, r.FaceTop * s, r.Face * s, PhoneLedgerUpright.NameGap * s, r.BarTop * s, r.HpTop * s,
            PhoneLedgerUpright.BarHeight * s, PhoneLedgerUpright.NameRight * s);
    }

    /// <summary>The text's left from the cell's: the face (2 in) and the room after it.</summary>
    public float TextX => 2 + Face + FaceGap;

    /// <summary>The cell's width for the widest of the text's lines (<paramref name="widest"/>).</summary>
    public float Width(float widest) => TextX + widest + Right;

    /// <summary>
    /// What a tab shows with the cell: its columns but the type and the rows, Name headed "Name · Type", the groups over
    /// them indexed again.
    /// </summary>
    public static LedgerTabColumns Merge(LedgerTabColumns shown)
    {
        var columns = shown.Columns.Where(c => !Merged.Contains(c.Column))
            .Select(c => c.Column == DeploymentColumn.Name ? c with { Header = Header } : c).ToList();
        var groups = new List<LedgerGroup>();
        foreach (var group in shown.Groups)
        {
            int first = columns.FindIndex(c => c.Column == shown.Columns[group.First].Column);
            int last = columns.FindIndex(c => c.Column == shown.Columns[group.Last].Column);
            if (first >= 0 && last >= first)
                groups.Add(group with { First = first, Last = last });
        }
        return new LedgerTabColumns(columns, groups);
    }
}
