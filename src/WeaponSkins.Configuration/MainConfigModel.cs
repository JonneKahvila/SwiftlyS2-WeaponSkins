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
}