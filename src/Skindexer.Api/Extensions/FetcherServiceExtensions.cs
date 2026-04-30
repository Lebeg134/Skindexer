using Skindexer.Api.Options;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Games.CS2.Fetchers.ByMykelItemFetcher;
using Skindexer.Fetchers.Games.CS2.Fetchers.CS2Sh;
using Skindexer.Fetchers.Games.CS2.Fetchers.KaggleFetcher;
using Skindexer.Fetchers.Games.CS2.Fetchers.PriceEmpireFetcher;
using Skindexer.Fetchers.Games.CS2.Fetchers.SteamAnalyst;
using Skindexer.Fetchers.Games.CS2.Fetchers.SteamWebApiFetcher;
using Skindexer.Fetchers.Games.Rust;

namespace Skindexer.Api.Extensions;

public static class FetcherServiceExtensions
{
    private static readonly FetcherDescriptor[] Descriptors =
    [
        CS2ByMykelItemFetcher.Descriptor,
        CS2CS2ShFetcher.Descriptor,
        CS2PricempireFetcher.Descriptor,
        CS2SteamAnalystFetcher.Descriptor,
        CS2SteamWebApiFetcher.Descriptor,
        CS2KagglePriceFetcher.Descriptor,
        RustFetcher.Descriptor,
    ];

    public static IServiceCollection AddSkindexerFetchers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(FetcherOptions.SectionName)
            .Get<FetcherOptions>() ?? new FetcherOptions();

        var index = Descriptors.ToDictionary(
            d => d.FetcherId,
            StringComparer.OrdinalIgnoreCase);

        foreach (var fetcherId in options.Enabled)
        {
            if (index.TryGetValue(fetcherId, out var descriptor))
            {
                descriptor.Register(services, configuration);
            }
            else
            {
                Console.Error.WriteLine(
                    $"[Skindexer] Unknown fetcher ID '{fetcherId}' in Fetchers:Enabled — skipping. " +
                    $"Known IDs: {string.Join(", ", index.Keys)}");
            }
        }

        services.AddSingleton<FetcherRegistry>();
        return services;
    }
}