using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

using WeaponSkins.Extensions;
using WeaponSkins.Shared;

namespace WeaponSkins;

public partial class MenuService
{
    public IMenuOption GetAgentMenuSubmenuOption(IPlayer player)
    {
        if (!ItemPermissionService.CanUseAgents(player.SteamID))
        {
            return CreateDisabledOption(LocalizationService[player].MenuTitleAgents);
        }

        var option = new SubmenuMenuOption(LocalizationService[player].MenuTitleAgents, () => Task.FromResult(BuildAgentMenu(player)));
        return option;
    }

    public IMenuAPI BuildAgentMenu(IPlayer player)
    {
        var main = Core.MenusAPI.CreateBuilder();
        main.Design.SetMenuTitle(LocalizationService[player].MenuTitleAgents);

        var reset = new ButtonMenuOption(LocalizationService[player].MenuReset);
        reset.Click += (_, args) =>
        {
            ResetAgent(args.Player, args.Player.Controller.Team);
            return ValueTask.CompletedTask;
        };
        main.AddOption(reset);

        var team = player.Controller.Team;
        
        // Filter agents by team: CT agents start with "ctm_", T agents start with "tm_"
        // Team 3 = CT, Team 2 = T
        var teamPrefix = (int)team == 3 ? "ctm_" : "tm_";
        var agents = EconService.Agents.Values
            .Where(a => a.ModelPath.Contains($"/{teamPrefix}", StringComparison.OrdinalIgnoreCase) || 
                       a.ModelPath.StartsWith(teamPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Rarity.Id)
            .ThenBy(a => EconService.GetLocalizedName(a.LocalizedNames, player.PlayerLanguage.Value))
            .ToList();

        foreach (var agent in agents)
        {
            var title = EconService.GetLocalizedName(agent.LocalizedNames, player.PlayerLanguage.Value);
            var coloredTitle = HtmlGradient.GenerateGradientText(title, agent.Rarity.Color.HexColor);
            var option = new ButtonMenuOption(coloredTitle);
            option.Click += (_, args) =>
            {
                SelectAgent(args.Player, args.Player.Controller.Team, agent);
                return ValueTask.CompletedTask;
            };

            main.AddOption(option);
        }

        return main.Build();
    }

    /// <summary>
    /// Saves <paramref name="agent"/> for <paramref name="team"/>. The model is only swapped live
    /// when the player is currently on that team; otherwise it applies on their next spawn there.
    /// </summary>
    public void SelectAgent(IPlayer player,
        Team team,
        AgentDefinition agent)
    {
        var onTeam = player.Controller.Team == team;

        if (onTeam && player.IsAlive())
        {
            var pawn = player.PlayerPawn!;
            var current = pawn.CBodyComponent!.SceneNode!.GetSkeletonInstance()
                .ModelState
                .ModelName;
            DataService.AgentDataService.CaptureDefaultModel(player.SteamID, team, current);
        }

        DataService.AgentDataService.SetAgent(player.SteamID, team, agent.Index);
        Api.UpdateAgentSkin(player.SteamID, team, agent.Index, true);

        if (!onTeam) return;

        ApplyAgentModel(player, agent.ModelPath);
        Core.Scheduler.DelayBySeconds(0.1f, () =>
        {
            ApplyAgentModel(player, agent.ModelPath);
        });
    }

    public void ResetAgent(IPlayer player,
        Team team)
    {
        DataService.AgentDataService.TryRemoveAgent(player.SteamID, team);
        if (player.Controller.Team == team &&
            DataService.AgentDataService.TryGetDefaultModel(player.SteamID, team, out var defaultModel))
        {
            ApplyAgentModel(player, defaultModel);
        }

        Api.ResetAgentSkin(player.SteamID, team, true);
    }

    private string? GetRefreshModel(string currentModel,
        string targetModel)
    {
        foreach (var agent in EconService.Agents.Values)
        {
            var candidate = agent.ModelPath;
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (string.Equals(candidate, currentModel, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(candidate, targetModel, StringComparison.OrdinalIgnoreCase)) continue;
            return candidate;
        }

        return null;
    }

    private void ApplyAgentModel(IPlayer player,
        string modelPath)
    {
        if (!player.IsAlive()) return;

        Core.Scheduler.NextTick(() =>
        {
            if (!player.IsAlive()) return;
            var pawn = player.PlayerPawn!;

            var current = pawn.CBodyComponent!.SceneNode!.GetSkeletonInstance()
                .ModelState
                .ModelName;

            var refreshModel = GetRefreshModel(current, modelPath);
            if (!string.IsNullOrWhiteSpace(refreshModel))
            {
                pawn.SetModel(refreshModel);
                pawn.SetModel(current);
            }

            Core.Scheduler.NextTick(() =>
            {
                if (!player.IsAlive()) return;
                pawn.SetModel(modelPath);
            });
        });
    }
}
