using Godot;
using Kichikuou.Content.Audio;
using Kichikuou.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Sound effects from the extracted <c>wav\</c> folder and music from the game folder's
/// <c>_inmm.ini</c> playlist (docs/assets.md §4-§5). <c>SQ l, r</c> plays its two sounds on
/// buses panned hard left and right, as xsystem35 mixes them into one stereo stream.
/// </summary>
public partial class BattleAudio : Node, IBattleAudio
{
    private const string LeftBus = "SfxLeft", RightBus = "SfxRight";

    private readonly Dictionary<int, AudioStream?> _sounds = [];
    private MusicTable? _music;
    private AudioStreamPlayer _sfx = null!, _left = null!, _right = null!, _bgm = null!;

    /// <summary>The track <c>SS</c> last started (0 = none), for <c>SR</c>.</summary>
    public int Track { get; private set; }

    public override void _Ready()
    {
        EnsureBus(LeftBus, -1f);
        EnsureBus(RightBus, 1f);
        _sfx = AddPlayer("Master");
        _left = AddPlayer(LeftBus);
        _right = AddPlayer(RightBus);
        _bgm = AddPlayer("Master");
        _bgm.VolumeDb = -6;
        if (System.IO.Directory.Exists(GameData.GameDirectory))
            _music = new MusicTable(GameData.GameDirectory);
    }

    private AudioStreamPlayer AddPlayer(string bus)
    {
        var player = new AudioStreamPlayer { Bus = bus };
        AddChild(player);
        return player;
    }

    private static void EnsureBus(string name, float pan)
    {
        if (AudioServer.GetBusIndex(name) >= 0)
            return;
        AudioServer.AddBus();
        int index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, name);
        AudioServer.SetBusSend(index, "Master");
        AudioServer.AddBusEffect(index, new AudioEffectPanner { Pan = pan });
    }

    public void PlaySound(int number) => Play(_sfx, number);

    public void PlaySoundPair(int left, int right)
    {
        // SQ with a number < 1 stops instead (cmds.c:308-311).
        if (left < 1 || right < 1)
        {
            _left.Stop();
            _right.Stop();
            return;
        }
        Play(_left, left);
        Play(_right, right);
    }

    private void Play(AudioStreamPlayer player, int number)
    {
        if (Sound(number) is not { } stream)
            return;
        player.Stream = stream;
        player.Play();
    }

    private AudioStream? Sound(int number)
    {
        if (_sounds.TryGetValue(number, out var cached))
            return cached;
        AudioStream? stream = null;
        if (SoundTable.FileName(number) is { } name)
        {
            string path = System.IO.Path.Combine(GameData.WavDirectory, name);
            if (System.IO.File.Exists(path))
                stream = AudioStreamWav.LoadFromFile(path, new Godot.Collections.Dictionary());
        }
        _sounds[number] = stream;
        return stream;
    }

    public void PlayMusic(int track)
    {
        Track = track;
        _bgm.Stop();
        if (_music?.PathOf(track) is not { } path || !System.IO.File.Exists(path))
            return;
        var stream = AudioStreamMP3.LoadFromFile(path);
        stream.Loop = true;
        _bgm.Stream = stream;
        _bgm.Play();
    }
}
