using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace WeaponSkins;

public partial class MenuService
{
    public IMenuAPI BuildMusicKitMenu(IPlayer player)
    {
        var menu = Core.MenusAPI.CreateBuilder();
        menu.Design.SetMenuTitle(LocalizationService[player].MenuTitleMusicKits);

        var resetOption = new ButtonMenuOption(LocalizationService[player].MenuReset);
        resetOption.Click += (_, args) =>
        {
            Api.ResetMusicKit(player.SteamID);
            return ValueTask.CompletedTask;
        };
        menu.AddOption(resetOption);

        foreach (var musicKit in EconService.MusicKits.Values.OrderBy(mk => mk.Index))
        {
            var musicKitName = EconService.GetLocalizedName(musicKit.LocalizedNames, player.PlayerLanguage.Value);

            var truncatedName = musicKitName.Length > 30 ? musicKitName.Substring(0, 27) + "..." : musicKitName;
            var index = musicKit.Index;

            var selectOption = new ButtonMenuOption(truncatedName);
            selectOption.Click += (_, args) =>
            {
                Api.SetMusicKit(args.Player.SteamID, index);
                return ValueTask.CompletedTask;
            };

            menu.AddOption(selectOption);
        }

        return menu.Build();
    }

    private IMenuOption GetMusicKitMenuSubmenuOption(IPlayer player)
    {
        if (!ItemPermissionService.CanUseMusicKits(player.SteamID))
        {
            return CreateDisabledOption(LocalizationService[player].MenuTitleMusicKits);
        }

        var option = new SubmenuMenuOption(LocalizationService[player].MenuTitleMusicKits, () =>
        {
            return Task.FromResult(BuildMusicKitMenu(player));
        });

        return option;
    }
}
