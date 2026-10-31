using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneTitleArt"/>: the title's characters, logo and texture cut from the player's CGs 301 and 302
/// (docs/ui/phone.md, §44: the sizes the round's analysis found).
/// </summary>
public class PhoneTitleArtTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));

    private static CgImage Background => Cgs.Value.Get(PhoneTitleArt.BackgroundCg)!;

    private static CgImage Lit => Cgs.Value.Get(PhoneTitleArt.LitCg)!;

    private static int Opaque(PhoneTitleArt.Picture p) => Enumerable.Range(0, p.Width * p.Height).Count(i => p.Rgba[i * 4 + 3] != 0);

    [Theory]
    [InlineData(0, 147, 143)]     // Destiny: the girl on her saucer and the small creature at her left
    [InlineData(1, 106, 122)]     // B.G.M
    [InlineData(2, 113, 95)]      // Alice: the easel and the ghost
    [InlineData(3, 75, 85)]       // Please
    [InlineData(4, 92, 79)]       // Spy, cut under her skirt
    [InlineData(5, 85, 105)]      // Load
    [InlineData(6, 121, 133)]     // Start
    [InlineData(7, 146, 158)]     // C.G
    public void Each_character_is_cut_without_its_word(int button, int width, int height)
    {
        var p = PhoneTitleArt.Character(Background, Lit, button);
        Assert.Equal((width, height), (p.Width, p.Height));
        Assert.True(Opaque(p) > p.Width * p.Height / 3);
    }

    [Fact]
    public void The_logo_is_its_letters_with_a_dark_edge()
    {
        var p = PhoneTitleArt.Logo(Background);
        // The letters span 116-525 × 132-264; the edge 2 and the margin 1 round them.
        Assert.Equal((410 + 6, 133 + 6), (p.Width, p.Height));
        Assert.Equal(0, p.Rgba[3]);                                         // a corner of the margin is clear
    }

    [Fact]
    public void The_texture_has_no_logo_left()
    {
        var p = PhoneTitleArt.Texture(Background);
        Assert.Equal((640, 364), (p.Width, p.Height));
        var palette = Background.OwnPalette;
        var red = palette[86];                                              // the logo's red
        for (int i = 0; i < p.Width * p.Height; i++)
            Assert.False((p.Rgba[i * 4], p.Rgba[i * 4 + 1], p.Rgba[i * 4 + 2]) == red, $"the logo's red at {i % p.Width}, {i / p.Width}");
        Assert.Equal((640, 16), (PhoneTitleArt.FriezeTop(Background).Width, PhoneTitleArt.FriezeTop(Background).Height));
        Assert.Equal((640, 20), (PhoneTitleArt.FriezeFoot(Background).Width, PhoneTitleArt.FriezeFoot(Background).Height));
    }
}
