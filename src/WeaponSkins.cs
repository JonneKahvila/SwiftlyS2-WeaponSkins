using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.SteamAPI;

using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

using WeaponSkins.Configuration;
using WeaponSkins.Injections;
using WeaponSkins.Services;
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
    private ServiceProvider _provider = null!;

    public WeaponSkins(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration.InitializeJsonWithModel<MainConfigModel>("config.jsonc", "Main")
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", false, true);
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
            .AddOptions<MainConfigModel>()
            .BindConfiguration("Main");

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
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IWeaponSkinAPI, WeaponSkinAPI>("WeaponSkins.API",
            _provider.GetRequiredService<WeaponSkinAPI>());
    }
}