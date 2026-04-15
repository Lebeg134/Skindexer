namespace Skindexer.Api.Features.Enrichment;

public interface IItemEnricher
{
    public Task EnrichAsync(string gameId, CancellationToken ct);
}