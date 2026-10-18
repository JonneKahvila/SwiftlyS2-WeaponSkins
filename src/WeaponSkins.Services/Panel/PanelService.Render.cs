using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

using WeaponSkins.Shared;

namespace WeaponSkins;

public partial class PanelService
{
    public const int GridPageSize = 12;

    private static readonly int[] RarityIds = [0, 1, 2, 3, 4, 5, 6, 7, 99];

    /// <summary>Best float of each wear tier: FN, MW, FT, WW, BS.</summary>
    private static readonly float[] WearPresets = [0.01f, 0.08f, 0.16f, 0.39f, 0.46f];

    private static readonly string[] WearNames = ["FN", "MW", "FT", "WW", "BS"];

    private static int WearTier(float wear) => wear switch
    {
        < 0.07f => 0,
        < 0.15f => 1,
        < 0.38f => 2,
        < 0.45f => 3,
        _ => 4
    };

    /// <summary>Text that never changes while the panel is open.</summary>
    private void WriteStaticLabels(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state)
    {
        var loc = LocalizationService[player];

        SetVar(hud, state, "brand", loc.PanelBrand);
        SetVar(hud, state, "lbl_loadout", loc.PanelLoadout);
        SetVar(hud, state, "lbl_both", loc.PanelBoth);
        SetVar(hud, state, "lbl_items", loc.PanelItems);
        SetVar(hud, state, "lbl_music", loc.PanelMusicKit);
        SetVar(hud, state, "lbl_back", loc.PanelBack);
        SetVar(hud, state, "lbl_reset", loc.PanelReset);
        SetVar(hud, state, "lbl_wear", loc.PanelWear);
        SetVar(hud, state, "lbl_seed", loc.PanelSeed);
        SetVar(hud, state, "lbl_random", loc.PanelRandom);
        SetVar(hud, state, "lbl_off", loc.PanelOff);
        SetVar(hud, state, "lbl_on", loc.PanelOn);
        SetVar(hud, state, "lbl_stickers", loc.PanelStickers);
        SetVar(hud, state, "lbl_charm", loc.PanelCharm);
        SetVar(hud, state, "lbl_equip", loc.PanelEquip);
        SetVar(hud, state, "home_t", loc.PanelHomeT);
        SetVar(hud, state, "home_ct", loc.PanelHomeCT);

        foreach (var category in Enum.GetValues<PanelCategory>())
        {
            SetVar(hud, state, $"nav_{NavId(category)}", loc.PanelNav(NavId(category)));
        }
    }

    private static string NavId(PanelCategory category) => category.ToString().ToLowerInvariant();

    private void Navigate(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state,
        PanelCategory category)
    {
        state.Category = category;
        state.Page = 0;
        state.ItemName = null;
        state.Paintkit = null;
        state.Notice = null;
        state.View = category == PanelCategory.Home
            ? PanelView.Home
            : HasTypes(category) ? PanelView.Types : PanelView.Finishes;

        Render(hud, player, state);
    }

    private void Render(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state)
    {
        foreach (var category in Enum.GetValues<PanelCategory>())
        {
            var id = $"sk_nav_{NavId(category)}";
            SetClass(hud, state, id, "is-active", state.Category == category);
            SetClass(hud, state, id, "is-locked", !CanUse(player, category));
        }

        SetClass(hud, state, "sk_team_t", "is-active", state.Team == TeamChoice.T);
        SetClass(hud, state, "sk_team_ct", "is-active", state.Team == TeamChoice.CT);
        SetClass(hud, state, "sk_team_both", "is-active", state.Team == TeamChoice.Both);

        var grid = state.View is PanelView.Types or PanelView.Finishes;
        SetClass(hud, state, "sk_view_home", "is-hidden", state.View != PanelView.Home);
        SetClass(hud, state, "sk_view_grid", "is-hidden", !grid);
        SetClass(hud, state, "sk_view_detail", "is-hidden", state.View != PanelView.Detail);

        switch (state.View)
        {
            case PanelView.Home:
                RenderHome(hud, player, state);
                break;
            case PanelView.Detail:
                RenderDetail(hud, player, state);
                break;
            default:
                RenderGrid(hud, player, state);
                break;
        }
    }

    private static void SetImage(CCSCustomHudLayout hud,
        PanelState state,
        string panelId,
        PanelImage? image)
    {
        var images = state.Client.Images;
        var previous = images.GetValueOrDefault(panelId);
        if (previous == image) return;
        images[panelId] = image;

        var next = image?.Classes().ToHashSet() ?? [];
        if (previous is not null)
        {
            foreach (var className in previous.Classes())
            {
                if (!next.Contains(className)) SetClass(hud, state, panelId, className, false);
            }
        }

        foreach (var className in next)
        {
            SetClass(hud, state, panelId, className, true);
        }
    }

    private static void SetRarity(CCSCustomHudLayout hud,
        PanelState state,
        string panelId,
        int rarity)
    {
        foreach (var id in RarityIds)
        {
            SetClass(hud, state, panelId, $"rar-{id}", id == rarity);
        }
    }

    // ---- Home ---------------------------------------------------------------------------------

    private void RenderHome(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state)
    {
        var loc = LocalizationService[player];
        SetVar(hud, state, "title", loc.PanelHomeTitle);
        SetVar(hud, state, "subtitle", state.Notice ?? loc.PanelHomeSubtitle);

        Team[] sides = [Team.T, Team.CT];
        for (var s = 0; s < sides.Length; s++)
        {
            var team = sides[s];
            var choice = team == Team.T ? TeamChoice.T : TeamChoice.CT;
            var teamLabel = team == Team.T ? "T" : "CT";
            var baseIndex = s * 4;

            var knife = GetEquippedKnife(player, team);
            WriteHomeCard(hud, state, baseIndex,
                knife is null ? loc.PanelDefault : EquippedName(knife, player),
                loc.PanelKnife,
                knife is null ? null : FinishImage(knife.Item, knife.Paintkit),
                knife is null ? 0 : knife.Paintkit?.Rarity.Id ?? 99,
                _ => OpenCategoryFor(state, PanelCategory.Knives, choice));

            var glove = GetEquippedGlove(player, team);
            WriteHomeCard(hud, state, baseIndex + 1,
                glove is null ? loc.PanelDefault : EquippedName(glove, player),
                loc.PanelGloves,
                glove is null ? null : FinishImage(glove.Item, glove.Paintkit),
                glove?.Paintkit?.Rarity.Id ?? 0,
                _ => OpenCategoryFor(state, PanelCategory.Gloves, choice));

            var agent = GetEquippedAgent(player, team);
            WriteHomeCard(hud, state, baseIndex + 2,
                agent is null ? loc.PanelDefault : Localized(agent.LocalizedNames, player, agent.Name),
                loc.PanelAgent(teamLabel),
                agent is null ? null : PanelImage.Agent(agent.Index),
                agent?.Rarity.Id ?? 0,
                _ => OpenCategoryFor(state, PanelCategory.Agents, choice));

            var weapon = HomeWeapon(player, team);
            if (weapon is null)
            {
                WriteHomeCard(hud, state, baseIndex + 3, loc.PanelDefault, loc.PanelWeapon, null, 0, null);
                continue;
            }

            var saved = GetEquippedWeapon(player, team, weapon);
            var weaponName = weapon.Name;
            WriteHomeCard(hud, state, baseIndex + 3,
                saved is null ? ItemName(weapon, player) : EquippedName(saved, player),
                loc.PanelWeapon,
                FinishImage(weapon, saved?.Paintkit),
                saved?.Paintkit?.Rarity.Id ?? 0,
                _ =>
                {
                    OpenCategoryFor(state, GetWeaponCategory(weapon.Index), choice);
                    state.ItemName = weaponName;
                    state.View = PanelView.Finishes;
                });
        }

        var music = GetEquippedMusicKit(player);
        SetVar(hud, state, "home_music", music is null ? loc.PanelDefault : Localized(music.LocalizedNames, player, music.Name));
    }

    private void WriteHomeCard(CCSCustomHudLayout hud,
        PanelState state,
        int index,
        string name,
        string tag,
        PanelImage? image,
        int rarity,
        Action<IPlayer>? onClick)
    {
        SetVar(hud, state, $"h{index}_name", name);
        SetVar(hud, state, $"h{index}_tag", tag);
        SetImage(hud, state, $"sk_home_{index}_img", image);
        SetRarity(hud, state, $"sk_home_{index}", rarity);
        state.HomeActions[index] = onClick;
    }

    private string EquippedName(Equipped equipped,
        IPlayer player) =>
        equipped.Paintkit is null
            ? ItemName(equipped.Item, player)
            : $"{ItemName(equipped.Item, player)} | {PaintkitName(equipped.Paintkit, player)}";

    /// <summary>
    /// The weapon shown on a home column: what the player is holding when that column is their
    /// current team, otherwise the side's signature rifle.
    /// </summary>
    private ItemDefinition? HomeWeapon(IPlayer player,
        Team team)
    {
        if (player.Controller.Team == team)
        {
            ItemDefinitionHeld(player, out var held);
            if (held is not null) return held;
        }

        return EconService.Items.GetValueOrDefault(team == Team.T ? "weapon_ak47" : "weapon_m4a1_silencer");
    }

    private static void OpenCategoryFor(PanelState state,
        PanelCategory category,
        TeamChoice team)
    {
        state.Team = team;
        state.Category = category;
        state.Page = 0;
        state.ItemName = null;
        state.Notice = null;
        state.View = HasTypes(category) ? PanelView.Types : PanelView.Finishes;
    }

    // ---- Grid ---------------------------------------------------------------------------------

    private void RenderGrid(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state)
    {
        var loc = LocalizationService[player];
        var cards = BuildCards(player, state);
        state.Cards = cards;

        var totalPages = Math.Max(1, (cards.Count + GridPageSize - 1) / GridPageSize);
        state.Page = Math.Clamp(state.Page, 0, totalPages - 1);

        var title = state.View == PanelView.Finishes && state.ItemName is not null &&
                    EconService.Items.TryGetValue(state.ItemName, out var item)
            ? ItemName(item, player)
            : loc.PanelNav(NavId(state.Category));
        SetVar(hud, state, "title", title);
        SetVar(hud, state, "subtitle", state.Notice ?? loc.PanelEditing(TeamLabel(player, state), cards.Count));

        for (var i = 0; i < GridPageSize; i++)
        {
            var id = $"sk_card_{i}";
            var index = state.Page * GridPageSize + i;
            if (index >= cards.Count)
            {
                SetClass(hud, state, id, "is-hidden", true);
                continue;
            }

            var card = cards[index];
            SetVar(hud, state, $"c{i}_name", card.Name);
            SetVar(hud, state, $"c{i}_sub", card.Sub);
            SetImage(hud, state, $"{id}_img", card.Image);
            SetRarity(hud, state, id, card.Rarity);
            SetClass(hud, state, id, "is-equipped", card.Equipped);
            SetClass(hud, state, id, "is-hidden", false);
        }

        SetVar(hud, state, "page", loc.PanelPage(state.Page + 1, totalPages));
        SetClass(hud, state, "sk_page", "is-hidden", totalPages <= 1);
        SetClass(hud, state, "sk_prev", "is-hidden", state.Page <= 0);
        SetClass(hud, state, "sk_next", "is-hidden", state.Page >= totalPages - 1);

        // Weapon type lists have nothing single to reset; everything else does.
        var canReset = !(state.View == PanelView.Types && IsWeaponCategory(state.Category));
        SetClass(hud, state, "sk_reset", "is-hidden", !canReset);
    }

    // ---- Detail -------------------------------------------------------------------------------

    private void RenderDetail(CCSCustomHudLayout hud,
        IPlayer player,
        PanelState state)
    {
        var loc = LocalizationService[player];
        if (state.ItemName is null || state.Paintkit is null ||
            !EconService.Items.TryGetValue(state.ItemName, out var item))
        {
            state.View = PanelView.Finishes;
            RenderGrid(hud, player, state);
            return;
        }

        var paintkit = state.Paintkit;
        var isWeapon = IsWeaponCategory(state.Category);
        var isGlove = state.Category == PanelCategory.Gloves;

        SetVar(hud, state, "title", loc.PanelCustomize);
        SetVar(hud, state, "subtitle", state.Notice ?? "");
        SetVar(hud, state, "d_kicker", ItemName(item, player));
        SetVar(hud, state, "d_name", PaintkitName(paintkit, player));
        SetImage(hud, state, "sk_d_img", FinishImage(item, paintkit));
        SetRarity(hud, state, "sk_d_preview", paintkit.Rarity.Id);

        var tier = WearTier(state.Wear);
        SetVar(hud, state, "d_wear", $"{WearNames[tier]} · {state.Wear:0.0000}");
        for (var i = 0; i < WearPresets.Length; i++)
        {
            SetClass(hud, state, $"sk_wear_{i}", "is-active", i == tier);
        }

        SetVar(hud, state, "d_seed", state.Seed.ToString());

        SetClass(hud, state, "sk_st_row", "is-hidden", isGlove);
        SetClass(hud, state, "sk_st_off", "is-active", !state.StatTrak);
        SetClass(hud, state, "sk_st_on", "is-active", state.StatTrak);

        SetVar(hud, state, "d_applies", loc.PanelAppliesTo(TeamLabel(player, state)));

        SetClass(hud, state, "sk_stickers", "is-hidden", !(isWeapon && ItemPermissionService.CanUseStickers(player.SteamID)));
        SetClass(hud, state, "sk_charm", "is-hidden", !(isWeapon && ItemPermissionService.CanUseKeychains(player.SteamID)));
    }
}
