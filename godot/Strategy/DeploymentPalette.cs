using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The colours first made for our deployment screen: the first, dark blue, sat badly with the portrait's gold
/// frame, so the owner looked at four more (2026-10-12), kept them all for the player to choose (Option → Display)
/// and made steel the default. The deployment screen has been lacquer since the owner's E1/E2 (2026-10-19,
/// docs/ui/deployment-table.md); the palettes still colour our battle screen, <see cref="BattleChosen"/>. The map's name
/// plates had a palette of their own for a day (2026-10-24) and keep steel's colours as their own since (MapMarkersView).
/// </summary>
internal sealed record DeploymentPalette(
    string Name,
    Color Back,             // the backdrop, the panels, an empty place
    Color Card,             // a member's card
    Color Border,           // the panels' and the cards' frames
    Color Text,
    Color Muted,            // labels, the troop type, an empty place's text
    Color Warning,          // ×, war expenses above the gold
    Color Accent,           // the frame of the place a drag would drop on
    Color Button,
    Color ButtonHover,
    Color ButtonPressed,
    Color ButtonDisabled,
    Color ButtonBorder,
    Color ButtonText,
    Color TableBack,        // the table's body (a little see-through)
    Color TableTitle,       // the table's headers
    Color MemberRow,        // the party's members' rows
    Color GreyText)         // the rows of units deployed this week
{
    /// <summary>The first palette: dark blue.</summary>
    public static readonly DeploymentPalette Navy = new("navy",
        Back: new(0.04f, 0.05f, 0.12f), Card: new(0.08f, 0.1f, 0.22f), Border: new(0.18f, 0.23f, 0.42f),
        Text: new(0.95f, 0.95f, 0.95f), Muted: new(0.56f, 0.59f, 0.73f), Warning: new(0.88f, 0.48f, 0.48f),
        Accent: new(0.95f, 0.8f, 0.35f),
        Button: new(0.14f, 0.19f, 0.37f), ButtonHover: new(0.22f, 0.29f, 0.52f), ButtonPressed: new(0.1f, 0.13f, 0.27f),
        ButtonDisabled: new(0.08f, 0.1f, 0.18f), ButtonBorder: new(0.35f, 0.42f, 0.69f), ButtonText: new(0.95f, 0.95f, 0.95f),
        TableBack: new(0.04f, 0.05f, 0.12f, 0.94f), TableTitle: new(0.12f, 0.14f, 0.26f),
        MemberRow: new(0.2f, 0.3f, 0.6f, 0.6f), GreyText: new(0.5f, 0.5f, 0.55f));

    /// <summary>Gold on warm charcoal: the portrait's frame carried over the whole screen.</summary>
    public static readonly DeploymentPalette Gold = new("gold",
        Back: Hex("15120e"), Card: Hex("2a2219"), Border: Hex("b8913f"),
        Text: Hex("f2e8d2"), Muted: Hex("a8977a"), Warning: Hex("e07a5f"), Accent: Hex("ffd36b"),
        Button: Hex("3a2d1c"), ButtonHover: Hex("574226"), ButtonPressed: Hex("2a2015"),
        ButtonDisabled: Hex("1d1812"), ButtonBorder: Hex("d4a94e"), ButtonText: Hex("f6e3b0"),
        TableBack: Hex("15120ef0"), TableTitle: Hex("2e2418"), MemberRow: Hex("6b5320a0"), GreyText: Hex("6e6658"));

    /// <summary>Gold on grey steel: the original's grey tiles with their gold edges.</summary>
    public static readonly DeploymentPalette Steel = new("steel",
        Back: Hex("2b2d31"), Card: Hex("3b3e44"), Border: Hex("a88a4a"),
        Text: Hex("f0f0ee"), Muted: Hex("a5a7ab"), Warning: Hex("e2725b"), Accent: Hex("f2c75c"),
        Button: Hex("45484f"), ButtonHover: Hex("575b63"), ButtonPressed: Hex("34363b"),
        ButtonDisabled: Hex("2f3135"), ButtonBorder: Hex("c9a85a"), ButtonText: Hex("f4e6c0"),
        TableBack: Hex("2b2d31f0"), TableTitle: Hex("3d4046"), MemberRow: Hex("7a6430a0"), GreyText: Hex("7d7f84"));

    /// <summary>Dark ink on parchment, framed in gold: the map's sepia.</summary>
    public static readonly DeploymentPalette Parchment = new("parchment",
        Back: Hex("e9dfc6"), Card: Hex("f6efdc"), Border: Hex("9c7a3c"),
        Text: Hex("2c2116"), Muted: Hex("7a6a52"), Warning: Hex("a8321e"), Accent: Hex("b8860b"),
        Button: Hex("d9c79c"), ButtonHover: Hex("e8d8ae"), ButtonPressed: Hex("c8b68a"),
        ButtonDisabled: Hex("ddd3bc"), ButtonBorder: Hex("8a6a2e"), ButtonText: Hex("3a2a14"),
        TableBack: Hex("efe6cff5"), TableTitle: Hex("d8c79f"), MemberRow: Hex("e2c46ca0"), GreyText: Hex("a39a88"));

    /// <summary>Gold on deep crimson: Leazas's royal colours.</summary>
    public static readonly DeploymentPalette Crimson = new("crimson",
        Back: Hex("1e0d10"), Card: Hex("33161b"), Border: Hex("b8913f"),
        Text: Hex("f4e9dc"), Muted: Hex("b39a8a"), Warning: Hex("ff8a70"), Accent: Hex("ffd36b"),
        Button: Hex("4a1d24"), ButtonHover: Hex("66303a"), ButtonPressed: Hex("3a141a"),
        ButtonDisabled: Hex("2a1216"), ButtonBorder: Hex("d4a94e"), ButtonText: Hex("f6e3b0"),
        TableBack: Hex("1e0d10f0"), TableTitle: Hex("3d1a20"), MemberRow: Hex("7a5a24a0"), GreyText: Hex("7a6466"));

    /// <summary>
    /// Our other screens' lacquer and gold (<see cref="Lacquer"/>), offered for the battle screen to look at (owner,
    /// 2026-10-24): if it holds, the battle screen takes it for good and the palettes go.
    /// </summary>
    public static readonly DeploymentPalette LacquerGold = new("lacquer",
        Back: Lacquer.Panel with { A = 1 }, Card: Lacquer.Row, Border: Lacquer.Gold,
        Text: Lacquer.Text, Muted: Lacquer.Quiet, Warning: Lacquer.Minus, Accent: Lacquer.HoverLine,
        Button: Lacquer.Row, ButtonHover: Lacquer.RowHover, ButtonPressed: Color.Color8(24, 18, 13),
        ButtonDisabled: Lacquer.OffButton, ButtonBorder: Lacquer.Gold, ButtonText: Lacquer.Name,
        TableBack: Lacquer.Panel, TableTitle: Color.Color8(46, 34, 22), MemberRow: Lacquer.PartyRow, GreyText: Lacquer.OffText);

    public static IReadOnlyList<DeploymentPalette> All { get; } = [Navy, Gold, Steel, Parchment, Crimson, LacquerGold];

    private static DeploymentPalette? _battle;

    /// <summary>The name in the menu: "Steel" for steel.</summary>
    public string Title => char.ToUpperInvariant(Name[0]) + Name[1..];

    /// <summary>
    /// The battle screen's palette, with what comes before its field (the scout report, a strategy's cut-in) and the touch
    /// mode's Skip under its cards: <c>[display] palette_battle</c> in <c>user://settings.cfg</c> (see <see cref="Load"/>).
    /// Setting it saves it for later launches.
    /// </summary>
    public static DeploymentPalette BattleChosen
    {
        get => _battle ??= Load("palette_battle");
        set => Save(_battle = value, "palette_battle");
    }

    /// <summary>
    /// <c>--palette &lt;name&gt;</c> for the launch (development), else the saved <paramref name="key"/>, else the one
    /// palette saved before the split of 2026-10-24 (<c>[display] palette</c>), else steel.
    /// </summary>
    private static DeploymentPalette Load(string key)
    {
        var args = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(args, "--palette");
        if (at >= 0 && at + 1 < args.Length && Named(args[at + 1]) is { } given)
            return given;
        var config = new ConfigFile();
        if (config.Load(GameData.SettingsPath) != Error.Ok)
            return Steel;
        return Named((string)config.GetValue("display", key, "")) ?? Named((string)config.GetValue("display", "palette", "")) ?? Steel;
    }

    private static void Save(DeploymentPalette palette, string key)
    {
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("display", key, palette.Name);
        config.Save(GameData.SettingsPath);
    }

    private static DeploymentPalette? Named(string name) => All.FirstOrDefault(p => p.Name == name);

    private static Color Hex(string rgba) => Color.FromHtml(rgba);
}
