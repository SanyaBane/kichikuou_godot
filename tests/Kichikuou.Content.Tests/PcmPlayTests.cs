using Kichikuou.Content.Battles;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary><c>IZ</c>'s glide (<c>ags.c:740-751</c>).</summary>
public class PointerGlideTests
{
    /// <summary>Seven eased points then the target; the first steps are the long ones, and leftward moves shift arithmetically as in C.</summary>
    [Fact]
    public void The_pointer_glides_on_the_engines_curve()
    {
        Assert.Equal([(53, 0), (92, 0), (121, 0), (140, 0), (152, 0), (157, 0), (160, 0), (160, 0)],
            PointerGlide.Path((0, 0), (160, 0)));
        // 100 to the left is not the mirror of 100 to the right (that would start at 67):
        // >> rounds towards minus infinity.
        Assert.Equal([(66, 9), (42, 9), (24, 9), (12, 9), (5, 9), (1, 9), (0, 9), (0, 9)],
            PointerGlide.Path((100, 9), (0, 9)));
    }
}

/// <summary><c>PCMPLAY.ADV</c>'s modes as the presenters play them (<see cref="IBattleAudio.PcmPlay"/>).</summary>
public class PcmPlayTests
{
    private sealed class Recorder : IBattleAudio
    {
        public List<(int Number, int Loops)> Sounds { get; } = [];

        public void PlaySound(int number, int loops = 1) => Sounds.Add((number, loops));

        public void PlaySoundPair(int left, int right) { }

        public void PlayMusic(int track, int loops = 0) { }
    }

    /// <summary>
    /// Mode 0 and 1 <c>SP RetVal, 1</c>, mode 2 <c>SP RetVal, 0</c> (endless), mode 3 <c>SP 5, 1</c>
    /// whatever <c>RetVal</c> holds, mode 10 nothing (<c>PCMPLAY.ADV:28-43</c>).
    /// </summary>
    [Fact]
    public void Each_mode_plays_what_the_file_does()
    {
        var audio = new Recorder();
        foreach (int mode in new[] { 0, 1, 2, 3, 10 })
            IBattleAudio.PcmPlay(audio, new SoundEvent((Sys3Int)mode, (Sys3Int)70));
        Assert.Equal([(70, 1), (70, 1), (70, 0), (5, 1)], audio.Sounds);
    }
}
