using Microsoft.Extensions.Logging;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace WeaponSkins;

public class LocalizationService
{
    private ISwiftlyCore Core { get; init; }
    private ILogger Logger { get; init; }
    private ILocalizer Localizer { get; init; }

    public LocalizationService(ISwiftlyCore core,
        ILogger<LocalizationService> logger)
    {
        Core = core;
        Logger = logger;
        Localizer = Core.Localizer;
    }

    public PlayerLocalizationService this[IPlayer player] => new(Core.Translation.GetPlayerLocalizer(player));
}

public class PlayerLocalizationService
{
    private ILocalizer Localizer { get; init; }

    public PlayerLocalizationService(ILocalizer localizer)
    {
        Localizer = localizer;
    }

    public IPlayer Player { get; init; } = null!;


    public string MenuTitle => Localizer["menu.title"];
    public string MenuTitleSkins => Localizer["menu.skins.title"];
    public string MenuTitleKnifes => Localizer["menu.knifes.title"];
    public string MenuTitleGloves => Localizer["menu.gloves.title"];
    public string MenuTitleStickers => Localizer["menu.stickers.title"];
    public string MenuTitleKeychains => Localizer["menu.keychains.title"];
    public string MenuTitleAgents => Localizer["menu.agents.title"];
    public string MenuTitleMusicKits => Localizer["menu.musickits.title"];
    public string MenuTitleSkinProperties => Localizer["menu.skinproperties.title"];
    public string MenuTitleKnifeProperties => Localizer["menu.knifeproperties.title"];
    public string MenuTitleGloveProperties => Localizer["menu.gloveproperties.title"];
    public string MenuTitleStickerProperties => Localizer["menu.stickerproperties.title"];
    public string MenuTitleKeychainProperties => Localizer["menu.keychainproperties.title"];
    public string MenuSkinPropertiesSetStattrak => Localizer["menu.skinproperties.setstattrak"];
    public string MenuSkinPropertiesUnsetStattrak => Localizer["menu.skinproperties.unsetstattrak"];
    public string MenuSkinPropertiesSetSouvenir => Localizer["menu.skinproperties.setsouvenir"];
    public string MenuSkinPropertiesUnsetSouvenir => Localizer["menu.skinproperties.unsetsouvenir"];
    public string MenuSkinPropertiesSetSticker(int slot, string stickerName) => Localizer["menu.skinproperties.setsticker", slot, stickerName];
    public string MenuSkinPropertiesSetKeychain(int slot, string keychainName) => Localizer["menu.skinproperties.setkeychain", slot, keychainName];
    public string MenuSkinPropertiesWear => Localizer["menu.skinproperties.wear"];
    public string MenuSkinPropertiesSeed => Localizer["menu.skinproperties.seed"];
    public string MenuSkinPropertiesNametag => Localizer["menu.skinproperties.nametag"];
    public string MenuSkinPropertiesNametagNone => Localizer["menu.skinproperties.nametagnone"];
    public string MenuSkinPropertiesNametagUnset => Localizer["menu.skinproperties.nametagunset"];
    public string MenuStickerPropertiesOffsetX => Localizer["menu.stickerproperties.offsetx"];
    public string MenuStickerPropertiesOffsetY => Localizer["menu.stickerproperties.offsety"];
    public string MenuStickerPropertiesResetOffset => Localizer["menu.stickerproperties.resetoffset"];
    public string MenuKeychainPropertiesOffsetX => Localizer["menu.keychainproperties.offsetx"];
    public string MenuKeychainPropertiesOffsetY => Localizer["menu.keychainproperties.offsety"];
    public string MenuKeychainPropertiesOffsetZ => Localizer["menu.keychainproperties.offsetz"];
    public string MenuKeychainPropertiesResetOffset => Localizer["menu.keychainproperties.resetoffset"];
    public string MenuReset => Localizer["menu.reset"];

    public string MenuSkinPropertiesStattrakCount(int stattrak) =>
        Localizer["menu.skinproperties.stattrakcount", stattrak];

    public string MenuUnset => Localizer["menu.unset"];

    // Panorama panel (PanelService)
    public string PanelBrand => Localizer["panel.brand"];
    public string PanelLoadout => Localizer["panel.loadout"];
    public string PanelItems => Localizer["panel.items"];
    public string PanelBoth => Localizer["panel.both"];
    public string PanelBothTeams => Localizer["panel.bothteams"];
    public string PanelNav(string category) => Localizer[$"panel.nav.{category}"];
    public string PanelBack => Localizer["panel.back"];
    public string PanelReset => Localizer["panel.reset"];
    public string PanelWear => Localizer["panel.wear"];
    public string PanelSeed => Localizer["panel.seed"];
    public string PanelRandom => Localizer["panel.random"];
    public string PanelOff => Localizer["panel.off"];
    public string PanelOn => Localizer["panel.on"];
    public string PanelStickers => Localizer["panel.stickers"];
    public string PanelCharm => Localizer["panel.charm"];
    public string PanelEquip => Localizer["panel.equip"];
    public string PanelHomeTitle => Localizer["panel.home.title"];
    public string PanelHomeSubtitle => Localizer["panel.home.subtitle"];
    public string PanelHomeT => Localizer["panel.home.t"];
    public string PanelHomeCT => Localizer["panel.home.ct"];
    public string PanelDefault => Localizer["panel.default"];
    public string PanelVanilla => Localizer["panel.vanilla"];
    public string PanelKnife => Localizer["panel.knife"];
    public string PanelGloves => Localizer["panel.gloves"];
    public string PanelAgent(string team) => Localizer["panel.agent", team];
    public string PanelWeapon => Localizer["panel.weapon"];
    public string PanelMusicKit => Localizer["panel.musickit"];
    public string PanelCustomize => Localizer["panel.customize"];
    public string PanelEditing(string team, int count) => Localizer["panel.editing", team, count];
    public string PanelSaved(string team) => Localizer["panel.saved", team];
    public string PanelResetDone(string team) => Localizer["panel.resetdone", team];
    public string PanelAppliesTo(string team) => Localizer["panel.appliesto", team];
    public string PanelNoPermission => Localizer["panel.nopermission"];
    public string PanelPage(int page, int total) => Localizer["panel.page", page, total];
}