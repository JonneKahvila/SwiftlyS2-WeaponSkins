using Microsoft.Extensions.DependencyInjection;

namespace WeaponSkins.Injections;

public static class PanelServiceInjection
{
    public static IServiceCollection AddPanelService(this IServiceCollection services)
    {
        services.AddSingleton<PanelService>();
        return services;
    }

    public static IServiceProvider UsePanelService(this IServiceProvider provider)
    {
        provider.GetRequiredService<PanelService>();
        return provider;
    }
}
