using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using SwiftlyS2.Shared.Players;

using WeaponSkins.Database;
using WeaponSkins.Econ;
using WeaponSkins.Services;
using WeaponSkins.Shared;

namespace WeaponSkins;

public class WeaponSkinAPI : IWeaponSkinAPI
{
    private IInventoryUpdateService InventoryUpdateService { get; init; }
    private DataService DataService { get; init; }
    private StorageService StorageService { get; init; }
    private EconService EconService { get; init; }
    private ItemPermissionService ItemPermissionService { get; init; }
    private WeaponSkinGetterAPI WeaponSkinGetterAPI { get; init; }
    private ILogger<WeaponSkinAPI> Logger { get; init; }

    public IReadOnlyDictionary<string, ItemDefinition> Items => EconService.Items.AsReadOnly();

    public IReadOnlyDictionary<string, List<PaintkitDefinition>> WeaponToPaintkits =>
        EconService.WeaponToPaintkits.AsReadOnly();

    public IReadOnlyDictionary<string, StickerCollectionDefinition> StickerCollections =>
        EconService.StickerCollections.AsReadOnly();

    public IReadOnlyDictionary<string, KeychainDefinition> Keychains => EconService.Keychains.AsReadOnly();

    public WeaponSkinAPI(IInventoryUpdateService inventoryUpdateService,
        WeaponSkinGetterAPI weaponSkinGetterAPI,
        DataService dataService,
        StorageService storageService,
        EconService econService,
        ItemPermissionService itemPermissionService,
        ILogger<WeaponSkinAPI> logger
    )
    {
        InventoryUpdateService = inventoryUpdateService;
        DataService = dataService;
        StorageService = storageService;
        EconService = econService;
        ItemPermissionService = itemPermissionService;
        WeaponSkinGetterAPI = weaponSkinGetterAPI;
        Logger = logger;
    }

    /// <summary>
    /// Runs a storage write off the game thread. The provider is resolved on the caller's thread so a
    /// concurrent <see cref="SetExternalStorageProvider"/> can't redirect an in-flight write, and faults
    /// are logged rather than surfacing later as unobserved task exceptions.
    /// </summary>
    private void Persist(string operation,
        Func<IStorageProvider, Task> action)
    {
        var provider = StorageService.Get();
        _ = Task.Run(async () =>
        {
            try
            {
                await action(provider);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to persist {Operation} to storage provider {Provider}.",
                    operation, provider.Name);
            }
        });
    }

    public void SetWeaponSkins(IEnumerable<WeaponSkinData> skins,
        bool permanent = false)
    {
        InventoryUpdateService.UpdateWeaponSkins(skins);
        if (permanent)
        {
            Persist("weapon skins", provider => provider.StoreSkinsAsync(skins));
        }
    }

    public void SetKnifeSkins(IEnumerable<KnifeSkinData> knives,
        bool permanent = false)
    {
        InventoryUpdateService.UpdateKnifeSkins(knives);
        if (permanent)
        {
            Persist("knife skins", provider => provider.StoreKnifesAsync(knives));
        }
    }

    public void SetGloveSkins(IEnumerable<GloveData> gloves,
        bool permanent = false)
    {
        InventoryUpdateService.UpdateGloveSkins(gloves);
        if (permanent)
        {
            Persist("glove skins", provider => provider.StoreGlovesAsync(gloves));
        }
    }

    public void UpdateWeaponSkin(ulong steamid,
        Team team,
        ushort definitionIndex,
        Action<WeaponSkinData> action,
        bool permanent = false)
    {
        if (!DataService.WeaponDataService.TryGetSkin(steamid, team, definitionIndex, out var skin))
        {
            skin = new WeaponSkinData { SteamID = steamid, Team = team, DefinitionIndex = definitionIndex };
        }

        var newSkin = skin.DeepClone();
        action(newSkin);
        var constrained = ItemPermissionService.ApplyWeaponUpdateRules(skin, newSkin);
        SetWeaponSkins([constrained], permanent);
    }

    public void UpdateKnifeSkin(ulong steamid,
        Team team,
        Action<KnifeSkinData> action,
        bool permanent = false)
    {
        if (!DataService.KnifeDataService.TryGetKnife(steamid, team, out var knife))
        {
            knife = new KnifeSkinData { SteamID = steamid, Team = team, DefinitionIndex = 0 };
        }

        var newKnife = knife.DeepClone();
        action(newKnife);
        if (newKnife.DefinitionIndex == 0)
        {
            return;
        }

        if (!ItemPermissionService.CanUseKnifeSkins(steamid))
        {
            return;
        }

        SetKnifeSkins([newKnife], permanent);
    }

    public void UpdateGloveSkin(ulong steamid,
        Team team,
        Action<GloveData> action,
        bool permanent = false)
    {
        if (!DataService.GloveDataService.TryGetGlove(steamid, team, out var glove))
        {
            glove = new GloveData { SteamID = steamid, Team = team, DefinitionIndex = 0 };
        }

        var newGlove = glove.DeepClone();
        action(newGlove);
        if (newGlove.DefinitionIndex == 0)
        {
            return;
        }

        if (!ItemPermissionService.CanUseGloveSkins(steamid))
        {
            return;
        }

        SetGloveSkins([newGlove], permanent);
    }

    public bool TryGetWeaponSkin(ulong steamid,
        Team team,
        ushort definitionIndex,
        [MaybeNullWhen(false)] out WeaponSkinData skin) =>
        WeaponSkinGetterAPI.TryGetWeaponSkin(steamid, team, definitionIndex, out skin);

    public bool TryGetWeaponSkins(ulong steamid,
        [MaybeNullWhen(false)] out IEnumerable<WeaponSkinData> result) =>
        WeaponSkinGetterAPI.TryGetWeaponSkins(steamid, out result);

    public bool TryGetKnifeSkin(ulong steamid,
        Team team,
        [MaybeNullWhen(false)] out KnifeSkinData knife) =>
        WeaponSkinGetterAPI.TryGetKnifeSkin(steamid, team, out knife);

    public bool TryGetKnifeSkins(ulong steamid,
        [MaybeNullWhen(false)] out IEnumerable<KnifeSkinData> result) =>
        WeaponSkinGetterAPI.TryGetKnifeSkins(steamid, out result);

    public bool TryGetGloveSkin(ulong steamid,
        Team team,
        [MaybeNullWhen(false)] out GloveData glove) =>
        WeaponSkinGetterAPI.TryGetGloveSkin(steamid, team, out glove);

    public bool TryGetGloveSkins(ulong steamid,
        [MaybeNullWhen(false)] out IEnumerable<GloveData> result) =>
        WeaponSkinGetterAPI.TryGetGloveSkins(steamid, out result);

    public bool TryGetAgentSkin(ulong steamid,
        Team team,
        out int agentIndex) =>
        DataService.AgentDataService.TryGetAgent(steamid, team, out agentIndex);

    public bool TryGetAgentSkins(ulong steamid,
        [MaybeNullWhen(false)] out IEnumerable<(Team Team, int AgentIndex)> result)
    {
        var agents = new List<(Team, int)>();

        if (DataService.AgentDataService.TryGetAgent(steamid, Team.T, out var tIndex))
        {
            agents.Add((Team.T, tIndex));
        }

        if (DataService.AgentDataService.TryGetAgent(steamid, Team.CT, out var ctIndex))
        {
            agents.Add((Team.CT, ctIndex));
        }

        if (agents.Count > 0)
        {
            result = agents;
            return true;
        }

        result = null;
        return false;
    }

    public void ResetWeaponSkin(ulong steamid,
        Team team,
        ushort definitionIndex,
        bool permanent = false
    )
    {
        InventoryUpdateService.ResetWeaponSkin(steamid, team, definitionIndex);
        if (permanent)
        {
            Persist("weapon skin reset", provider => provider.RemoveSkinAsync(steamid, team, definitionIndex));
        }
    }

    public void ResetKnifeSkin(ulong steamid,
        Team team,
        bool permanent = false
    )
    {
        InventoryUpdateService.ResetKnifeSkin(steamid, team);
        if (permanent)
        {
            Persist("knife skin reset", provider => provider.RemoveKnifeAsync(steamid, team));
        }
    }

    public void ResetGloveSkin(ulong steamid,
        Team team,
        bool permanent = false)
    {
        InventoryUpdateService.ResetGloveSkin(steamid, team);
        if (permanent)
        {
            Persist("glove skin reset", provider => provider.RemoveGloveAsync(steamid, team));
        }
    }

    public void UpdateAgentSkin(ulong steamid,
        Team team,
        int agentIndex,
        bool permanent = false)
    {
        DataService.AgentDataService.SetAgent(steamid, team, agentIndex);
        if (permanent)
        {
            Persist("agent skin", provider => provider.StoreAgentsAsync([(steamid, team, agentIndex)]));
        }
    }

    public void ResetAgentSkin(ulong steamid,
        Team team,
        bool permanent = false)
    {
        DataService.AgentDataService.TryRemoveAgent(steamid, team);
        if (permanent)
        {
            Persist("agent skin reset", provider => provider.RemoveAgentAsync(steamid, team));
        }
    }

    public void SetMusicKit(ulong steamid,
        int musicKitIndex,
        bool permanent = true)
    {
        DataService.MusicKitDataService.SetMusicKit(steamid, musicKitIndex);
        InventoryUpdateService.UpdateMusicKit(steamid, musicKitIndex);
        if (permanent)
        {
            Persist("music kit", provider => provider.StoreMusicKitsAsync([(steamid, musicKitIndex)]));
        }
    }

    public void ResetMusicKit(ulong steamid,
        bool permanent = true)
    {
        DataService.MusicKitDataService.RemoveMusicKit(steamid);
        InventoryUpdateService.ResetMusicKit(steamid);
        if (permanent)
        {
            Persist("music kit reset", provider => provider.RemoveMusicKitAsync(steamid));
        }
    }

    public void SetExternalStorageProvider(IStorageProvider provider)
    {
        StorageService.Set(provider);
    }
}