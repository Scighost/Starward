using Microsoft.Extensions.DependencyInjection;
using Starward.GameInstall.HotUpdate;
using Starward.GameInstall.Service;

namespace Starward.GameInstall;

public static class GameInstallServiceCollectionExtensions
{
    public static IServiceCollection AddStarwardGameInstall(this IServiceCollection services)
    {
        services.AddScoped<HoYoPlayService>();
        services.AddScoped<GameConfigService>();
        services.AddScoped<GameResourceService>();
        services.AddScoped<GenshinDispatchClient>();
        services.AddScoped<GenshinHotUpdateService>();
        services.AddSingleton<HttpService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<GameInstallRunner>();
        return services;
    }
}