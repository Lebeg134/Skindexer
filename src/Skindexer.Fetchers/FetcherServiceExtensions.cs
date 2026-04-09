using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skindexer.Fetchers.Games.CS2.Fetchers;
using Skindexer.Fetchers.Games.CS2.Mappers;
using Skindexer.Fetchers.Games.Rust;
using Skindexer.Fetchers.Interfaces;
using Skindexer.Fetchers.Options;

namespace Skindexer.Fetchers;

public static class FetcherServiceExtensions
{
    public static IServiceCollection AddSkindexerFetchers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: extract to Skindexer.Game.CS2 package
        // CS2
        services.Configure<KaggleFetcherOptions>(configuration.GetSection("Kaggle"));
        services.AddSingleton<CS2KagglePriceFetcher>();
        
        services.AddHttpClient<CS2ByMykelItemFetcher>();
        services.AddSingleton<IGameFetcher, CS2ByMykelItemFetcher>();
        services.AddSingleton<CS2ByMykelSkinMapper>();
        services.AddSingleton<CS2ByMykelCollectibleMapper>();
        services.AddSingleton<CS2ByMykelPatchMapper>();
        services.AddSingleton<CS2ByMykelMusicKitMapper>();
        
        // Rust
        services.AddHttpClient<RustFetcher>();
        services.AddSingleton<IGameFetcher, RustFetcher>();
        
        services.AddSingleton<FetcherRegistry>();

        return services;
    }
}
