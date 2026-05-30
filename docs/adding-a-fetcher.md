# Adding a New Game Fetcher

This guide walks through adding a new fetcher to Skindexer. Fetchers are the plugin points
for ingesting item catalogs and prices from external sources. The system is game-agnostic —
the same interfaces and patterns apply whether you're adding CS2, Rust, or any other game.

---

## Concepts

**`IGameFetcher`** — the base interface every fetcher implements. Responsible for fetching
and returning a `FetchResult` containing items, variants, and/or prices.

**`IScheduledFetcher`** — extends `IGameFetcher` with a `DefaultCronExpression` for automatic
scheduling. Implement this if your fetcher should run on a recurring cron schedule.

**`FetcherDescriptor`** — a static descriptor on your fetcher class that defines its ID and
how it self-registers into DI. Single source of truth for fetcher identity.

**`FetchResult`** — the return value of `FetchAsync`. Use the static factory methods:
`FetchResult.Success(...)`, `FetchResult.Partial(...)`, `FetchResult.Failure(...)`.

**`IFetchResultPersister`** — lives in `Skindexer.Api`. Fetchers do NOT call this directly —
the scheduler calls it after `FetchAsync` returns. Your fetcher just returns a `FetchResult`.

**`IsAuthoritativeItemSource`** — if `true`, your fetcher can overwrite existing item metadata.
Only set this for the canonical item source of a game (e.g. ByMykel for CS2). All others default to `false`,
meaning they only insert genuinely new items and never overwrite existing metadata.

---

## Step 1 — Create your fetcher class

Create a new folder and file: `Skindexer.Fetchers/Games/<YourGame>/Fetchers/<YourSource>/`.

Below is a complete, annotated reference implementation. Use this as your starting point —
either manually or by feeding it to an AI along with the prompt in the [AI-assisted section](#using-ai-to-implement-a-fetcher).

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.MyGame.Fetchers.MySourceFetcher;

/// <summary>
/// Fetches [item catalog / prices] for [GameName] from [SourceName].
/// Source docs: https://api.example.com/docs
/// </summary>
public sealed class MyGameMySourceFetcher : IScheduledFetcher
{
    // ── Identity ─────────────────────────────────────────────────────────────
    // FetcherId must be unique across all fetchers.
    // Convention: "{game-id}-{source}" e.g. "cs2-skinport", "rust-steammarket"
    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "mygame-mysource",
        Register = (services, _) =>
        {
            services.AddHttpClient<MyGameMySourceFetcher>();
            // ALWAYS register as IGameFetcher — never as IScheduledFetcher directly.
            // FetcherRegistry uses IEnumerable<IGameFetcher> and categorizes via OfType<>.
            services.AddSingleton<IGameFetcher, MyGameMySourceFetcher>();
        }
    };

    // Must match Descriptor.FetcherId exactly. No compiler enforcement — convention only.
    public string FetcherId => Descriptor.FetcherId;
    public string DisplayName => "MyGame MySource Fetcher";

    // Standard 5-field cron (parsed by Cronos in Skindexer.Scheduler).
    // Keep Cronos out of Skindexer.Fetchers — it lives in Scheduler only.
    public string DefaultCronExpression => "0 3 * * *"; // 3:00 AM daily

    // false = only insert new items, never overwrite existing metadata.
    // true  = canonical source, may overwrite. Use sparingly — only one fetcher per game should be true.
    public bool IsAuthoritativeItemSource => false;

    // ── Dependencies ──────────────────────────────────────────────────────────
    private readonly HttpClient _http;
    private readonly ILogger<MyGameMySourceFetcher> _logger;

    public MyGameMySourceFetcher(
        IHttpClientFactory httpClientFactory,
        ILogger<MyGameMySourceFetcher> logger)
    {
        _http = httpClientFactory.CreateClient(nameof(MyGameMySourceFetcher));
        _logger = logger;
    }

    // ── Core Execution ────────────────────────────────────────────────────────
    public async Task<FetchResult> FetchAsync(CancellationToken ct = default)
    {
        // 1. Fetch raw data from the external API
        List<MySourceItemDto>? rawItems;
        try
        {
            rawItems = await FetchRawAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Fetcher}] HTTP fetch failed", FetcherId);
            return FetchResult.Failure("mygame", Sources.MyGameMySource, ex.Message);
        }

        if (rawItems is null || rawItems.Count == 0)
            return FetchResult.Failure("mygame", Sources.MyGameMySource, "Response was empty or null");

        _logger.LogInformation("[{Fetcher}] Received {Count} items", FetcherId, rawItems.Count);

        // 2. Map raw DTOs to SkinItem / SkinVariant / SkinPrice
        var items = new List<SkinItem>();
        var variants = new List<SkinVariant>();
        var prices = new List<SkinPrice>();
        var warnings = new List<string>();
        var recordedAt = DateTime.UtcNow;

        foreach (var raw in rawItems)
        {
            // Map to your domain models here.
            // See CS2SkinportFetcher and CS2ByMykelItemFetcher for real examples.
        }

        // 3. Return result using static factory methods — never construct FetchResult directly
        return warnings.Count > 0
            ? FetchResult.Partial("mygame", Sources.MyGameMySource, items, variants, prices, warnings, IsAuthoritativeItemSource)
            : FetchResult.Success("mygame", Sources.MyGameMySource, items, variants, prices, IsAuthoritativeItemSource);
    }

    // ── HTTP ──────────────────────────────────────────────────────────────────
    private async Task<List<MySourceItemDto>?> FetchRawAsync(CancellationToken ct)
    {
        // Add any required headers (auth, compression, etc.) here.
        // cs2.sh requires Accept-Encoding: gzip + GZipStream decompression.
        // Skinport requires Accept-Encoding: br + BrotliStream decompression.
        var response = await _http.GetAsync("https://api.example.com/items", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MySourceItemDto>>(ct: ct);
    }
}
```

### Key models

**`SkinItem`** — represents a tradeable item. Slug must be globally unique per `(GameId, Slug)`.

**`SkinVariant`** — represents a specific variant of an item (e.g. wear condition for a skin).
Each variant has its own `Slug` and a `Metadata` dictionary for game-specific properties.

**`SkinPrice`** — a price record. Set `VariantId = Guid.Empty` and populate `Slug` — the persister
resolves `VariantId` via `GetSlugToVariantIdMapAsync` after variants are upserted. Never trust
in-memory GUIDs for `VariantId` in prices.

---

## Step 2 — Add your descriptor to `FetcherServiceExtensions`

Open `Skindexer.Fetchers/FetcherServiceExtensions.cs` and add to the `Descriptors` array:

```csharp
private static readonly FetcherDescriptor[] Descriptors =
[
    // existing fetchers...
    MyGameMySourceFetcher.Descriptor,
];
```

That's all the wiring needed — the config-driven registration picks it up automatically.

---

## Step 3 — Add constants to `Skindexer.Contracts`

If your fetcher introduces a new game or source, add constants to the relevant files:

```csharp
// Skindexer.Contracts/Constants/GameIds.cs
public static class GameIds
{
    public const string MyGame = "mygame";
}

// Skindexer.Contracts/Constants/Sources.cs
public static class Sources
{
    public const string MyGameMySource = "mygame-mysource";
}
```

If your fetcher produces prices with specific price types (e.g. buy orders, averages),
add them to `PriceTypes.cs` as well.

---

## Step 4 — Enable via config

```json
{
  "Fetchers": {
    "Enabled": "mygame-mysource"
  }
}
```

Or via environment variable: `Fetchers__Enabled=mygame-mysource`

If `Fetchers:Enabled` is absent or empty, **nothing registers** — there are no defaults.

---

## Using AI to implement a fetcher

The reference implementation above, combined with a real fetcher from the codebase, gives an AI
everything it needs to produce a working fetcher. Use the following prompt template:

---

> **Prompt template**
>
> I'm adding a new fetcher to Skindexer, a game-agnostic skin price API built in .NET 9 / ASP.NET Core.
>
> Here is the fetcher interface contract and a complete reference implementation to follow:
> `[paste the annotated reference above]`
>
> Here is a real existing fetcher for reference (CS2 Skinport):
> `[paste CS2SkinportFetcher.cs]`
>
> Here is the API I want to fetch from:
> `[paste the API docs or a sample JSON response]`
>
> Please implement a fetcher for `[GameName]` using `[SourceName]`.
> The fetcher ID should be `[game-id]-[source]`.
> It should produce: `[items / prices / both]`.
> Notes: `[any quirks — compression format, auth headers, response shape, etc.]`

---

### Tips for better AI output

- Paste a real sample API response — AI maps concrete JSON far more accurately than descriptions
- Mention compression quirks upfront (gzip, brotli, etc.) — these are easy to miss
- If the source uses non-standard price units (e.g. prices in cents), say so explicitly
- Ask for the DTO class alongside the fetcher — keeping them in the same file initially is fine
- After generation, verify: `FetcherId` matches `Descriptor.FetcherId`, registered as `IGameFetcher`, `IsAuthoritativeItemSource` is explicitly set

---

## Checklist

- [ ] Class implements `IGameFetcher` (or `IScheduledFetcher` for cron scheduling)
- [ ] `static FetcherDescriptor Descriptor` defined on the class
- [ ] `FetcherId` property matches `Descriptor.FetcherId` exactly
- [ ] Registered as `IGameFetcher` in `Descriptor.Register` — never as `IScheduledFetcher` directly
- [ ] Descriptor added to `Descriptors` array in `FetcherServiceExtensions`
- [ ] `GameId` constant added to `GameIds.cs` if new game
- [ ] `Sources` constant added to `Sources.cs` if producing prices
- [ ] `IsAuthoritativeItemSource` explicitly set (`false` is the safe default)
- [ ] `SkinPrice.VariantId = Guid.Empty` — persister resolves it via slug map
- [ ] `FetchResult` built via static factories (`Success`, `Partial`, `Failure`)
- [ ] Tests added in `Skindexer.Fetchers.Tests`

---

## Notes

- **Cronos lives only in `Skindexer.Scheduler`** — never reference it from `Skindexer.Fetchers`
- **`IFetchResultPersister` is in `Skindexer.Api`** — fetchers never call it; the scheduler does
- **Binary COPY is used for bulk DB writes** — column order must match temp table DDL exactly
- **Never overwrite item metadata from a non-authoritative source** — the persister enforces this via `IsAuthoritativeItemSource`, but set it correctly to be explicit
- **`FetcherDescriptor.FetcherId` and `IGameFetcher.FetcherId` must match** — no compiler check, pure convention
