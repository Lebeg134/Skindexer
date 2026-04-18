using System.Net.Http.Json;
using System.Text.Json;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Contracts.Models.Metadata;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;
using Skindexer.Fetchers.Games.CS2.Mappers;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers;

public class CS2ByMykelItemFetcher : IScheduledFetcher
{
    private const string GameId = GameIds.CounterStrike;
    private const string Source = "bymykel";
    private const string BaseUrl = "https://raw.githubusercontent.com/ByMykel/CSGO-API/main/public/api/en";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly CS2ByMykelSkinMapper _skinMapper;
    private readonly CS2ByMykelCollectibleMapper _collectibleMapper;
    private readonly CS2ByMykelPatchMapper _patchMapper;
    private readonly CS2ByMykelMusicKitMapper _musicKitMapper;

    public CS2ByMykelItemFetcher(HttpClient http, CS2ByMykelSkinMapper skinMapper,
        CS2ByMykelCollectibleMapper collectibleMapper, CS2ByMykelPatchMapper patchMapper,
        CS2ByMykelMusicKitMapper musicKitMapper)
    {
        _http = http;
        _skinMapper = skinMapper;
        _collectibleMapper = collectibleMapper;
        _patchMapper = patchMapper;
        _musicKitMapper = musicKitMapper;
    }

    public string FetcherId => "cs2-bymykel";
    public string DisplayName => "CSGO-API (ByMykel)";

    // TODO: make PollingInterval configurable via appsettings.json (CS2ByMykelItemFetcherOptions)
    public TimeSpan PollingInterval => TimeSpan.FromDays(1);

    public async Task<FetchResult> FetchAsync(CancellationToken ct = default)
    {
        var items = new List<SkinItem>();
        var variants = new List<SkinVariant>();
        var warnings = new List<string>();

        var skinDtos = await FetchDtosAsync<ByMykelSkin>("skins.json", warnings, ct);
        var skinResult = _skinMapper.Map(skinDtos, warnings);
        items.AddRange(skinResult.Items);
        variants.AddRange(skinResult.Variants);

        var collectibleDtos = await FetchDtosAsync<ByMykelCollectible>("collectibles.json", warnings, ct);
        var collectibleResult = _collectibleMapper.Map(collectibleDtos, warnings);
        items.AddRange(collectibleResult.Items);
        variants.AddRange(collectibleResult.Variants);

        var patchDtos = await FetchDtosAsync<ByMykelPatch>("patches.json", warnings, ct);
        var patchResult = _patchMapper.Map(patchDtos, warnings);
        items.AddRange(patchResult.Items);
        variants.AddRange(patchResult.Variants);

        var musicKitDtos = await FetchDtosAsync<ByMykelMusicKit>("music_kits.json", warnings, ct);
        var musicKitResult = _musicKitMapper.Map(musicKitDtos, warnings);
        items.AddRange(musicKitResult.Items);
        variants.AddRange(musicKitResult.Variants);
        await FetchEndpoint<ByMykelSticker>("stickers.json", MapSticker, items, warnings, ct);
        await FetchEndpoint<ByMykelKeychain>("keychains.json", MapKeychain, items, warnings, ct);
        await FetchEndpoint<ByMykelCrate>("crates.json", MapCrate, items, warnings, ct);
        await FetchEndpoint<ByMykelKey>("keys.json", MapKey, items, warnings, ct);
        await FetchEndpoint<ByMykelAgent>("agents.json", MapAgent, items, warnings, ct);
        await FetchEndpoint<ByMykelGraffiti>("graffiti.json", MapGraffiti, items, warnings, ct);

        var dupes = items
            .GroupBy(i => (i.GameId, i.Slug))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        warnings.AddRange(dupes.Select(dupe => $"Duplicate slug detected: [{dupe.GameId}] {dupe.Slug}"));

        var dupeVariants = variants
            .GroupBy(v => v.Slug)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        warnings.AddRange(dupeVariants.Select(slug => $"Duplicate variant slug detected: {slug}"));
            
            
        return warnings.Count > 0
            ? FetchResult.Partial(GameId, Source, items, variants, [], warnings)
            : FetchResult.Success(GameId, Source, items, variants, []);
    }


    private async Task<IReadOnlyList<TDto>> FetchDtosAsync<TDto>(
        string endpoint, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var dtos = await _http.GetFromJsonAsync<List<TDto>>(
                $"{BaseUrl}/{endpoint}", JsonOptions, ct);

            if (dtos is not null) return dtos;
            warnings.Add($"{endpoint}: received null response");
            return [];
        }
        catch (Exception ex)
        {
            warnings.Add($"{endpoint}: fetch failed — {ex.Message}");
            return [];
        }
    }

    // --- Endpoint fetching ---

    private async Task FetchEndpoint<TDto>(
        string endpoint,
        Func<TDto, SkinItem?> mapper,
        List<SkinItem> results,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var dtos = await _http.GetFromJsonAsync<List<TDto>>(
                $"{BaseUrl}/{endpoint}", JsonOptions, ct);

            if (dtos is null)
            {
                warnings.Add($"{endpoint}: received null response");
                return;
            }

            foreach (var dto in dtos)
            {
                try
                {
                    var item = mapper(dto);
                    if (item is not null)
                        results.Add(item);
                }
                catch (Exception ex)
                {
                    warnings.Add($"{endpoint}: failed to map item — {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"{endpoint}: fetch failed — {ex.Message}");
        }
    }

    // --- Mappers ---

    private SkinItem? MapSticker(ByMykelSticker dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2StickerMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Effect = dto.Effect,
            TournamentName = dto.Tournament?.Name,
            Collection = dto.Collections?.FirstOrDefault()?.Name,
        };

        return BuildItem(CS2ItemTypes.Sticker, dto.Name, dto.Image, dto.MarketHashName is not null, metadata.ToDictionary());
    }

    private SkinItem? MapKeychain(ByMykelKeychain dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2KeychainMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Collection = dto.Collections?.FirstOrDefault()?.Name,
        };

        return BuildItem(CS2ItemTypes.Keychain, dto.Name, dto.Image, dto.MarketHashName is not null, metadata.ToDictionary());
    }

    private SkinItem? MapCrate(ByMykelCrate dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2CrateMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Type = dto.Type,
            FirstSaleDate = dto.FirstSaleDate,
            Rental = dto.Rental,
        };

        return BuildItem(CS2ItemTypes.Container, dto.Name, dto.Image, dto.MarketHashName is not null, metadata.ToDictionary());
    }

    private SkinItem? MapKey(ByMykelKey dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2KeyMetadata
        {
            Rarity = null, // keys have no rarity field in the API
            RarityColor = null,
        };

        return BuildItem(CS2ItemTypes.Key, dto.Name, dto.Image, dto.Marketable, metadata.ToDictionary());
    }

    private SkinItem? MapAgent(ByMykelAgent dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2AgentMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Team = dto.Team?.Name,
            Collection = dto.Collections?.FirstOrDefault()?.Name,
        };

        return BuildItem(CS2ItemTypes.Agent, dto.Name, dto.Image, dto.MarketHashName is not null, metadata.ToDictionary());
    }

    private SkinItem? MapGraffiti(ByMykelGraffiti dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2GraffitiMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
        };

        return BuildItem(CS2ItemTypes.Graffiti, dto.Name, dto.Image, dto.MarketHashName is not null, metadata.ToDictionary());
    }

    // --- Shared builder for non-skin items ---

    private static SkinItem BuildItem(
        string type,
        string name,
        string? imageUrl,
        bool isMarketable,
        Dictionary<string, object?> metadata) => new()
    {
        Id = Guid.NewGuid(),
        GameId = GameId,
        ItemType = type,
        Slug = CS2ByMykelSlugHelper.GenerateSlug(name),
        Name = name,
        ImageUrl = imageUrl,
        IsTradeable = isMarketable,
        IsMarketable = isMarketable,
        Metadata = metadata,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };
}