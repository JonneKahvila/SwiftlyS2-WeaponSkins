using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

using WeaponSkins.Configuration;
using WeaponSkins.Econ;
using WeaponSkins.Services;
using WeaponSkins.Shared;

namespace WeaponSkins;

/// <summary>
/// Panorama skin panel (cs2-hud-jk's jk_skins.xml). Owns one CCSCustomHudLayout entity, the
/// per-player navigation state, and the OnCustomHudClicked round trip.
/// </summary>
/// <remarks>
/// Same shape as SwiftlyS2-Retakes' WeaponMenuHudService: the layout declares every slot up front
/// and the server only fills dialog variables and toggles classes per player. Every write goes
/// through <see cref="PanelState"/>'s cache so a re-render only sends what actually changed,
/// which also keeps the entity's per-player state from churning.
/// </remarks>
public partial class PanelService : IDisposable
{
    private const string DialogPanel = "jk_sk_dialog";

    private const EHudPanelClassStatus_t On = EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass;
    private const EHudPanelClassStatus_t Off = EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass;

    private ISwiftlyCore Core { get; init; }
    private ILogger<PanelService> Logger { get; init; }
    private IOptionsMonitor<MainConfigModel> Options { get; init; }
    private EconService EconService { get; init; }
    private WeaponSkinAPI Api { get; init; }
    private DataService DataService { get; init; }
    private ItemPermissionService ItemPermissionService { get; init; }
    private LocalizationService LocalizationService { get; init; }
    private MenuService MenuService { get; init; }

    private CCSCustomHudLayout? _hud;
    private bool _spawnErrorLogged;

    private readonly Dictionary<int /* PlayerID */, PanelState> _states = new();
    private readonly Dictionary<int /* PlayerID */, PanelClientCache> _clients = new();

    public PanelService(ISwiftlyCore core,
        ILogger<PanelService> logger,
        IOptionsMonitor<MainConfigModel> options,
        EconService econService,
        WeaponSkinAPI api,
        DataService dataService,
        ItemPermissionService itemPermissionService,
        LocalizationService localizationService,
        MenuService menuService)
    {
        Core = core;
        Logger = logger;
        Options = options;
        EconService = econService;
        Api = api;
        DataService = dataService;
        ItemPermissionService = itemPermissionService;
        LocalizationService = localizationService;
        MenuService = menuService;

        Core.Event.OnCustomHudClicked += OnCustomHudClicked;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.Event.OnMapLoad += OnMapLoad;
    }

    private MainConfigModel Config => Options.CurrentValue;

    public bool IsEnabled => Config.PanelEnabled;

    public bool IsOpen(IPlayer player) => _states.ContainsKey(player.PlayerID);

    /// <summary>
    /// Opens the panel at <paramref name="category"/>. Returns false when the HUD could not be
    /// shown, so the caller can fall back to the native menu.
    /// </summary>
    public bool Show(IPlayer player,
        PanelCategory category = PanelCategory.Home)
    {
        if (!IsEnabled || !player.IsValid || player.IsFakeClient) return false;

        var hud = EnsureSpawned();
        if (hud is null) return false;

        if (!_clients.TryGetValue(player.PlayerID, out var client))
        {
            client = new PanelClientCache();
            _clients[player.PlayerID] = client;
        }

        var state = new PanelState
        {
            PlayerId = player.PlayerID,
            Client = client,
            Team = player.Controller.Team switch
            {
                Team.T => TeamChoice.T,
                Team.CT => TeamChoice.CT,
                _ => TeamChoice.Both
            }
        };
        _states[player.PlayerID] = state;

        try
        {
            WriteStaticLabels(hud, player, state);
            Navigate(hud, player, state, category);
            hud.SetHasClassForPlayer(player.PlayerID, DialogPanel, "is-hidden", Off);
            hud.SetInputCaptureEnabledForPlayer(player.PlayerID, true);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to show the skin panel for {Player}.", player.Name);
            _states.Remove(player.PlayerID);
            return false;
        }
    }

    /// <summary>
    /// Opens straight at the finishes of the weapon in hand (the <c>skins</c> shortcut), or at
    /// Rifles when the player isn't holding a skinnable weapon.
    /// </summary>
    public bool ShowWeaponInHand(IPlayer player)
    {
        ItemDefinitionHeld(player, out var held);
        if (!Show(player, held is null ? PanelCategory.Rifles : GetWeaponCategory(held.Index))) return false;
        if (held is null || _hud is not { } hud || !_states.TryGetValue(player.PlayerID, out var state)) return true;

        state.ItemName = held.Name;
        state.View = PanelView.Finishes;
        Render(hud, player, state);
        return true;
    }

    private void ItemDefinitionHeld(IPlayer player,
        out ItemDefinition? held)
    {
        held = null;
        if (!MenuService.TryGetWeaponInHand(player, out var weapon)) return;

        var index = weapon.AttributeManager.Item.ItemDefinitionIndex;
        if (Utilities.IsWeaponDefinitionIndex(index) && FindItem(index) is { } item &&
            EconService.WeaponToPaintkits.ContainsKey(item.Name))
        {
            held = item;
        }
    }

    public void Close(IPlayer player) => Close(player.PlayerID);

    public void Close(int playerId)
    {
        _states.Remove(playerId);

        var hud = _hud;
        if (hud is null || !hud.IsValid) return;

        try
        {
            hud.SetInputCaptureEnabledForPlayer(playerId, false);
            hud.SetHasClassForPlayer(playerId, DialogPanel, "is-hidden", On);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to close the skin panel for player {PlayerId}.", playerId);
        }
    }

    private CCSCustomHudLayout? EnsureSpawned()
    {
        if (_hud is not null && _hud.IsValid) return _hud;
        _hud = null;

        try
        {
            var hud = Core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
            hud.StrLayout = Config.PanelLayoutPath;
            hud.StrLayoutUpdated();
            hud.DispatchSpawn();
            hud.SetHasClass(DialogPanel, "is-hidden", On);

            _hud = hud;
            _clients.Clear();
            Logger.LogInformation("Skin panel HUD spawned with layout {Layout}.", Config.PanelLayoutPath);
            return _hud;
        }
        catch (Exception ex)
        {
            if (!_spawnErrorLogged)
            {
                _spawnErrorLogged = true;
                Logger.LogError(ex, "Failed to spawn the skin panel HUD; commands fall back to the native menu.");
            }
            return null;
        }
    }

    private void ReleaseEntities()
    {
        var hud = _hud;
        _hud = null;
        if (hud is null) return;

        try
        {
            if (hud.IsValid) hud.Despawn();
        }
        catch (Exception ex)
        {
            // Expected on a map change: the entity died with the map already.
            Logger.LogDebug(ex, "Skin panel HUD entity was already gone.");
        }
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        // The old map's entities are gone; respawn lazily on the next Show.
        _hud = null;
        _spawnErrorLogged = false;
        _states.Clear();
        _clients.Clear();
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        var playerId = @event.PlayerId;
        _states.Remove(playerId);
        if (!_clients.Remove(playerId, out var client)) return;

        // The entity keys per-player state by slot, so whoever gets this slot next would inherit
        // it. Drop every override back to the layout default.
        var hud = _hud;
        if (hud is null || !hud.IsValid) return;
        try
        {
            hud.SetInputCaptureEnabledForPlayer(playerId, false);
            hud.SetHasClassForPlayer(playerId, DialogPanel, "is-hidden", EHudPanelClassStatus_t.k_eHudPanelClassStatus_Undefined);
            foreach (var (panel, className) in client.Classes.Keys)
            {
                hud.SetHasClassForPlayer(playerId, panel, className, EHudPanelClassStatus_t.k_eHudPanelClassStatus_Undefined);
            }
            foreach (var name in client.Vars.Keys)
            {
                hud.RemoveDialogVariableStringForPlayer(playerId, DialogPanel, name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to clear skin panel state for player {PlayerId}.", playerId);
        }
    }

    public void Dispose()
    {
        Core.Event.OnCustomHudClicked -= OnCustomHudClicked;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;
        Core.Event.OnMapLoad -= OnMapLoad;

        foreach (var playerId in _states.Keys.ToList())
        {
            Close(playerId);
        }

        ReleaseEntities();
        GC.SuppressFinalize(this);
    }

    // ---- Cached per-player writes -------------------------------------------------------------

    private static void SetVar(CCSCustomHudLayout hud,
        PanelState state,
        string name,
        string value)
    {
        var vars = state.Client.Vars;
        if (vars.TryGetValue(name, out var current) && current == value) return;
        vars[name] = value;
        hud.SetDialogVariableStringForPlayer(state.PlayerId, DialogPanel, name, value);
    }

    private static void SetClass(CCSCustomHudLayout hud,
        PanelState state,
        string panelId,
        string className,
        bool enabled)
    {
        var classes = state.Client.Classes;
        var key = (panelId, className);
        var current = classes.TryGetValue(key, out var cached) ? cached : HasClassInLayout(panelId, className);
        if (current == enabled) return;
        classes[key] = enabled;
        hud.SetHasClassForPlayer(state.PlayerId, panelId, className, enabled ? On : Off);
    }

    /// <summary>Classes jk_skins.xml puts on a panel before the server touches it.</summary>
    private static bool HasClassInLayout(string panelId,
        string className) =>
        className == "is-hidden" &&
        (panelId is "sk_view_grid" or "sk_view_detail" || panelId.StartsWith("sk_card_", StringComparison.Ordinal));
}
