using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

/// <summary>
/// A script menu of the battle as our own dialog on the battle screen (our interface;
/// docs/ui/battle-screen.md, step 4; owner's mock-up B, 2026-10-13): the screen dimmed, a box centred over
/// the scene with our question (<see cref="BattleDialog"/>) over the script's items, styled as the cards'
/// context menus. A left release on an item chooses it; Esc, a right press or a left press off the box
/// cancels (no code, as a right click on the script's menu). Points are layout pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class BattleDialogLayer : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    private static readonly Rect2 SceneArea = new(0, 0, 1280, 410);
    private const float Width = 300, RowHeight = 30, Pad = 8, MarkWidth = 18, QuestionLine = 24, Rule = 10;
    private const float QuestionText = 18, ItemText = 17;
    private static readonly Color Dim = new(0, 0, 0, 0.35f);

    // The touch mode's dialog (owner's A2, 2026-10-25; docs/ui/touch.md, "The battle within the thumbs' reach"): beside the
    // left rail's top, where its Retreat stands, the items side by side as buttons a thumb's size; a warning item
    // (Retreat) farthest from the rail, so a second tap on the rail's button does not confirm it.
    private static readonly Vector2 TouchCorner = new(146, 470);
    private const float TouchWidth = 330, TouchButton = 80, TouchGap = 12, TouchPad = 14, TouchItemText = 19;
    private static bool Touch => Device.TouchLayout;                // the tablet's layout, read live (docs/ui/phone-build.md, §3)
    private readonly FontFile _brush = Lacquer.Brush();

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

    /// <summary>The layout: <paramref name="phone"/> the phone's frame held either way, upright the board <paramref name="height"/> layout pixels high.</summary>
    public void Layout(float scale, PhoneFrame? phone = null, float height = 800)
    {
        _scale = scale;
        _phone = phone;
        _height = height;
        QueueRedraw();
    }

    /// <summary>The box: centred over the scene; in the touch mode beside the left rail's top; on the phone beside Retreat.</summary>
    private Rect2 Box
    {
        get
        {
            if (_phone is { } f)
                return f.IsPortrait ? B(PortraitQuestion()) : L(PhoneQuestion(f));
            if (Touch)
                return new Rect2(TouchCorner, new Vector2(TouchWidth, TouchPad * 2 + QuestionLine * 2 + Rule + TouchGap + TouchButton));
            float height = Pad * 2 + QuestionLine * 2 + Rule + (_dialog?.Items.Count ?? 0) * RowHeight;
            return new Rect2(SceneArea.GetCenter() - new Vector2(Width, height) / 2, new Vector2(Width, height));
        }
    }

    /// <summary>The top of the first item's row.</summary>
    private float ItemsTop => Box.Position.Y + (Touch ? TouchPad + TouchGap : Pad) + QuestionLine * 2 + Rule;

    /// <summary>Item <paramref name="item"/>'s place: a row, or in the touch mode a button in the row of them.</summary>
    private Rect2 ItemRect(int item)
    {
        if (_phone is { } f)
            return f.IsPortrait
                ? B(PhoneBattlePortrait.RetreatAnswers(PortraitQuestion(), _dialog?.Items.Count ?? 1)[TouchColumn(item)])
                : L(PhoneBattle.RetreatAnswers(f, PhoneQuestion(f), _dialog?.Items.Count ?? 1)[TouchColumn(item)]);
        var box = Box;
        if (!Touch)
            return new Rect2(box.Position.X + Pad - 4, ItemsTop + item * RowHeight, box.Size.X - (Pad - 4) * 2, RowHeight);
        int count = _dialog?.Items.Count ?? 0;
        float w = (box.Size.X - TouchPad * 2 - TouchGap * (count - 1)) / Math.Max(1, count);
        return new Rect2(box.Position.X + TouchPad + TouchColumn(item) * (w + TouchGap), ItemsTop, w, TouchButton);
    }

    /// <summary>Item <paramref name="item"/>'s column in the touch mode's row: the warning items last, farthest from the rail.</summary>
    private int TouchColumn(int item)
    {
        var order = _dialog!.Items.Select((it, i) => (it.Warning, i)).OrderBy(x => x.Warning).Select(x => x.i).ToList();
        return order.IndexOf(item);
    }

    /// <summary>The item under <paramref name="p"/>, or -1.</summary>
    private int ItemAt(Vector2 p)
    {
        if (_dialog is null || !Box.HasPoint(p))
            return -1;
        for (int i = 0; i < _dialog.Items.Count; i++)
        {
            if (ItemRect(i).HasPoint(p))
                return i;
        }
        return -1;
    }

    /// <summary>The centre of item <paramref name="item"/> (layout pixels; unattended play).</summary>
    public Vector2 ItemCentre(int item) => ItemRect(item).GetCenter();

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
        if (_phone is { } phone)
        {
            DrawPhone(phone);
            return;
        }
        DrawRect(new Rect2(Vector2.Zero, new Vector2(1280, 800) * s), Dim);
        if (Touch)
        {
            DrawTouch();
            return;
        }
        var box = new Rect2(Box.Position * s, Box.Size * s);
        DrawRect(new Rect2(box.Position + new Vector2(4, 5) * s, box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.Gold, filled: false, width: Math.Max(1, 2 * s));
        if (GetThemeDefaultFont() is not { } font)
            return;
        int question = (int)Math.Round(QuestionText * s), item = (int)Math.Round(ItemText * s);
        float left = box.Position.X + (Pad + 4) * s, y = box.Position.Y + Pad * s;
        DrawString(font, new Vector2(left, y + (QuestionLine * s + font.GetAscent(question) - font.GetDescent(question)) / 2), _dialog.Question,
            HorizontalAlignment.Left, -1, question, Ui.Text);
        y += QuestionLine * s;
        DrawString(font, new Vector2(left, y + (QuestionLine * s + font.GetAscent(question) - font.GetDescent(question)) / 2), _dialog.Note,
            HorizontalAlignment.Left, -1, question, Ui.Quiet);
        y += QuestionLine * s + Rule / 2 * s;
        DrawLine(new Vector2(box.Position.X + Pad * s, y), new Vector2(box.End.X - Pad * s, y), Ui.Gold, Math.Max(1, s));
        for (int i = 0; i < _dialog.Items.Count; i++)
        {
            var row = new Rect2(box.Position.X + (Pad - 4) * s, ItemsTop * s + i * RowHeight * s, box.Size.X - (Pad - 4) * 2 * s, RowHeight * s);
            if (i == _hot)
                DrawRect(row, Ui.RowHover);
            float baseline = row.Position.Y + (row.Size.Y + font.GetAscent(item) - font.GetDescent(item)) / 2;
            DrawString(font, new Vector2(row.Position.X + (4 + MarkWidth) * s, baseline), _labels[i], HorizontalAlignment.Left, -1, item,
                _dialog.Items[i].Warning ? Ui.Minus : Ui.Text);
        }
    }

    /// <summary>The touch mode's box in the rails' lacquer: the question, a rule, the items as buttons (a warning one as Turn END).</summary>
    private void DrawTouch()
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        var box = Box;
        DrawRect(new Rect2(box.Position + new Vector2(4, 5), box.Size), new Color(0, 0, 0, 0.3f));
        Ui.DrawPanel(this, box, s);
        if (GetThemeDefaultFont() is { } font)
        {
            float left = box.Position.X + TouchPad + 4, y = box.Position.Y + TouchPad;
            foreach (var (line, colour) in new[] { (_dialog!.Question, Ui.Text), (_dialog.Note, Ui.Quiet) })
            {
                DrawString(font, new Vector2(left, Lacquer.Baseline(new Rect2(left, y, 0, QuestionLine), font, (int)QuestionText)), line,
                    HorizontalAlignment.Left, -1, (int)QuestionText, colour);
                y += QuestionLine;
            }
            y += Rule / 2;
            DrawLine(new Vector2(box.Position.X + TouchPad, y), new Vector2(box.End.X - TouchPad, y), Ui.DimGold, Lacquer.Line(1, s));
            for (int i = 0; i < _dialog.Items.Count; i++)
            {
                var r = ItemRect(i);
                bool hot = i == _hot || i == _pressed;
                if (_dialog.Items[i].Warning)
                    Ui.DrawRedButton(this, r, hot, false, s);
                else
                    Ui.DrawRowBox(this, r, hot, s);
                var (face, size, colour) = _dialog.Items[i].Warning ? ((Font)_brush, TouchItemText, Color.Color8(250, 226, 160)) : (font, TouchItemText - 2, Ui.Text);
                int px = (int)size;
                while (px > 11 && face.GetStringSize(_labels[i], fontSize: px).X > r.Size.X - 12)
                    px--;
                DrawString(face, new Vector2(r.Position.X, Lacquer.Baseline(r, face, px)), _labels[i], HorizontalAlignment.Center, r.Size.X, px,
                    colour);
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
