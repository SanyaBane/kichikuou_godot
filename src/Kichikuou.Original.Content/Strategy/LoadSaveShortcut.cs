using Kichikuou.Engine.Script;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>The list our panel's Load or Save row opens: its answer in <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s Load / Save / Don't menu.</summary>
public enum SaveList
{
    /// <summary>Load (<c>L_0022f</c>).</summary>
    Load = 1,

    /// <summary>Save (<c>L_006a2</c>).</summary>
    Save = 2,
}

/// <summary>
/// Our command panel's Load and Save rows (owner, 2026-10-19; docs/ui/command-screen.md, "The command panel"):
/// both answer <c>COMMAND</c>'s Load/Save, which calls <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c> with mode 2, the Load / Save / Don't
/// menu (<c>L_001c2</c>, <c>:82-104</c>). This answers that menu unseen: the row's list the first time; Don't
/// when the menu comes back (the list left by a right click), so the player is back on the map. Interface only:
/// the script runs as it is.
/// </summary>
public sealed class LoadSaveShortcut
{
    private const string File = "ﾛｰﾄﾞｾｰﾌﾞ.ADV";
    private const int Dont = 3;

    private SaveList? _list;
    private bool _opened;

    /// <summary>Whether a list is under way: the menu is answered by this until it is left.</summary>
    public bool Running => _list is not null;

    /// <summary>The panel answered: with <paramref name="list"/> for a Load or Save row, null for anything else.</summary>
    public void Start(SaveList? list)
    {
        _list = list;
        _opened = false;
    }

    /// <summary>The save screen closed (<c>SaveScreenEvent</c>, also after a load): nothing under way.</summary>
    public void Stop() => Start(null);

    /// <summary>The answer to <paramref name="items"/> when they are the Load / Save / Don't menu and a list is under way; else null.</summary>
    public int? Choose(IReadOnlyList<SceneMenuItem> items)
    {
        if (_list is not { } list || !IsMenu(items))
            return null;
        if (!_opened)
        {
            _opened = true;
            return (int)list;
        }
        Stop();
        return Dont;
    }

    /// <summary>Whether <paramref name="items"/> are <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>'s Load / Save / Don't (<c>:91, 95, 99</c>).</summary>
    public static bool IsMenu(IReadOnlyList<SceneMenuItem> items) =>
        items.Count == 3 && items.All(i => i.At.File == File)
        && items[0].At.Line == 91 && items[1].At.Line == 95 && items[2].At.Line == 99;
}
