using SwiftlyS2.Shared.Players;

using WeaponSkins.Shared;

namespace WeaponSkins;

public partial class PanelService
{
    /// <summary>Weapon definition index to sidebar category; anything unlisted lands in Heavy.</summary>
    private static readonly Dictionary<int, PanelCategory> WeaponCategories = new()
    {
        // Pistols
        [1] = PanelCategory.Pistols, [2] = PanelCategory.Pistols, [3] = PanelCategory.Pistols,
        [4] = PanelCategory.Pistols, [30] = PanelCategory.Pistols, [32] = PanelCategory.Pistols,
        [36] = PanelCategory.Pistols, [61] = PanelCategory.Pistols, [63] = PanelCategory.Pistols,
        [64] = PanelCategory.Pistols,
        // Rifles
        [7] = PanelCategory.Rifles, [8] = PanelCategory.Rifles, [10] = PanelCategory.Rifles,
        [13] = PanelCategory.Rifles, [16] = PanelCategory.Rifles, [39] = PanelCategory.Rifles,
        [60] = PanelCategory.Rifles,
        // Snipers
        [9] = PanelCategory.Snipers, [11] = PanelCategory.Snipers, [38] = PanelCategory.Snipers,
        [40] = PanelCategory.Snipers,
        // SMGs
        [17] = PanelCategory.Smgs, [19] = PanelCategory.Smgs, [23] = PanelCategory.Smgs,
        [24] = PanelCategory.Smgs, [26] = PanelCategory.Smgs, [33] = PanelCategory.Smgs,
        [34] = PanelCategory.Smgs,
    };

    private static PanelCategory GetWeaponCategory(int definitionIndex) =>
        WeaponCategories.GetValueOrDefault(definitionIndex, PanelCategory.Heavy);

    private static bool IsWeaponCategory(PanelCategory category) =>
        category is PanelCategory.Rifles or PanelCategory.Snipers or PanelCategory.Pistols
            or PanelCategory.Smgs or PanelCategory.Heavy;

    /// <summary>Categories whose grid starts at a list of item types rather than finishes.</summary>
    private static bool HasTypes(PanelCategory category) =>
        category is PanelCategory.Knives or PanelCategory.Gloves || IsWeaponCategory(category);

    private bool CanUse(IPlayer player,
        PanelCategory category) => category switch
    {
        PanelCategory.Home => true,
        PanelCategory.Knives => ItemPermissionService.CanUseKnifeSkins(player.SteamID),
        PanelCategory.Gloves => ItemPermissionService.CanUseGloveSkins(player.SteamID),
        PanelCategory.Agents => ItemPermissionService.CanUseAgents(player.SteamID),
        PanelCategory.Music => ItemPermissionService.CanUseMusicKits(player.SteamID),
        _ => ItemPermissionService.CanUseWeaponSkins(player.SteamID)
    };

    /// <summary>The team whose saved items the grid marks as equipped and the detail view starts from.</summary>
    private static Team ReferenceTeam(IPlayer player,
        PanelState state) => state.Team switch
    {
        TeamChoice.T => Team.T,
        TeamChoice.CT => Team.CT,
        _ => player.Controller.Team == Team.CT ? Team.CT : Team.T
    };

    private string Localized(Dictionary<string, string> names,
        IPlayer player,
        string fallback)
    {
        var name = EconService.GetLocalizedName(names, player.PlayerLanguage.Value);
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private string ItemName(ItemDefinition item,
        IPlayer player) => Localized(item.LocalizedNames, player, item.Name);

    private string PaintkitName(PaintkitDefinition paintkit,
        IPlayer player) => Localized(paintkit.LocalizedNames, player, paintkit.Name);

    private Dictionary<int, ItemDefinition>? _itemsByIndex;

    private ItemDefinition? FindItem(int definitionIndex)
    {
        _itemsByIndex ??= EconService.Items.Values
            .GroupBy(i => i.Index)
            .ToDictionary(g => g.Key, g => g.First());
        return _itemsByIndex.GetValueOrDefault(definitionIndex);
    }

    private PaintkitDefinition? FindPaintkit(string itemName,
        int paintkitIndex)
    {
        if (paintkitIndex == 0) return null;
        return EconService.WeaponToPaintkits.TryGetValue(itemName, out var paintkits)
            ? paintkits.FirstOrDefault(p => p.Index == paintkitIndex)
            : null;
    }

    /// <summary>
    /// The game's own econ icon for an item with a finish; paintkit 0 is the base weapon icon
    /// (gloves have none, so that combination simply has no CSS rule).
    /// </summary>
    private static PanelImage FinishImage(ItemDefinition item,
        PaintkitDefinition? paintkit) => new(item.Index.ToString(), paintkit?.Index ?? 0);

    // ---- What the player has saved ------------------------------------------------------------

    private sealed record Equipped(ItemDefinition Item, PaintkitDefinition? Paintkit);

    private Equipped? GetEquippedWeapon(IPlayer player,
        Team team,
        ItemDefinition item)
    {
        if (!Api.TryGetWeaponSkin(player.SteamID, team, (ushort)item.Index, out var skin)) return null;
        return new Equipped(item, FindPaintkit(item.Name, skin.Paintkit));
    }

    private Equipped? GetEquippedKnife(IPlayer player,
        Team team)
    {
        if (!Api.TryGetKnifeSkin(player.SteamID, team, out var knife)) return null;
        var item = FindItem(knife.DefinitionIndex);
        return item is null ? null : new Equipped(item, FindPaintkit(item.Name, knife.Paintkit));
    }

    private Equipped? GetEquippedGlove(IPlayer player,
        Team team)
    {
        if (!Api.TryGetGloveSkin(player.SteamID, team, out var glove)) return null;
        var item = FindItem(glove.DefinitionIndex);
        return item is null ? null : new Equipped(item, FindPaintkit(item.Name, glove.Paintkit));
    }

    private AgentDefinition? GetEquippedAgent(IPlayer player,
        Team team)
    {
        if (!Api.TryGetAgentSkin(player.SteamID, team, out var index)) return null;
        return EconService.Agents.Values.FirstOrDefault(a => a.Index == index);
    }

    private MusicKitDefinition? GetEquippedMusicKit(IPlayer player)
    {
        if (!DataService.MusicKitDataService.TryGetMusicKit(player.SteamID, out var index)) return null;
        return EconService.MusicKits.Values.FirstOrDefault(m => m.Index == index);
    }

    private static bool IsAgentForTeam(AgentDefinition agent,
        Team team)
    {
        // Same rule as the native agent menu: CT models are ctm_*, T models are tm_*.
        var prefix = team == Team.CT ? "ctm_" : "tm_";
        return agent.ModelPath.Contains($"/{prefix}", StringComparison.OrdinalIgnoreCase) ||
               agent.ModelPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Grid contents ------------------------------------------------------------------------

    private List<PanelCard> BuildCards(IPlayer player,
        PanelState state)
    {
        return state.View switch
        {
            PanelView.Types => BuildTypeCards(player, state),
            PanelView.Finishes when state.Category == PanelCategory.Agents => BuildAgentCards(player, state),
            PanelView.Finishes when state.Category == PanelCategory.Music => BuildMusicKitCards(player),
            PanelView.Finishes => BuildFinishCards(player, state),
            _ => []
        };
    }

    private IEnumerable<ItemDefinition> ItemsInCategory(PanelCategory category)
    {
        foreach (var name in EconService.WeaponToPaintkits.Keys)
        {
            if (!EconService.Items.TryGetValue(name, out var item)) continue;

            var matches = category switch
            {
                PanelCategory.Knives => Utilities.IsKnifeDefinitionIndex(item.Index),
                PanelCategory.Gloves => Utilities.IsGloveDefinitionIndex(item.Index),
                _ => Utilities.IsWeaponDefinitionIndex(item.Index) && GetWeaponCategory(item.Index) == category
            };

            if (matches) yield return item;
        }
    }

    private List<PanelCard> BuildTypeCards(IPlayer player,
        PanelState state)
    {
        var team = ReferenceTeam(player, state);
        var loc = LocalizationService[player];

        var items = ItemsInCategory(state.Category);
        items = IsWeaponCategory(state.Category)
            ? items.OrderBy(i => i.Index)
            : items.OrderBy(i => ItemName(i, player));

        var cards = new List<PanelCard>();
        foreach (var item in items)
        {
            Equipped? current = state.Category switch
            {
                PanelCategory.Knives => GetEquippedKnife(player, team) is { } k && k.Item.Index == item.Index ? k : null,
                PanelCategory.Gloves => GetEquippedGlove(player, team) is { } g && g.Item.Index == item.Index ? g : null,
                _ => GetEquippedWeapon(player, team, item)
            };

            var sub = current?.Paintkit is { } pk ? PaintkitName(pk, player) : loc.PanelDefault;
            // Gloves have no unpainted icon, so an unowned glove type shows its first finish.
            var image = current is null && state.Category == PanelCategory.Gloves
                ? FinishImage(item, EconService.WeaponToPaintkits[item.Name].FirstOrDefault())
                : FinishImage(item, current?.Paintkit);

            var itemName = item.Name;
            cards.Add(new PanelCard(
                ItemName(item, player),
                sub,
                image,
                current?.Paintkit?.Rarity.Id ?? 0,
                current is not null && (current.Paintkit is not null || !IsWeaponCategory(state.Category)),
                _ =>
                {
                    state.ItemName = itemName;
                    state.View = PanelView.Finishes;
                    state.Page = 0;
                }));
        }

        return cards;
    }

    private List<PanelCard> BuildFinishCards(IPlayer player,
        PanelState state)
    {
        if (state.ItemName is null || !EconService.Items.TryGetValue(state.ItemName, out var item)) return [];

        var team = ReferenceTeam(player, state);
        var loc = LocalizationService[player];
        var itemTitle = ItemName(item, player);

        Equipped? current = state.Category switch
        {
            PanelCategory.Knives => GetEquippedKnife(player, team) is { } k && k.Item.Index == item.Index ? k : null,
            PanelCategory.Gloves => GetEquippedGlove(player, team) is { } g && g.Item.Index == item.Index ? g : null,
            _ => GetEquippedWeapon(player, team, item)
        };

        var cards = new List<PanelCard>();

        // No-finish entry: a weapon's default skin, or a knife with no paint. Gloves always need one.
        if (state.Category != PanelCategory.Gloves)
        {
            cards.Add(new PanelCard(
                state.Category == PanelCategory.Knives ? loc.PanelVanilla : loc.PanelDefault,
                itemTitle,
                FinishImage(item, null),
                0,
                current is not null && current.Paintkit is null && state.Category == PanelCategory.Knives
                    || current is null && state.Category != PanelCategory.Knives,
                p =>
                {
                    foreach (var t in state.TargetTeams)
                    {
                        if (state.Category == PanelCategory.Knives)
                        {
                            Api.UpdateKnifeSkin(p.SteamID, t, knife =>
                            {
                                knife.DefinitionIndex = (ushort)item.Index;
                                knife.Paintkit = 0;
                            }, true);
                        }
                        else
                        {
                            Api.ResetWeaponSkin(p.SteamID, t, (ushort)item.Index, true);
                        }
                    }

                    state.Notice = LocalizationService[p].PanelSaved(TeamLabel(p, state));
                }));
        }

        var paintkits = EconService.WeaponToPaintkits[item.Name]
            .OrderByDescending(p => p.Rarity.Id)
            .ThenBy(p => PaintkitName(p, player));

        foreach (var paintkit in paintkits)
        {
            cards.Add(new PanelCard(
                PaintkitName(paintkit, player),
                itemTitle,
                FinishImage(item, paintkit),
                paintkit.Rarity.Id,
                current?.Paintkit?.Index == paintkit.Index,
                p => OpenDetail(p, state, item, paintkit)));
        }

        return cards;
    }

    private List<PanelCard> BuildAgentCards(IPlayer player,
        PanelState state)
    {
        var cards = new List<PanelCard>();
        foreach (var team in state.TargetTeams)
        {
            var equipped = GetEquippedAgent(player, team);
            var teamLabel = team == Team.CT ? "CT" : "T";

            var agents = EconService.Agents.Values
                .Where(a => IsAgentForTeam(a, team))
                .OrderByDescending(a => a.Rarity.Id)
                .ThenBy(a => Localized(a.LocalizedNames, player, a.Name));

            foreach (var agent in agents)
            {
                var agentTeam = team;
                cards.Add(new PanelCard(
                    Localized(agent.LocalizedNames, player, agent.Name),
                    teamLabel,
                    PanelImage.Agent(agent.Index),
                    agent.Rarity.Id,
                    equipped?.Index == agent.Index,
                    p =>
                    {
                        MenuService.SelectAgent(p, agentTeam, agent);
                        state.Notice = LocalizationService[p].PanelSaved(teamLabel);
                    }));
            }
        }

        return cards;
    }

    private List<PanelCard> BuildMusicKitCards(IPlayer player)
    {
        var equipped = GetEquippedMusicKit(player);
        var label = LocalizationService[player].PanelMusicKit;

        return EconService.MusicKits.Values
            .OrderBy(m => m.Index)
            .Select(kit => new PanelCard(
                Localized(kit.LocalizedNames, player, kit.Name),
                label,
                PanelImage.MusicKit(kit.Index),
                kit.Rarity.Id,
                equipped?.Index == kit.Index,
                p => Api.SetMusicKit(p.SteamID, kit.Index)))
            .ToList();
    }

    private string TeamLabel(IPlayer player,
        PanelState state) => state.Team switch
    {
        TeamChoice.T => "T",
        TeamChoice.CT => "CT",
        _ => LocalizationService[player].PanelBothTeams
    };
}
