using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public abstract class CS2ByMykelMapperBase<TDto>
{
    private readonly ILogger _logger;
    private readonly string _filename;

    protected CS2ByMykelMapperBase(ILogger logger, string filename)
    {
        _logger = logger;
        _filename = filename;
    }

    public MapResult Map(IReadOnlyList<TDto> dtos, List<string> warnings)
    {
        // Step 1 — generate base slugs
        var mapped = new List<(TDto Dto, string BaseSlug)>();
        foreach (var dto in dtos)
        {
            try
            {
                mapped.Add((dto, CS2ByMykelSlugHelper.GenerateSlug(GetName(dto) ?? "")));
            }
            catch (Exception ex)
            {
                warnings.Add($"{_filename}: failed to generate slug for '{GetName(dto)}' — {ex.Message}");
            }
        }

        // Step 2 — detect collisions
        var collisionSlugs = SlugCollisionResolver.FindCollisions(mapped, GetDiscriminator);
        _logger.LogDebug("Found {Count} slug collision groups in {File}", collisionSlugs.Count, _filename);

        // Step 3 — construct SkinItems and SkinVariants with resolved slugs
        var items = new List<SkinItem>();
        var variants = new List<SkinVariant>();

        foreach (var (dto, baseSlug) in mapped)
        {
            try
            {
                var slug = SlugCollisionResolver.ResolveSlug(baseSlug, GetDiscriminator(dto), collisionSlugs);
                var item = MapItem(dto, slug);
                if (item is null) continue;

                items.Add(item);
                variants.AddRange(MapVariants(dto, item));
            }
            catch (Exception ex)
            {
                warnings.Add($"{_filename}: failed to map '{GetName(dto)}' — {ex.Message}");
            }
        }

        return new MapResult(items, variants);
    }

    protected abstract string? GetName(TDto dto);
    protected abstract string? GetDiscriminator(TDto dto);
    protected abstract SkinItem? MapItem(TDto dto, string slug);

    /// <summary>
    /// Produces the SkinVariant rows for a successfully mapped item.
    /// Default implementation returns empty — override in mappers that produce variants.
    /// </summary>
    protected virtual IReadOnlyList<SkinVariant> MapVariants(TDto dto, SkinItem item) => [];
}