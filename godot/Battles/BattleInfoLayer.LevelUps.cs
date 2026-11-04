using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

// The results plate's level-up rows on every device (owner, 2026-11-04; docs/ui/messages.md, §7, A2 with L2): each row in
// two columns — the left one LEVEL UP small, spaced and muted over the name, the name wrapped at its spaces (never inside a
// word); the right one the gains whole, wrapped at their spaces, every line at the column's left. A row is as tall as its
// taller column, the plate as tall as its rows.
internal sealed partial class BattleInfoLayer
{
    /// <summary>The label over each name (L2: the word type's 3/4, its letters 0.14 of it apart, muted).</summary>
    private const string LevelLabel = "LEVEL UP";

    private const float LabelTracking = 0.14f, LabelPitch = 1.3f, NamePitch = 1.08f, GainsPitch = 1.3f;

    /// <summary>
    /// A device's level-up rows, in its layout pixels: the types (the label, the name, the gains), the row's room at its
    /// left, right, top and foot, the room between the columns and between the rows, the row's width; whether the names
    /// stand whole (the name column as wide as the widest name: held landscape, whose rows are wide) or wrap at their spaces
    /// (the column as wide as the label or the widest word of the names).
    /// </summary>
    private readonly record struct LevelMetrics(int Label, int Name, int Gains, float Left, float Right, float Pad, float Gap,
        float Between, float Width, bool WholeNames);

    /// <summary>A row laid out: the name's lines, the gains' lines, its height.</summary>
    private readonly record struct LevelRow(IReadOnlyList<string> Names, IReadOnlyList<string> Gains, float Height);

    /// <summary>The rows of <paramref name="ups"/>, the name column's width and the rows' whole height (the room between them in).</summary>
    private (IReadOnlyList<LevelRow> Rows, float Column, float Height) LevelRows(IReadOnlyList<BattleLevelUp> ups, LevelMetrics m)
    {
        if (ups.Count == 0)
            return ([], 0, 0);
        float column = SpacedWidth(LevelLabel, m.Label);
        foreach (var up in ups)
        {
            foreach (string word in m.WholeNames ? [up.Unit] : up.Unit.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                column = Math.Max(column, _semi.GetStringSize(word, fontSize: m.Name).X);
        }
        float room = m.Width - m.Left - column - m.Gap - m.Right;
        var rows = new List<LevelRow>();
        foreach (var up in ups)
        {
            var names = Wrap(_semi, [.. up.Unit.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => (w, " "))], m.Name, column);
            var gains = Wrap(_cinzel, GainWords(up.Gains), m.Gains, room);
            float left = m.Label * LabelPitch + names.Count * m.Name * NamePitch, right = gains.Count * m.Gains * GainsPitch;
            rows.Add(new LevelRow(names, gains, 2 * m.Pad + Math.Max(left, right)));
        }
        return (rows, column, rows.Sum(r => r.Height) + (rows.Count - 1) * m.Between);
    }

    /// <summary>The gains as words, each with what joins it to the word before: the separator kept with the word after it.</summary>
    private static List<(string Word, string Glue)> GainWords(IReadOnlyList<string> gains)
    {
        var words = new List<(string, string)>();
        for (int i = 0; i < gains.Count; i++)
        {
            var parts = gains[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < parts.Length; j++)
                words.Add(i > 0 && j == 0 ? ("·  " + parts[j], "  ") : (parts[j], " "));
        }
        return words;
    }

    /// <summary>Words into lines no wider than <paramref name="width"/>, broken only between words (a wider word alone on its line).</summary>
    private static List<string> Wrap(Font font, IReadOnlyList<(string Word, string Glue)> words, int size, float width)
    {
        var lines = new List<string>();
        string line = "";
        foreach (var (word, glue) in words)
        {
            string trial = line.Length == 0 ? word : line + glue + word;
            if (line.Length > 0 && font.GetStringSize(trial, fontSize: size).X > width)
            {
                lines.Add(line);
                line = word;
            }
            else
                line = trial;
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }

    /// <summary>The label's width at <paramref name="size"/>, its letters spaced.</summary>
    private float SpacedWidth(string text, int size) =>
        text.Sum(c => _semi.GetStringSize(c.ToString(), fontSize: size).X) + LabelTracking * size * (text.Length - 1);

    /// <summary>The rows from <paramref name="top"/>, their left at <paramref name="x"/> (layout pixels).</summary>
    private void DrawLevelRows(float x, float top, IReadOnlyList<LevelRow> rows, float column, LevelMetrics m)
    {
        float gx = x + m.Left + column + m.Gap;
        foreach (var row in rows)
        {
            Ui.DrawRowBox(this, new Rect2(x, top, m.Width, row.Height), false, _k);
            float inner = row.Height - 2 * m.Pad;
            float left = m.Label * LabelPitch + row.Names.Count * m.Name * NamePitch, right = row.Gains.Count * m.Gains * GainsPitch;
            float lt = top + m.Pad + (inner - left) / 2;
            DrawSpaced(x + m.Left, Lacquer.Baseline(new Rect2(x, lt, m.Width, m.Label * LabelPitch), _semi, m.Label), LevelLabel, m.Label);
            lt += m.Label * LabelPitch;
            foreach (string name in row.Names)
            {
                float pitch = m.Name * NamePitch;
                DrawString(_semi, new Vector2(x + m.Left, Lacquer.Baseline(new Rect2(x, lt, m.Width, pitch), _semi, m.Name)), name,
                    fontSize: m.Name, modulate: Ui.Name);
                lt += pitch;
            }
            float gt = top + m.Pad + (inner - right) / 2;
            foreach (string line in row.Gains)
            {
                float pitch = m.Gains * GainsPitch;
                DrawString(_cinzel, new Vector2(gx, Lacquer.Baseline(new Rect2(x, gt, m.Width, pitch), _cinzel, m.Gains)), line,
                    fontSize: m.Gains, modulate: Ui.Value);
                gt += pitch;
            }
            top += row.Height + m.Between;
        }
    }

    /// <summary>The label at <paramref name="baseline"/>, its letters spaced, muted.</summary>
    private void DrawSpaced(float x, float baseline, string text, int size)
    {
        foreach (char c in text)
        {
            string s = c.ToString();
            DrawString(_semi, new Vector2(x, baseline), s, fontSize: size, modulate: Ui.Quiet);
            x += _semi.GetStringSize(s, fontSize: size).X + LabelTracking * size;
        }
    }
}
