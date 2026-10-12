using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="UnitFaces"/>: the units' faces and their squares (docs/ui/deployment-table.md, third part).</summary>
public class UnitFacesTests
{
    [Fact]
    public void A_face_is_cut_to_the_top_144_pixels_of_its_middle_but_Asukas_lower()
    {
        Assert.Equal(new FaceCrop(24, 0, 144), UnitFaces.CropOf(1));        // Rance's (unit 1)
        Assert.Equal(new FaceCrop(24, 64, 144), UnitFaces.CropOf(430));     // Asuka's (unit 11), under Pigu's hat
        Assert.All(UnitFaces.Exceptions.Values.Append(UnitFaces.Default), c =>
        {
            Assert.InRange(c.X, 0, UnitFaces.Width - c.Size);
            Assert.InRange(c.Y, 0, UnitFaces.Height - c.Size);
        });
    }

    /// <summary>The booted game's units: names and faces from <c>SET_CHR</c>; unit 48 is empty until a scene fills it.</summary>
    [Fact]
    public async Task The_booted_games_units_have_their_faces_but_unit_48()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;

        var faces = UnitFaces.Units(game.State);

        Assert.Equal(Enumerable.Range(1, 50), faces.Select(f => f.Unit));
        Assert.Equal(("Rance", 1), (faces[0].Name, faces[0].FaceCg));
        Assert.Equal(("Asuka", 430), (faces[10].Name, faces[10].FaceCg));
        Assert.Equal(("", 0), (faces[47].Name, faces[47].FaceCg));
        var cgs = new CgLibrary(LocalData.CgDirectory);
        Assert.All(faces.Where(f => f.FaceCg != 0), f =>
        {
            var cg = cgs.Get(f.FaceCg);
            Assert.NotNull(cg);
            Assert.Equal((UnitFaces.Width, UnitFaces.Height), (cg.Width, cg.Height));
        });
    }
}
