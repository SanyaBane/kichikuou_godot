using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// A script menu of the battle as our own dialog on the battle screen (Option → Interface → New;
/// docs/ui/battle-screen.md, step 4; owner's mock-up B, 2026-10-13): the screen dimmed, a box centred over
/// the scene with our question (<see cref="BattleDialog"/>) over the script's items, styled as the cards'
/// context menus. A left release on an item chooses it; Esc, a right press or a left press off the box
/// cancels (no code, as a right click on the script's menu). Points are layout pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class BattleDialogLayer : Control
{
    private static readonly Rect2 SceneArea = new(0, 0, 1280, 410);
    private const float Width = 300, RowHeight = 30, Pad = 8, MarkWidth = 18, QuestionLine = 24, Rule = 10;
    private const float QuestionText = 18, ItemText = 17;
    private static readonly Color Dim = new(0, 0, 0, 0.35f);

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private float _scale = 1;
    private BattleDialog? _dialog;
    private IReadOnlyList<string> _labels = [];
    private int _hot = -1, _pressed = -1;

    public BattleDialogLayer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 11;
    }

    /// <summary>Whether a dialog is open.</summary>
    public bool Active => _dialog is not null;

    /// <summary>Whether the player has answered (<see cref="Result"/>: the code, or null for a cancel).</summary>
    public bool Done { get; private set; }

    /// <summary>The chosen code, or null when the player cancelled.</summary>
    public Sys3Int? Result { get; private set; }

    /// <summary>Opens <paramref name="dialog"/>; <paramref name="text"/> reads its items' labels.</summary>
    public void Begin(BattleDialog dialog, Func<ScriptRef, string> text)
    {
        _dialog = dialog;
        _labels = [.. dialog.Items.Select(i => text(i.Label).Trim())];
        (Done, Result, _hot, _pressed) = (false, null, -1, -1);
        QueueRedraw();
    }

    /// <summary>Closes the dialog.</summary>
    public void End()
    {
        _dialog = null;
        QueueRedraw();
    }

    public void Layout(float scale)
    {
        _scale = scale;
        QueueRedraw();
    }

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        QueueRedraw();
    }

    /// <summary>The box: centred over the scene.</summary>
    private Rect2 Box
    {
        get
        {
            float height = Pad * 2 + QuestionLine * 2 + Rule + (_dialog?.Items.Count ?? 0) * RowHeight;
            return new Rect2(SceneArea.GetCenter() - new Vector2(Width, height) / 2, new Vector2(Width, height));
        }
    }

    /// <summary>The top of the first item's row.</summary>
    private float ItemsTop => Box.Position.Y + Pad + QuestionLine * 2 + Rule;

    /// <summary>The item under <paramref name="p"/>, or -1.</summary>
    private int ItemAt(Vector2 p)
    {
        if (_dialog is null || !Box.HasPoint(p))
            return -1;
        int row = (int)Math.Floor((p.Y - ItemsTop) / RowHeight);
        return p.Y >= ItemsTop && row < _dialog.Items.Count ? row : -1;
    }

    /// <summary>The centre of item <paramref name="item"/> (layout pixels; unattended play).</summary>
    public Vector2 ItemCentre(int item) => new(Box.GetCenter().X, ItemsTop + (item + 0.5f) * RowHeight);

    // ------------------------------------------------------------------ input

    public void Move(Vector2 p)
    {
        int hot = ItemAt(p);
        if (hot == _hot)
            return;
        _hot = hot;
        QueueRedraw();
    }

    public void Press(bool right, Vector2 p)
    {
        if (_dialog is null || Done)
            return;
        if (right || !Box.HasPoint(p))
        {
            Cancel();
            return;
        }
        _pressed = ItemAt(p);
    }

    public void Release(bool right, Vector2 p)
    {
        if (_dialog is null || Done || right)
            return;
        if (_pressed >= 0 && ItemAt(p) == _pressed)
        {
            Result = _dialog.Items[_pressed].Code;
            Done = true;
        }
        _pressed = -1;
    }

    /// <summary>Esc: cancels the open dialog (true), else does nothing (false).</summary>
    public bool Escape()
    {
        if (_dialog is null || Done)
            return false;
        Cancel();
        return true;
    }

    private void Cancel()
    {
        Result = null;
        Done = true;
    }

    // ------------------------------------------------------------------ drawing

    public override void _Draw()
    {
        if (_dialog is null)
            return;
        float s = _scale;
        DrawRect(new Rect2(Vector2.Zero, new Vector2(1280, 800) * s), Dim);
        var box = new Rect2(Box.Position * s, Box.Size * s);
        DrawRect(new Rect2(box.Position + new Vector2(4, 5) * s, box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, _colours.Card);
        DrawRect(box, _colours.ButtonBorder, filled: false, width: Math.Max(1, 2 * s));
        if (GetThemeDefaultFont() is not { } font)
            return;
        int question = (int)Math.Round(QuestionText * s), item = (int)Math.Round(ItemText * s);
        float left = box.Position.X + (Pad + 4) * s, y = box.Position.Y + Pad * s;
        DrawString(font, new Vector2(left, y + (QuestionLine * s + font.GetAscent(question) - font.GetDescent(question)) / 2), _dialog.Question,
            HorizontalAlignment.Left, -1, question, _colours.Text);
        y += QuestionLine * s;
        DrawString(font, new Vector2(left, y + (QuestionLine * s + font.GetAscent(question) - font.GetDescent(question)) / 2), _dialog.Note,
            HorizontalAlignment.Left, -1, question, _colours.Muted);
        y += QuestionLine * s + Rule / 2 * s;
        DrawLine(new Vector2(box.Position.X + Pad * s, y), new Vector2(box.End.X - Pad * s, y), _colours.ButtonBorder, Math.Max(1, s));
        for (int i = 0; i < _dialog.Items.Count; i++)
        {
            var row = new Rect2(box.Position.X + (Pad - 4) * s, ItemsTop * s + i * RowHeight * s, box.Size.X - (Pad - 4) * 2 * s, RowHeight * s);
            if (i == _hot)
                DrawRect(row, _colours.ButtonHover);
            float baseline = row.Position.Y + (row.Size.Y + font.GetAscent(item) - font.GetDescent(item)) / 2;
            DrawString(font, new Vector2(row.Position.X + (4 + MarkWidth) * s, baseline), _labels[i], HorizontalAlignment.Left, -1, item,
                _dialog.Items[i].Warning ? _colours.Warning : _colours.Text);
        }
    }
}
