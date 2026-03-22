using Skindexer.Contracts.Models;

namespace Skindexer.Api.Features.Prices;

public interface IPriceRepository
{
    Task<IReadOnlyList<SkinPrice>> GetCurrentPricesByGameAsync(string gameId, CancellationToken ct = default);
}