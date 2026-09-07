using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

using WeaponSkins.Configuration;

namespace WeaponSkins;

public partial class CommandService : IDisposable
{
    private const string CommandName = "ws";
    private const string CommandHelpText = "Opens the weapon skins menu.";

    private ISwiftlyCore Core { get; init; }
    private ILogger Logger { get; init; }
    private MenuService MenuService { get; init; }

    private readonly IDisposable? _configChangeSubscription;
    private Guid? _commandGuid;
    private string _permission;

    public CommandService(ISwiftlyCore core,
        ILogger<CommandService> logger,
        MenuService menuService,
        IOptionsMonitor<MainConfigModel> options)
    {
        Core = core;
        Logger = logger;
        MenuService = menuService;

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
            Logger.LogInformation("Re-registered command with permission {Permission}.",
                permission.Length == 0 ? "<none>" : permission);
        });
    }

    public void RegisterCommands()
    {
        _commandGuid = Core.Command.RegisterCommand(
            CommandName,
            CommandSkin,
            permission: _permission,
            helpText: CommandHelpText);
    }

    private void UnregisterCommands()
    {
        if (_commandGuid is { } guid)
        {
            Core.Command.UnregisterCommand(guid);
            _commandGuid = null;
        }
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

    public void Dispose()
    {
        _configChangeSubscription?.Dispose();
        UnregisterCommands();
        GC.SuppressFinalize(this);
    }
}
