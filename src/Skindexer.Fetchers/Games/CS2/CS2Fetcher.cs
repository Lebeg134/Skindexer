using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2;

public class CS2Fetcher : IScheduledFetcher
{
    private readonly HttpClient _http;
    private readonly ILogger<CS2Fetcher> _logger;

    public string FetcherId => "cs2";
    public string DisplayName => "Counter-Strike 2";
    public TimeSpan PollingInterval => TimeSpan.FromMinutes(5);

    public CS2Fetcher(HttpClient http, ILogger<CS2Fetcher> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: implement Steam Market API calls
            // This is a skeleton — real implementation comes next
            _logger.LogInformation("Fetching CS2 prices...");

            var items = new List<SkinItem>();
            var variants = new List<SkinVariant>();
            var prices = new List<SkinPrice>();

            return FetchResult.Success(FetcherId, "steam_market", items, variants, prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch CS2 prices");
            return FetchResult.Failure(FetcherId, "steam_market", ex.Message);
        }
    }
}