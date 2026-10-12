using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="UnitFaces"/> and <see cref="FaceCrops"/>: the faces and their squares (docs/ui/deployment-table.md, third part).</summary>
public class UnitFacesTests
{
    [Fact]
    public void A_face_is_cut_to_144_pixels_of_its_middle_below_the_gold_frame_but_the_owners_own()
    {
        Assert.Equal(new FaceCrop(24, 7, 144), UnitFaces.CropOf(5));        // a face the owner left as it is
        Assert.Equal(new FaceCrop(33, 7, 128), UnitFaces.CropOf(1));        // Rance's (unit 1), closer
        Assert.Equal(new FaceCrop(26, 45, 144), UnitFaces.CropOf(430));     // Asuka's (unit 11), under Pigu's hat
        Assert.All(UnitFaces.Exceptions.Values.Append(UnitFaces.Default), c => Assert.Equal(c, c.Clamped()));
    }

    [Fact]
    public void The_owners_edits_stay_inside_the_face_and_survive_their_file()
    {
        var crops = new FaceCrops();
        crops.Set(1, new FaceCrop(100, -5, 144));                           // past the right edge and the top
        crops.Set(2, new FaceCrop(24, 16, 400));                            // larger than the CG
        crops.Set(430, new FaceCrop(26, 45, 144));                          // Asuka's built-in square: no edit

        Assert.Equal(new FaceCrop(48, 0, 144), crops.CropOf(1));
        Assert.Equal(new FaceCrop(0, 16, 192), crops.CropOf(2));
        Assert.False(crops.IsEdited(430));
        Assert.Equal(UnitFaces.Default, crops.CropOf(5));
        Assert.Equal("1 48 0 144\n2 0 16 192\n", crops.Write());

        var read = FaceCrops.Read(crops.Write() + "garbage line\n");
        Assert.Equal(crops.Edits, read.Edits);
        read.Reset(1);
        Assert.Equal(UnitFaces.CropOf(1), read.CropOf(1));
    }

    /// <summary>The booted game's units: names and faces from <c>SET_CHR</c>; unit 48 is empty until a scene fills it.</summary>
    [Fact]
    public async Task The_booted_games_units_have_their_faces_but_unit_48()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;

        var faces = UnitFaces.Units(game.State);

        Assert.Equal(Enumerable.Range(1, 50), faces.Select(f => f.Record));
        Assert.Equal(("Rance", 1), (faces[0].Name, faces[0].FaceCg));
        Assert.Equal(("Asuka", 430), (faces[10].Name, faces[10].FaceCg));
        Assert.Equal(("", 0), (faces[47].Name, faces[47].FaceCg));
        AssertFacesExist(faces);
    }

    /// <summary>
    /// The booted game's commanders: 197 from <c>SET_CHR</c>; commander 100, the scenes' own, is empty
    /// and not listed; nine of the rest have no face (Aladdin, commander 21, among them).
    /// </summary>
    [Fact]
    public async Task The_booted_games_commanders_have_their_faces_but_ten()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;

        var faces = UnitFaces.Commanders(game.State);

        Assert.Equal(196, faces.Count);
        Assert.DoesNotContain(faces, f => f.Record == 100);
        Assert.Equal(("Ex", 405), (faces[0].Name, faces[0].FaceCg));     // Ex (commander 1)
        Assert.Contains(new RecordFace(21, "Aladdin", 0), faces);
        Assert.Equal(9, faces.Count(f => f.FaceCg == 0));
        AssertFacesExist(faces);
    }

    private static void AssertFacesExist(IEnumerable<RecordFace> faces)
    {
        var cgs = new CgLibrary(LocalData.CgDirectory);
        Assert.All(faces.Where(f => f.FaceCg != 0), f =>
        {
            var cg = cgs.Get(f.FaceCg);
            Assert.NotNull(cg);
            Assert.Equal((UnitFaces.Width, UnitFaces.Height), (cg.Width, cg.Height));
        });
    }
}
