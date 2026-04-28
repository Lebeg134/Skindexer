using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.Rust;

public class RustFetcher : IScheduledFetcher
{
    private readonly HttpClient _http;
    private readonly ILogger<RustFetcher> _logger;

    public string FetcherId => "rust";
    public string DisplayName => "Rust";
    
    public bool IsAuthoritativeItemSource { get; } = false;
    public TimeSpan PollingInterval => TimeSpan.FromMinutes(15);

    public RustFetcher(HttpClient http, ILogger<RustFetcher> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: implement Rust skin API calls
            _logger.LogInformation("Fetching Rust skin prices...");

            var items = new List<SkinItem>();
            var variants = new List<SkinVariant>();
            var prices = new List<SkinPrice>();

            return FetchResult.Success(FetcherId, "steam_market", items, variants, prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Rust skin prices");
            return FetchResult.Failure(FetcherId, "steam_market", ex.Message);
        }
    }
}