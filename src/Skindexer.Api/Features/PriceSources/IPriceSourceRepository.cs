using Skindexer.Contracts.Models;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.PriceSources;

public interface IPriceSourceRepository
{
    Task<IReadOnlyList<PriceSourceResponse>> GetPriceSourcesAsync(
        string gameId,
        PriceSourceQueryParams queryParams,
        CancellationToken ct = default);
}