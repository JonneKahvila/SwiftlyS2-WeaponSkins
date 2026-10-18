using SwiftlyS2.Shared.Players;

using WeaponSkins.Shared;

namespace WeaponSkins;

/// <summary>Sidebar entries, in the order of the sk_nav_* buttons in jk_skins.xml.</summary>
public enum PanelCategory
{
    Home,
    Knives,
    Gloves,
    Agents,
    Rifles,
    Snipers,
    Pistols,
    Smgs,
    Heavy,
    Music
}

public enum PanelView
{
    Home,

    /// <summary>Grid of weapon / knife / glove types within a category.</summary>
    Types,

    /// <summary>Grid of finishes for one item, or the flat agent / music kit list.</summary>
    Finishes,

    Detail
}

public enum TeamChoice
{
    T,
    CT,
    Both
}

/// <summary>
/// Which picture a jk-sk-img panel shows: an item class (weapon/knife/glove defindex, <c>ag</c> for
/// agents, <c>mk</c> for music kits) plus a code (paintkit, agent defindex or music kit id), matching
/// the rules cs2-hud-jk's tools/gen_jk_skins_images.py generates into jk_skins_images.css.
/// </summary>
public sealed record PanelImage(string Item, int Code)
{
    public const int Digits = 5;

    public static PanelImage Agent(int definitionIndex) => new("ag", definitionIndex);
    public static PanelImage MusicKit(int id) => new("mk", id);

    public IEnumerable<string> Classes()
    {
        yield return $"i-{Item}";
        var code = Code.ToString().PadLeft(Digits, '0');
        if (code.Length > Digits) yield break;
        for (var i = 0; i < Digits; i++)
        {
            yield return $"d{i}-{code[i]}";
        }
    }
}

/// <summary>One grid card. <see cref="OnClick"/> runs on the main thread from OnCustomHudClicked.</summary>
public sealed record PanelCard(
    string Name,
    string Sub,
    PanelImage? Image,
    int Rarity,
    bool Equipped,
    Action<IPlayer> OnClick);

public sealed class PanelState
{
    public required int PlayerId { get; init; }

    public PanelView View { get; set; } = PanelView.Home;
    public PanelCategory Category { get; set; } = PanelCategory.Home;
    public TeamChoice Team { get; set; } = TeamChoice.Both;
    public int Page { get; set; }

    /// <summary>EconService.Items key of the weapon / knife / glove being browsed.</summary>
    public string? ItemName { get; set; }

    public PaintkitDefinition? Paintkit { get; set; }
    public float Wear { get; set; }
    public int Seed { get; set; }
    public bool StatTrak { get; set; }

    /// <summary>Transient status line shown under the title until the next navigation.</summary>
    public string? Notice { get; set; }

    /// <summary>Cards of the grid as last rendered, so a click maps back to the same entry.</summary>
    public List<PanelCard> Cards { get; set; } = [];

    /// <summary>Home cards as last rendered: index 0..7 matches sk_home_0..7.</summary>
    public Action<IPlayer>?[] HomeActions { get; } = new Action<IPlayer>?[8];

    /// <summary>What this player's client currently shows; outlives the state across re-opens.</summary>
    public required PanelClientCache Client { get; init; }

    public IEnumerable<SwiftlyS2.Shared.Players.Team> TargetTeams => Team switch
    {
        TeamChoice.T => [SwiftlyS2.Shared.Players.Team.T],
        TeamChoice.CT => [SwiftlyS2.Shared.Players.Team.CT],
        _ => [SwiftlyS2.Shared.Players.Team.T, SwiftlyS2.Shared.Players.Team.CT]
    };
}

/// <summary>
/// The dialog variables and class overrides the HUD entity holds for one player. These persist on
/// the entity between opens, so the cache lives as long as the entity and the player do; a class
/// with no entry is still at its jk_skins.xml default.
/// </summary>
public sealed class PanelClientCache
{
    public Dictionary<string, string> Vars { get; } = new();
    public Dictionary<(string Panel, string Class), bool> Classes { get; } = new();

    /// <summary>Image classes currently on each jk-sk-img panel, so a change can clear the old ones.</summary>
    public Dictionary<string, PanelImage?> Images { get; } = new();
}
