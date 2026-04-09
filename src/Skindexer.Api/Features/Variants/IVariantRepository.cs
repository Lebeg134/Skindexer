using Skindexer.Contracts.Models;

namespace Skindexer.Api.Features.Variants;

public interface IVariantRepository
{
    Task UpsertVariantsAsync(IReadOnlyList<SkinVariant> variants, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, Guid>> GetSlugToVariantIdMapAsync(string gameId, CancellationToken ct = default);
}