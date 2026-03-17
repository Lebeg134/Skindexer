using Microsoft.Extensions.DependencyInjection;
using Skindexer.Fetchers.Games.CS2;
using Skindexer.Fetchers.Games.Rust;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers;

public static class FetcherServiceExtensions
{
    public static IServiceCollection AddSkindexerFetchers(this IServiceCollection services)
    {
        // Register each fetcher as IGameFetcher
        // Adding a new game = adding one line here
        services.AddHttpClient<CS2Fetcher>();
        services.AddHttpClient<RustFetcher>();

        services.AddSingleton<IGameFetcher, CS2Fetcher>();
        services.AddSingleton<IGameFetcher, RustFetcher>();

        // Registry gets all IGameFetcher implementations injected automatically
        services.AddSingleton<FetcherRegistry>();

        return services;
    }
}
