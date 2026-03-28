namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

public static class SlugCollisionResolver
{
    public static HashSet<string> FindCollisions<TDto>(
        IReadOnlyList<(TDto Dto, string BaseSlug)> mapped,
        Func<TDto, string?> paintIndexSelector)
    {
        return mapped
            .GroupBy(m => m.BaseSlug)
            .Where(g => g.Select(m => paintIndexSelector(m.Dto)).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
    }

    public static string ResolveSlug(string baseSlug, string? paintIndex, HashSet<string> collisions)
    {
        return collisions.Contains(baseSlug) && paintIndex is not null
            ? $"{baseSlug}-{paintIndex}"
            : baseSlug;
    }
}