using Microsoft.Extensions.Logging;

namespace WeaponSkins.Database;

public class DatabaseSynchronizeService
{
    private DatabaseService DatabaseService { get; init; }
    private DataService DataService { get; init; }
    private ILogger<DatabaseSynchronizeService> Logger { get; init; }

    public DatabaseSynchronizeService(DatabaseService databaseService,
        DataService dataService,
        ILogger<DatabaseSynchronizeService> logger)
    {
        DatabaseService = databaseService;
        DataService = dataService;
        Logger = logger;
    }

    public void Synchronize()
    {
        // Runs off the game thread; the data services are all ConcurrentDictionary-backed. Faults are
        // logged here so a failed sync doesn't disappear as an unobserved task exception.
        _ = Task.Run(async () =>
        {
            try
            {
                var skins = await DatabaseService.GetAllSkinsAsync();
                skins.ToList().ForEach(skin => DataService.WeaponDataService.StoreSkin(skin));
                var knives = await DatabaseService.GetAllKnifesAsync();
                knives.ToList().ForEach(knife => DataService.KnifeDataService.StoreKnife(knife));
                var gloves = await DatabaseService.GetAllGlovesAsync();
                gloves.ToList().ForEach(glove => DataService.GloveDataService.StoreGlove(glove));
                var agents = await DatabaseService.GetAllAgentsAsync();
                agents.ToList().ForEach(agent => DataService.AgentDataService.SetAgent(agent.SteamID, agent.Team, agent.AgentIndex));
                var musicKits = await DatabaseService.GetAllMusicKitsAsync();
                musicKits.ToList().ForEach(mk => DataService.MusicKitDataService.SetMusicKit(mk.SteamID, mk.MusicKitIndex));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to synchronize skin data from the database.");
            }
        });
    }
}
