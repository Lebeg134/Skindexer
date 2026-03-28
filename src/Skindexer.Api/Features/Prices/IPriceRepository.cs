using Skindexer.Contracts.Models;

namespace Skindexer.Api.Features.Prices;

public interface IPriceRepository
{
    Task<IReadOnlyList<SkinPrice>> GetCurrentPricesByGameAsync(string gameId, CancellationToken ct = default);
 
    /// <summary>
    /// Appends price snapshots. Never updates existing records —
    /// price history is immutable once written.
    /// </summary>
    Task InsertPricesAsync(IReadOnlyList<SkinPrice> prices, CancellationToken ct = default);
}