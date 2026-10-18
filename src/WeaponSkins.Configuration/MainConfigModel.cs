namespace WeaponSkins.Configuration;

public class MainConfigModel
{
    public string StorageBackend { get; set; } = "inherit";

    public string InventoryUpdateBackend { get; set; } = "hook";

    public bool SyncFromDatabaseWhenPlayerJoin { get; set; } = false;

    public List<string> ItemLanguages { get; set; } = [];

    /// <summary>
    /// Permission required to run the skin menu command. Empty means everyone may use it.
    /// </summary>
    public string MenuPermission { get; set; } = "";

    public ItemPermissionConfig ItemPermissions { get; set; } = new();

    /// <summary>
    /// Open the Panorama panel instead of the native menu for <c>ws</c> and the shortcut commands.
    /// Needs the jk_hud addon (cs2-hud-jk) on clients; falls back to the native menu when the HUD
    /// entity can't be spawned.
    /// </summary>
    public bool PanelEnabled { get; set; } = true;

    public string PanelLayoutPath { get; set; } = "panorama/layout/custom_game/jk_skins.xml";
}