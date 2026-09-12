using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

using WeaponSkins.Configuration;
using WeaponSkins.Services;

namespace WeaponSkins;

public partial class CommandService : IDisposable
{
    private ISwiftlyCore Core { get; init; }
    private ILogger Logger { get; init; }
    private MenuService MenuService { get; init; }
    private ItemPermissionService ItemPermissionService { get; init; }

    private readonly IDisposable? _configChangeSubscription;
    private readonly List<Guid> _commandGuids = new();
    private string _permission;

    public CommandService(ISwiftlyCore core,
        ILogger<CommandService> logger,
        MenuService menuService,
        ItemPermissionService itemPermissionService,
        IOptionsMonitor<MainConfigModel> options)
    {
        Core = core;
        Logger = logger;
        MenuService = menuService;
        ItemPermissionService = itemPermissionService;

        _permission = options.CurrentValue.MenuPermission ?? "";
        RegisterCommands();

        // The permission is configurable, so the command has to be re-registered when it changes.
        _configChangeSubscription = options.OnChange(config =>
        {
            var permission = config.MenuPermission ?? "";
            if (permission == _permission)
            {
                return;
            }

            _permission = permission;
            UnregisterCommands();
            RegisterCommands();
            Logger.LogInformation("Re-registered commands with permission {Permission}.",
                permission.Length == 0 ? "<none>" : permission);
        });
    }

    public void RegisterCommands()
    {
        RegisterCommand("ws", CommandSkin, "Opens the main weapon skins menu.");

        RegisterCommand("knife", CommandKnife, "Opens the knife skins menu.");
        RegisterCommand("knives", CommandKnife, "Opens the knife skins menu.");

        RegisterCommand("gloves", CommandGlove, "Opens the glove skins menu.");
        RegisterCommand("glove", CommandGlove, "Opens the glove skins menu.");

        RegisterCommand("agents", CommandAgent, "Opens the agent skins menu.");
        RegisterCommand("agent", CommandAgent, "Opens the agent skins menu.");

        RegisterCommand("keychain", CommandKeychain, "Opens the weapon keychains/charms menu.");
        RegisterCommand("keychains", CommandKeychain, "Opens the weapon keychains/charms menu.");
        RegisterCommand("charm", CommandKeychain, "Opens the weapon keychains/charms menu.");
        RegisterCommand("charms", CommandKeychain, "Opens the weapon keychains/charms menu.");

        RegisterCommand("music", CommandMusic, "Opens the music kits menu.");
        RegisterCommand("musickit", CommandMusic, "Opens the music kits menu.");

        RegisterCommand("skins", CommandWeaponSkin, "Opens the weapon skins menu.");
        RegisterCommand("skin", CommandWeaponSkin, "Opens the weapon skins menu.");
        RegisterCommand("weapon", CommandWeaponSkin, "Opens the weapon skins menu.");
        RegisterCommand("weapons", CommandWeaponSkin, "Opens the weapon skins menu.");
    }

    private void RegisterCommand(string name, ICommandService.CommandListener handler, string helpText)
    {
        var guid = Core.Command.RegisterCommand(name, handler, permission: _permission, helpText: helpText);
        _commandGuids.Add(guid);
    }

    private void UnregisterCommands()
    {
        foreach (var guid in _commandGuids)
        {
            Core.Command.UnregisterCommand(guid);
        }
        _commandGuids.Clear();
    }

    private void CommandSkin(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        MenuService.OpenMainMenu(context.Sender!);
    }

    private void CommandKnife(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseKnifeSkins(player.SteamID))
        {
            context.Reply("You do not have permission to use knife skins.");
            return;
        }

        MenuService.OpenKnifeMenu(player);
    }

    private void CommandGlove(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseGloveSkins(player.SteamID))
        {
            context.Reply("You do not have permission to use glove skins.");
            return;
        }

        MenuService.OpenGloveMenu(player);
    }

    private void CommandAgent(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseAgents(player.SteamID))
        {
            context.Reply("You do not have permission to use agent skins.");
            return;
        }

        MenuService.OpenAgentMenu(player);
    }

    private void CommandKeychain(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseKeychains(player.SteamID))
        {
            context.Reply("You do not have permission to use keychains.");
            return;
        }

        MenuService.OpenKeychainMenu(player);
    }

    private void CommandMusic(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseMusicKits(player.SteamID))
        {
            context.Reply("You do not have permission to use music kits.");
            return;
        }

        MenuService.OpenMusicKitMenu(player);
    }

    private void CommandWeaponSkin(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (!ItemPermissionService.CanUseWeaponSkins(player.SteamID))
        {
            context.Reply("You do not have permission to use weapon skins.");
            return;
        }

        MenuService.OpenWeaponSkinMenu(player);
    }

    public void Dispose()
    {
        _configChangeSubscription?.Dispose();
        UnregisterCommands();
        GC.SuppressFinalize(this);
    }
}
