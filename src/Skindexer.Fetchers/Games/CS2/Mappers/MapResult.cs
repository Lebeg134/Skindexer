using Skindexer.Contracts.Models;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public record MapResult(
    IReadOnlyList<SkinItem> Items,
    IReadOnlyList<SkinVariant> Variants
);