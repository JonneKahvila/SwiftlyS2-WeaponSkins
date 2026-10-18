using Microsoft.Extensions.Logging;

using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

using WeaponSkins.Shared;

namespace WeaponSkins;

public partial class PanelService
{
    private void OnCustomHudClicked(IOnCustomHudClickedEvent @event)
    {
        try
        {
            var hud = _hud;
            if (hud is null || @event.CustomHudLayout?.Address != hud.Address) return;

            // No open state means a stale click (panel closed, map changed) or one from a player
            // who never opened it; either way there is nothing to act on.
            if (!_states.TryGetValue(@event.PlayerId, out var state)) return;

            var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
            if (player is null || !player.IsValid) return;

            if (!HandleClick(player, state, @event.ButtonId)) return;

            // A handler may have closed the panel (stickers / charm hand-off).
            if (_states.ContainsKey(player.PlayerID))
            {
                Render(hud, player, state);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to handle a skin panel click.");
        }
    }

    /// <returns>True when the panel should re-render.</returns>
    private bool HandleClick(IPlayer player,
        PanelState state,
        string buttonId)
    {
        var loc = LocalizationService[player];

        switch (buttonId)
        {
            case "sk_close":
                Close(player);
                return false;

            case "sk_team_t":
            case "sk_team_ct":
            case "sk_team_both":
                state.Team = buttonId switch
                {
                    "sk_team_t" => TeamChoice.T,
                    "sk_team_ct" => TeamChoice.CT,
                    _ => TeamChoice.Both
                };
                state.Notice = null;
                if (state.Category == PanelCategory.Agents) state.Page = 0;
                return true;

            case "sk_home_music":
                OpenCategoryFor(state, PanelCategory.Music, state.Team);
                return true;

            case "sk_back":
            case "sk_d_back":
                GoBack(state);
                return true;

            case "sk_prev":
                state.Page--;
                return true;

            case "sk_next":
                state.Page++;
                return true;

            case "sk_reset":
                Reset(player, state);
                state.Notice = loc.PanelResetDone(TeamLabel(player, state));
                return true;

            case "sk_seed_m100": state.Seed = Math.Clamp(state.Seed - 100, 0, 1000); return true;
            case "sk_seed_m1": state.Seed = Math.Clamp(state.Seed - 1, 0, 1000); return true;
            case "sk_seed_p1": state.Seed = Math.Clamp(state.Seed + 1, 0, 1000); return true;
            case "sk_seed_p100": state.Seed = Math.Clamp(state.Seed + 100, 0, 1000); return true;
            case "sk_seed_rand": state.Seed = Random.Shared.Next(0, 1001); return true;

            case "sk_st_off": state.StatTrak = false; return true;
            case "sk_st_on": state.StatTrak = true; return true;

            case "sk_equip":
                Equip(player, state);
                state.Notice = loc.PanelSaved(TeamLabel(player, state));
                state.View = PanelView.Finishes;
                return true;

            case "sk_stickers":
            case "sk_charm":
                Close(player);
                // The native sticker / charm menus edit the weapon in hand, so they open after the
                // panel has released the cursor.
                Core.Scheduler.NextTick(() =>
                {
                    if (!player.IsValid) return;
                    if (buttonId == "sk_stickers") MenuService.OpenStickerMenu(player);
                    else MenuService.OpenKeychainMenu(player);
                });
                return false;
        }

        if (buttonId.StartsWith("sk_nav_", StringComparison.Ordinal))
        {
            if (!Enum.TryParse<PanelCategory>(buttonId["sk_nav_".Length..], true, out var category)) return false;
            if (!CanUse(player, category))
            {
                state.Notice = loc.PanelNoPermission;
                return true;
            }

            if (_hud is { } hud) Navigate(hud, player, state, category);
            return false;
        }

        if (TrySlot(buttonId, "sk_home_", 8, out var home))
        {
            state.HomeActions[home]?.Invoke(player);
            return true;
        }

        if (TrySlot(buttonId, "sk_card_", GridPageSize, out var slot))
        {
            var index = state.Page * GridPageSize + slot;
            if (index < 0 || index >= state.Cards.Count) return false;

            state.Notice = null;
            state.Cards[index].OnClick(player);
            return true;
        }

        if (TrySlot(buttonId, "sk_wear_", WearPresets.Length, out var wear))
        {
            state.Wear = WearPresets[wear];
            return true;
        }

        return false;
    }

    private static bool TrySlot(string buttonId,
        string prefix,
        int count,
        out int slot)
    {
        slot = -1;
        return buttonId.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(buttonId.AsSpan(prefix.Length), out slot) &&
               slot >= 0 && slot < count;
    }

    private static void GoBack(PanelState state)
    {
        state.Notice = null;
        switch (state.View)
        {
            case PanelView.Detail:
                state.View = PanelView.Finishes;
                state.Paintkit = null;
                break;

            case PanelView.Finishes when HasTypes(state.Category):
                state.View = PanelView.Types;
                state.ItemName = null;
                state.Page = 0;
                break;

            default:
                state.View = PanelView.Home;
                state.Category = PanelCategory.Home;
                state.ItemName = null;
                state.Page = 0;
                break;
        }
    }

    private void OpenDetail(IPlayer player,
        PanelState state,
        ItemDefinition item,
        PaintkitDefinition paintkit)
    {
        state.ItemName = item.Name;
        state.Paintkit = paintkit;
        state.View = PanelView.Detail;

        // Start from what the player already has on this item, so re-picking a finish keeps
        // their float/seed/StatTrak instead of silently resetting them.
        var team = ReferenceTeam(player, state);
        state.Wear = WearPresets[0];
        state.Seed = 0;
        state.StatTrak = false;

        switch (state.Category)
        {
            case PanelCategory.Knives when Api.TryGetKnifeSkin(player.SteamID, team, out var knife) &&
                                           knife.DefinitionIndex == item.Index:
                state.Wear = knife.PaintkitWear;
                state.Seed = knife.PaintkitSeed;
                state.StatTrak = knife.Quality == EconItemQuality.StatTrak;
                break;

            case PanelCategory.Gloves when Api.TryGetGloveSkin(player.SteamID, team, out var glove) &&
                                           glove.DefinitionIndex == item.Index:
                state.Wear = glove.PaintkitWear;
                state.Seed = glove.PaintkitSeed;
                break;

            case var c when IsWeaponCategory(c) &&
                            Api.TryGetWeaponSkin(player.SteamID, team, (ushort)item.Index, out var skin):
                state.Wear = skin.PaintkitWear;
                state.Seed = skin.PaintkitSeed;
                state.StatTrak = skin.Quality == EconItemQuality.StatTrak;
                break;
        }
    }

    private static EconItemQuality WithStatTrak(EconItemQuality quality,
        bool statTrak) =>
        statTrak ? EconItemQuality.StatTrak
        : quality == EconItemQuality.StatTrak ? EconItemQuality.Normal
        : quality;

    private void Equip(IPlayer player,
        PanelState state)
    {
        if (state.ItemName is null || state.Paintkit is null ||
            !EconService.Items.TryGetValue(state.ItemName, out var item)) return;

        var definitionIndex = (ushort)item.Index;
        var paintkit = state.Paintkit.Index;
        var wear = state.Wear;
        var seed = state.Seed;
        var statTrak = state.StatTrak;

        foreach (var team in state.TargetTeams)
        {
            switch (state.Category)
            {
                case PanelCategory.Knives:
                    Api.UpdateKnifeSkin(player.SteamID, team, knife =>
                    {
                        knife.DefinitionIndex = definitionIndex;
                        knife.Paintkit = paintkit;
                        knife.PaintkitWear = wear;
                        knife.PaintkitSeed = seed;
                        knife.Quality = WithStatTrak(knife.Quality, statTrak);
                    }, true);
                    break;

                case PanelCategory.Gloves:
                    Api.UpdateGloveSkin(player.SteamID, team, glove =>
                    {
                        glove.DefinitionIndex = definitionIndex;
                        glove.Paintkit = paintkit;
                        glove.PaintkitWear = wear;
                        glove.PaintkitSeed = seed;
                    }, true);
                    break;

                default:
                    Api.UpdateWeaponSkin(player.SteamID, team, definitionIndex, skin =>
                    {
                        skin.Paintkit = paintkit;
                        skin.PaintkitWear = wear;
                        skin.PaintkitSeed = seed;
                        skin.Quality = WithStatTrak(skin.Quality, statTrak);
                    }, true);
                    break;
            }
        }
    }

    /// <summary>Footer Reset: clears whatever the current grid is about, for the selected teams.</summary>
    private void Reset(IPlayer player,
        PanelState state)
    {
        foreach (var team in state.TargetTeams)
        {
            switch (state.Category)
            {
                case PanelCategory.Knives:
                    Api.ResetKnifeSkin(player.SteamID, team, true);
                    break;
                case PanelCategory.Gloves:
                    Api.ResetGloveSkin(player.SteamID, team, true);
                    break;
                case PanelCategory.Agents:
                    MenuService.ResetAgent(player, team);
                    break;
                case PanelCategory.Music:
                    break;
                default:
                    if (state.ItemName is not null && EconService.Items.TryGetValue(state.ItemName, out var item))
                    {
                        Api.ResetWeaponSkin(player.SteamID, team, (ushort)item.Index, true);
                    }
                    break;
            }
        }

        // Music kits aren't per team.
        if (state.Category == PanelCategory.Music)
        {
            Api.ResetMusicKit(player.SteamID);
        }
    }
}
