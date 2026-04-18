namespace Skindexer.Api.Features.Enrichment;

public interface IItemEnricher
{
    string GameId { get; }
    Task EnrichAsync(CancellationToken ct = default);
}