using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;

using WeaponSkins.Configuration;
using WeaponSkins.Injections;
using WeaponSkins.Shared;

namespace WeaponSkins;


[PluginMetadata(
    Id = "WeaponSkins",
#if WORKFLOW
    Version = WORKFLOW_VERSION,
#else
    Version = "Local",
#endif
    Name = "WeaponSkins",
    Author = "samyyc & ELDment",
    Description = "A swiftlys2 plugin to change player's skins."
)]
public partial class WeaponSkins : BasePlugin
{
    /// <summary>
    /// Shared interface key, following the documented <c>PluginName.ServiceName.vX</c> convention.
    /// Bump the version suffix on a breaking change to <see cref="IWeaponSkinAPI"/>.
    /// </summary>
    public const string SharedInterfaceKey = "WeaponSkins.Api.v1";

    /// <summary>Unversioned key this plugin shipped with before <see cref="SharedInterfaceKey"/>.</summary>
    public const string LegacySharedInterfaceKey = "WeaponSkins.API";

    private ServiceProvider _provider = null!;

    public WeaponSkins(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration.InitializeJsonWithModel<MainConfigModel>("config.jsonc", "WeaponSkins")
            .Configure(builder =>
            {
                // Must be the full path from GetConfigPath: a bare filename resolves against the
                // server's working directory, not the plugin folder.
                builder.AddJsonFile(Core.Configuration.GetConfigPath("config.jsonc"), false, true);
            });

        StickerFixService.Initialize();
        var collection = new ServiceCollection()
            .AddSwiftly(Core)
            .AddDataService()
            .AddNativeService()
            .AddInventoryService()
            .AddPlayerService()
            .AddApi()
            .AddEconService()
            .AddMenuService()
            .AddStorageService()
            .AddStattrakService()
            .AddLocalizationService()
            .AddItemPermissionService()
            .AddCommandService();


        collection
            .AddOptionsWithValidateOnStart<MainConfigModel>()
            .BindConfiguration("WeaponSkins");

        _provider = collection.BuildServiceProvider();

        _provider
            .UseDataService()
            .UseNativeService()
            .UseInventoryService()
            .UsePlayerService()
            .UseApi()
            .UseEconService()
            .UseMenuService()
            .UseStorageService()
            .UseStattrakService()
            .UseLocalizationService()
            .UseItemPermissionService()
            .UseCommandService();
    }

    public override void Unload()
    {
        // Framework events/commands are torn down for us, but the container owns the FreeSql and
        // SQLite connection pools; without this they leak on every hot reload.
        // Disposing the container also disposes CommandService, which unregisters its command.
        _provider?.Dispose();
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var api = _provider.GetRequiredService<WeaponSkinAPI>();

        interfaceManager.AddSharedInterface<IWeaponSkinAPI, WeaponSkinAPI>(SharedInterfaceKey, api);

        // Kept so plugins built against the pre-versioned key keep resolving.
        interfaceManager.AddSharedInterface<IWeaponSkinAPI, WeaponSkinAPI>(LegacySharedInterfaceKey, api);
    }
}