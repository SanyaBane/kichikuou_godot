using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The title's own screen on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §44-§45, PE6, the
/// owner's choice), over the engine's picture: CG 301's stone over the whole screen, the logo laid whole, and while the
/// title polls the mouse a finger row for each button it draws — its character cut without its word, the characters'
/// middles on one line nearer the middle, the word left-aligned right of them in the brush face, Start a red button at the
/// right thumb. A row's tap is told (<see cref="Chosen"/>) and becomes a click in the middle of its button's box; a long
/// press (or a right click) is told (<see cref="Described"/>) and becomes the right click there, the button's description.
/// While the rows show, the view takes every press (the picture under it is hidden: its targets are the rows); while a
/// description shows, none (they are the key wait's). The pictures are cut from the player's CGs (<see cref="PhoneTitleArt"/>),
/// the places <see cref="PhoneTitleUpright"/>'s. Drawn in the frame's layout pixels over the whole screen
/// (<see cref="PhoneUnits"/>). Only on the phone held upright.
/// </summary>
internal sealed partial class TitleUprightView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>A press held this long on a row is the right click; moved this far it is no press (as <c>GameScreen.Touch</c>'s).</summary>
    private const ulong LongPressMs = 500;
    private const float LongPressSlop = 12;

    /// <summary>The rows' plates let the stone through a little (the mock-up's 88%).</summary>
    private const float RowAlpha = 224 / 255f;

    private readonly CgLibrary _cgs;
    private readonly FontFile _brush = Lacquer.Brush();
    private Art? _art;
    private PhoneFrame? _phone;
    private IReadOnlyList<int> _buttons = [];
    private bool _rows;
    private int _pressed = -1;                      // the row held
    private Vector2 _pressedAt;
    private ulong _pressedMs;
    private bool _held;                             // the press held long enough: the right click is told

    /// <summary>A row tapped: its button (0-7).</summary>
    public event Action<int>? Chosen;

    /// <summary>A row held, or right-clicked: its button (0-7).</summary>
    public event Action<int>? Described;

    public TitleUprightView(CgLibrary cgs)
    {
        _cgs = cgs;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Linear;
        Visible = false;
    }

    /// <summary>The logo's height in its CG pixels (the description's place follows it), or null without the CGs.</summary>
    public int? LogoHeight => Pictures()?.LogoSize.Y;

    /// <summary>Shows the screen for <paramref name="buttons"/>, in the rows' order; their rows if <paramref name="rows"/>.</summary>
    public void ShowTitle(IReadOnlyList<int> buttons, bool rows)
    {
        if (!Visible || rows != _rows || !buttons.SequenceEqual(_buttons))
        {
            _buttons = buttons;
            _rows = rows;
            _pressed = -1;
            MouseFilter = rows ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            QueueRedraw();
        }
        Visible = true;
    }

    public void HideTitle()
    {
        Visible = false;
        _pressed = -1;
    }

    /// <summary>Lays the view over the whole screen held upright; on any other device it never shows.</summary>
    public void Place()
    {
        var phone = Device.Upright(this);
        if (phone is null)
        {
            Visible = false;
            return;
        }
        var size = GetParentAreaSize();
        if (phone == _phone && Size == size)
            return;
        Position = Vector2.Zero;
        Size = size;
        _phone = phone;
        QueueRedraw();
    }

    private int RowAt(Vector2 at)
    {
        if (_phone is not { } f || !_rows)
            return -1;
        for (int i = 0; i < _buttons.Count; i++)
            if (PhoneTitleUpright.Row(f, i, _buttons.Count).Contains(at.X, at.Y))
                return i;
        return -1;
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = RowAt(button.Position);
                if (button.Pressed)
                {
                    (_pressed, _pressedAt, _pressedMs, _held) = (at, button.Position, Time.GetTicksMsec(), false);
                }
                else
                {
                    if (!_held && at >= 0 && at == _pressed)
                        Chosen?.Invoke(_buttons[at]);
                    _pressed = -1;
                }
                QueueRedraw();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } button:
                if (RowAt(button.Position) is >= 0 and var row)
                    Described?.Invoke(_buttons[row]);
                break;
            case InputEventMouseMotion motion when _pressed >= 0 && !_held && motion.Position.DistanceTo(_pressedAt) > LongPressSlop:
                _pressed = -1;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    public override void _Process(double delta)
    {
        if (!Visible || _pressed < 0 || _held || Time.GetTicksMsec() - _pressedMs < LongPressMs)
            return;
        _held = true;
        Described?.Invoke(_buttons[_pressed]);
    }

    public override void _Draw()
    {
        if (_phone is not { } f || Pictures() is not { } art)
            return;
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        foreach (var (box, flipped) in PhoneTitleUpright.Tiles(f, art.Texture.GetHeight()))
            DrawTextureRect(flipped ? art.Mirrored : art.Texture, u.Rect(box), false);
        DrawTextureRect(art.FriezeTop, u.Rect(PhoneTitleUpright.FriezeTop(f, art.FriezeTop.GetHeight())), false);
        DrawTextureRect(art.FriezeFoot, u.Rect(PhoneTitleUpright.FriezeFoot(f, art.FriezeFoot.GetHeight())), false);
        DrawTextureRect(art.Logo, u.Rect(PhoneTitleUpright.Logo(f, _buttons.Count, art.LogoSize.X, art.LogoSize.Y)), false);
        if (!_rows || _buttons.Count == 0)
            return;
        int size = u.Size(PhoneTitleUpright.WordText);
        float widestWord = PhoneTitleUpright.Words.Max(w => _brush.GetStringSize(w, fontSize: size).X) * u.K;
        float widestIcon = art.Characters.Max(c => PhoneTitleUpright.IconSize(f, c.GetWidth(), c.GetHeight()).W);
        var (left, words) = PhoneTitleUpright.Column(f, widestIcon, widestWord);
        for (int i = 0; i < _buttons.Count; i++)
        {
            var row = PhoneTitleUpright.Row(f, i, _buttons.Count);
            var box = u.Rect(row);
            int button = _buttons[i];
            bool held = _pressed == i;
            if (button == PhoneTitleUpright.Start)
                Ui.DrawRedButton(this, box, held, held, u.K);
            else
            {
                DrawRect(box, held ? Ui.RowHover : new Color(Ui.Row, RowAlpha));
                DrawRect(box, held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(3), u.K));
            }
            var character = art.Characters[button];
            var icon = PhoneTitleUpright.IconSize(f, character.GetWidth(), character.GetHeight());
            DrawTextureRect(character, u.Rect(PhoneTitleUpright.Icon(row, left, widestIcon, icon)), false);
            string word = PhoneTitleUpright.Words[button];
            DrawString(_brush, new Vector2(words / u.K, PhonePickerParts.Middle(_brush, size, box.GetCenter().Y)), word,
                fontSize: size, modulate: Ui.Name);
        }
    }

    /// <summary>The title's pictures cut from the player's CGs, once a library; null without them.</summary>
    private Art? Pictures()
    {
        if (_art is not null)
            return _art;
        if (Cache is { } cached && cached.From == _cgs)
            return _art = cached.Art;
        if (_cgs.Get(PhoneTitleArt.BackgroundCg) is not { } background || _cgs.Get(PhoneTitleArt.LitCg) is not { } lit)
            return null;
        var logo = PhoneTitleArt.Logo(background);
        var texture = Image(PhoneTitleArt.Texture(background));
        var mirrored = Image(PhoneTitleArt.Texture(background));
        mirrored.FlipY();
        _art = new Art(
            [.. Enumerable.Range(0, PhoneTitleUpright.Count).Select(n => Texture(Image(PhoneTitleArt.Character(background, lit, n))))],
            Texture(Image(logo)), new Vector2I(logo.Width, logo.Height),
            Texture(texture), Texture(mirrored),
            Texture(Image(PhoneTitleArt.FriezeTop(background))), Texture(Image(PhoneTitleArt.FriezeFoot(background))));
        Cache = (_cgs, _art);
        return _art;
    }

    private static Image Image(PhoneTitleArt.Picture p) =>
        Godot.Image.CreateFromData(p.Width, p.Height, false, Godot.Image.Format.Rgba8, p.Rgba);

    private static ImageTexture Texture(Image image) => ImageTexture.CreateFromImage(image);

    /// <summary>The last library's pictures: the game's screen and the gallery's entries share them.</summary>
    private static (CgLibrary From, Art Art)? Cache;

    private sealed record Art(
        ImageTexture[] Characters, ImageTexture Logo, Vector2I LogoSize, ImageTexture Texture, ImageTexture Mirrored,
        ImageTexture FriezeTop, ImageTexture FriezeFoot);
}
