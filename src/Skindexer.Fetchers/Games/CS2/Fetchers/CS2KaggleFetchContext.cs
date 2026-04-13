namespace Skindexer.Fetchers.Games.CS2.Fetchers;

public class CS2KaggleFetchContext
{
    public IReadOnlyDictionary<string, Guid> VariantSlugMap { get; init; } 
        = new Dictionary<string, Guid>();
}