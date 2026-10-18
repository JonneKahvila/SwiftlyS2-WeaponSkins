<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>WeaponSkins (Beta)</strong></h2>
  <h3>A powerful swiftlys2 plugin to change player's weapon skins, knifes and gloves.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/samyycX/WeaponSkins/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/samyycX/WeaponSkins?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/samyycX/WeaponSkins" alt="License">
</p>

## Features
- MySQL & sqlite database support
- Fully functioning in-game skin menu
- Compatible with CounterStrikeSharp WeaponPaints database
- Long-term stattrak tracking
- Player-based skin name localization
- Completely game-based econ data dumping (no network required)

## Main Configuration
The `config.jsonc` should have the following structure in the `WeaponSkins` section:
```jsonc
{
  "WeaponSkins": {
    "StorageBackend": "inherit",
    "InventoryUpdateBackend": "hook",
    "SyncFromDatabaseWhenPlayerJoin": false,
    "ItemLanguages": [],
    "MenuPermission": ""
  }
}
```

#### `StorageBackend`
When set to `inherit`, the plugin use the database configuration from swiftlys2 database configuration.

When set to `sqlite`, the plugin use its own file-based sqlite database.

#### `InventoryUpdateBackend`
Recommended to set to `hook` for now. Another option is `inventory` but its deprecated.

#### `SyncFromDatabaseWhenPlayerJoin`
When set to true, the plugin will automatically synchronize skin data from database when a player join.

The update is asynchronous so it won't introduce lags in theory.

### `ItemLanguages`
When unset or set to empty list, the plugin will load item and skin names in all languages.

When set to a list of language, the **first** language will be used as default fallback language, and only  languages in this list will be loaded and set for player based on their game language.

This configuration helps optimizing your memory usage, reducing 80MB at max.

Check the `Code` column in this table for all available languages: 
[Available language codes](https://swiftlys2.net/docs/development/translations/#language-codes)

### `MenuPermission`
Permission required to run the `ws` menu command. Leave empty (the default) to let everyone open the menu.

This gates the command itself, so players without it get the standard "no permission" response instead of a
menu full of disabled entries. Changing it takes effect on config reload — no server restart needed.

## Skin Panel
`!ws` opens a clickable Panorama panel: sidebar categories, a T/CT loadout overview, a card grid of
finishes, and a detail view with wear, pattern seed and StatTrak presets. `!knife`, `!gloves`, `!agents`,
`!music` and `!skins` open it at that category (`!skins` jumps to the weapon in hand). Running `!ws`
again closes it. Stickers, charms and name tags still use the classic menu, opened from the panel.

The panel's layout ships in the `jk_hud` client addon ([cs2-hud-jk](https://github.com/JonneKahvila/cs2-hud-jk)),
so clients need that addon mounted. If the HUD can't be spawned, or `PanelEnabled` is false, the commands
open the classic menu instead. `!wsmenu` always opens the classic menu.

```jsonc
{
  "WeaponSkins": {
    "PanelEnabled": true,
    "PanelLayoutPath": "panorama/layout/custom_game/jk_skins.xml"
  }
}
```

## Item Permissions
Gate entire feature groups with a single permission string in `config.jsonc`:
```jsonc
{
  "WeaponSkins": {
    "ItemPermissions": {
      "WeaponSkins": "vip",
      "KnifeSkins": "vip",
      "GloveSkins": "vip",
      "Stickers": "vip",
      "Keychains": "vip",
      "Agents": "vip",
      "MusicKits": "vip"
    }
  }
}
```
Leave a value empty or remove it to keep the feature available to everyone. Players without the required permission cannot open the related menus, and any equipped cosmetics of that type are hidden until they regain access.

## Showcase
[Youtube](https://youtu.be/MRa8JIRLysE)
  
## Building

- Open the project in your preferred .NET IDE (e.g., Visual Studio, Rider, VS Code).
- Build the project. The output DLL and resources will be placed in the `build/` directory.
- The publish process will also create a zip file for easy distribution.

## Publishing

- Use the `dotnet publish -c Release` command to build and package your plugin.
- The plugin is staged in `build/publish/WeaponSkins/` and packaged as `build/WeaponSkins.zip`.
- Distribute the generated zip file or the contents of `build/publish/WeaponSkins/`.

To deploy straight into a local server instead, override the publish directory:

```sh
dotnet publish -c Release -p:PublishDir=/path/to/csgo/addons/swiftlys2/plugins/WeaponSkins/
```

The zip step is skipped when you override `PublishDir`, so publishing into a live server won't
archive the rest of your plugins folder.
