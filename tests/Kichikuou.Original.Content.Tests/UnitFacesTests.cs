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
        Assert.Equal(UnitFaces.Default, UnitFaces.Default.Clamped());
        Assert.Equal(new FaceCrop(88, 56, 144), UnitFaces.CropOf(1215, 320, 256));    // Aladdin's sprite (commander 21): its middle
    }

    [Fact]
    public void A_stand_ins_edit_keeps_its_sprites_size_through_the_file()
    {
        var crops = new FaceCrops();
        crops.Set(1302, new FaceCrop(150, 90, 120), 320, 256);             // Sergeant Hanny's (commander 83) sprite
        crops.Set(1501, new FaceCrop(300, 0, 300), 320, 256);              // Kaiju Gaga's (commander 187): clamped

        Assert.Equal(new FaceCrop(150, 90, 120), crops.CropOf(1302, 320, 256));
        Assert.Equal(new FaceCrop(64, 0, 256), crops.CropOf(1501, 320, 256));
        Assert.Equal("1302 150 90 120 320 256\n1501 64 0 256 320 256\n", crops.Write());
        Assert.Equal(crops.Edits, FaceCrops.Read(crops.Write()).Edits);
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
    [ScenarioFact]
    public async Task The_booted_games_units_have_their_faces_but_unit_48()
    {
        var game = await EndGame.BootAsync();

        var faces = UnitFaces.Units(game.State);

        Assert.Equal(Enumerable.Range(1, 50), faces.Select(f => f.Record));
        Assert.Equal(("Rance", 1), (faces[0].Name, faces[0].FaceCg));
        Assert.Equal(("Asuka", 430), (faces[10].Name, faces[10].FaceCg));
        Assert.Equal(("", 0), (faces[47].Name, faces[47].FaceCg));
        AssertFacesExist(faces);
    }

    /// <summary>
    /// The booted game's commanders: 197 from <c>SET_CHR</c>; commander 100, the scenes' own, is empty
    /// and not listed; nine of the rest have no face (Aladdin, commander 21, among them) and stand in
    /// with their battle sprites (Sergeant Hanny, commander 83: CG 1302; Kaiju Gaga, commander 187: CG 1501).
    /// </summary>
    [ScenarioFact]
    public async Task The_booted_games_commanders_have_their_faces_or_their_sprites()
    {
        var game = await EndGame.BootAsync();

        var faces = UnitFaces.Commanders(game.State);

        Assert.Equal(196, faces.Count);
        Assert.DoesNotContain(faces, f => f.Record == 100);
        Assert.Equal(("Ex", 405), (faces[0].Name, faces[0].FaceCg));     // Ex (commander 1)
        Assert.Contains(new RecordFace(21, "Aladdin", 0, 1215), faces);
        Assert.Equal(9, faces.Count(f => f.FaceCg == 0));
        Assert.All(faces, f => Assert.Equal(f.FaceCg == 0, f.StandIn != 0));   // the faceless have their sprites
        Assert.Contains(new RecordFace(83, "Sergeant Hanny", 0, 1302), faces);
        Assert.Contains(new RecordFace(187, "Kaiju Gaga", 0, 1501), faces);
        AssertFacesExist(faces);
    }

    /// <summary>Every square of the table lies inside its CG, a face's 192×256 or a battle sprite's 320×256.</summary>
    [LocalFolderFact("cg")]
    public void Every_exception_lies_inside_its_picture()
    {
        var cgs = new CgLibrary(LocalData.CgDirectory);
        Assert.All(UnitFaces.Exceptions, e =>
        {
            var cg = cgs.Get(e.Key);
            Assert.NotNull(cg);
            Assert.Equal(e.Value, e.Value.Clamped(cg.Width, cg.Height));
        });
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
