using Godot;
using Kichikuou.Content.Audio;
using Kichikuou.Original.Content.Audio;
using Kichikuou.Original.Content.Battles;

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
        _sfx.Finished += OnSoundFinished;
        _left = AddPlayer(LeftBus);
        _right = AddPlayer(RightBus);
        _bgm = AddPlayer("Master");
        _bgm.VolumeDb = -6;
        _bgm.Finished += OnMusicFinished;
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

    /// <summary>
    /// <c>SP n, loops</c>: the effect replaces the one playing (one PCM channel), <paramref name="loops"/>
    /// times or endlessly for 0; <c>SP 0</c> stops it.
    /// </summary>
    public void PlaySound(int number, int loops = 1)
    {
        _sfxLoopsLeft = 0;
        if (number == 0)
        {
            _sfx.Stop();
            return;
        }
        _sfxLoopsLeft = loops == 0 ? int.MaxValue : loops;
        Play(_sfx, number);
    }

    private int _sfxLoopsLeft;

    /// <summary>An effect played a counted number of times (or endlessly) starts again until the count is used up.</summary>
    private void OnSoundFinished()
    {
        if (_sfxLoopsLeft == int.MaxValue || --_sfxLoopsLeft > 0)
            _sfx.Play();
    }

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

    /// <summary>
    /// <c>SS n</c>: the track already started goes on playing, even when it has ended (xsystem35
    /// <c>muscd_start</c> returns at once for the current track, <c>music_cdrom.c:81-82</c>); the
    /// map loop sends <c>SS</c> after every command. <paramref name="loops"/> plays it that many
    /// times, 0 endlessly (<c>SL</c>, <c>Mix_PlayMusic</c>).
    /// </summary>
    public void PlayMusic(int track, int loops = 0)
    {
        if (track != 0 && track == Track)
            return;
        Track = track;
        _bgm.Stop();
        _started = null;
        if (_music?.PathOf(track) is not { } path || !System.IO.File.Exists(path))
            return;
        var stream = AudioStreamMP3.LoadFromFile(path);
        stream.Loop = loops == 0;
        _loopsLeft = loops;
        _bgm.Stream = stream;
        _bgm.Play();
        _started = System.Diagnostics.Stopwatch.StartNew();
    }

    private System.Diagnostics.Stopwatch? _started;
    private int _loopsLeft;

    /// <summary>A track played a counted number of times starts again until the count is used up.</summary>
    private void OnMusicFinished()
    {
        if (--_loopsLeft > 0)
            _bgm.Play();
        else
            _started = null;
    }

    /// <summary>
    /// <c>SC</c> with xsystem35's <c>cdrom.mp3.c</c>: while the music plays, the time since it was
    /// started (wall clock, across its loops), else null ("nothing plays").
    /// </summary>
    public TimeSpan? MusicPosition => _started is { } started && _bgm.Playing ? started.Elapsed : null;
}
