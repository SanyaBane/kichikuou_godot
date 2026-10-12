using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The faces our screens show (<see cref="UnitFaces"/>): each CG decoded once as <c>G n, 40</c> draws
/// it (palette index 40 transparent) and its square shrunk to a size on the screen.
/// </summary>
internal sealed class FacePictures(CgLibrary cgs)
{
    private readonly Dictionary<int, Image?> _images = [];
    private readonly Dictionary<(int Cg, FaceCrop Crop, int Size), ImageTexture?> _squares = [];

    /// <summary>The player's CGs.</summary>
    public CgLibrary Cgs { get; } = cgs;

    /// <summary>CG <paramref name="cg"/> as <c>G n, 40</c> draws it, or null (0, or not in the player's files).</summary>
    public Image? Picture(int cg)
    {
        if (cg == 0)
            return null;
        if (!_images.TryGetValue(cg, out var image))
        {
            image = Cgs.Get(cg) is { } picture
                ? Image.CreateFromData(picture.Width, picture.Height, false, Image.Format.Rgba8, picture.ToRgba(40))
                : null;
            _images[cg] = image;
        }
        return image;
    }

    /// <summary>CG <paramref name="cg"/>'s size: a face's 192×256 or a stand-in's battle sprite's (a face's without the CG).</summary>
    public (int Width, int Height) SizeOf(int cg) =>
        Picture(cg) is { } image ? (image.GetWidth(), image.GetHeight()) : (UnitFaces.Width, UnitFaces.Height);

    /// <summary>The square in use for CG <paramref name="cg"/>: an edit of <paramref name="crops"/>, else the built-in one.</summary>
    public FaceCrop CropOf(int cg, FaceCrops crops)
    {
        var (width, height) = SizeOf(cg);
        return crops.CropOf(cg, width, height);
    }

    /// <summary>CG <paramref name="cg"/>'s <paramref name="crop"/> shrunk to <paramref name="size"/>² screen pixels, or null.</summary>
    public Image? Square(int cg, FaceCrop crop, int size) =>
        Picture(cg) is { } image ? Shrink(image.GetRegion(new Rect2I(crop.X, crop.Y, crop.Size, crop.Size)), size) : null;

    /// <summary>As <see cref="Square"/>, a texture kept for the next frame that asks for the same.</summary>
    public ImageTexture? SquareTexture(int cg, FaceCrop crop, int size)
    {
        if (!_squares.TryGetValue((cg, crop, size), out var texture))
        {
            texture = Square(cg, crop, size) is { } square ? ImageTexture.CreateFromImage(square) : null;
            _squares[(cg, crop, size)] = texture;
        }
        return texture;
    }

    /// <summary>
    /// <paramref name="image"/> resized to <paramref name="size"/>² without the colour key's magenta
    /// bleeding in: the transparent pixels' colour is dropped (premultiplied) before the filter and
    /// the alpha divided out after it.
    /// </summary>
    public static Image Shrink(Image image, int size)
    {
        image.PremultiplyAlpha();
        image.Resize(size, size, Image.Interpolation.Lanczos);
        byte[] rgba = image.GetData();
        for (int i = 0; i < rgba.Length; i += 4)
        {
            int a = rgba[i + 3];
            if (a is 0 or 255)
                continue;
            for (int c = 0; c < 3; c++)
                rgba[i + c] = (byte)Math.Min(255, rgba[i + c] * 255 / a);
        }
        return Image.CreateFromData(size, size, false, Image.Format.Rgba8, rgba);
    }
}
