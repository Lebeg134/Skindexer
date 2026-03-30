# Skindexer — Test Style Guide

## Stack

| Item | Detail |
|------|--------|
| Framework | .NET 9 xUnit |
| Mocking | NSubstitute |
| Assertions | Plain `xUnit Assert.*` — no FluentAssertions |
---

## File Structure

Tests are grouped using `#region` blocks by concern, in this order:

1. `Test Data Builders` — helpers and factory methods
2. `Construction` — constructor tests (if applicable)
3. `Guard Clauses` — argument validation / exception tests
4. Feature-specific regions — e.g. `FetchAsync`, `Correctness`, `SlugResolution`, `Persistence`

```csharp
public class ExampleTests
{
    #region Test Data Builders
    // private static helpers here
    #endregion

    #region Guard Clauses
    // ArgumentNullException, ArgumentOutOfRangeException, etc.
    #endregion

    #region Correctness
    // core behaviour tests
    #endregion
}
```

---

## Test Data

Named variables declared explicitly before use — never inline at the call site.

```csharp
// correct
var items = new List<SkinItem> { BuildSkinItem("ak-47-redline") };
var result = await fetcher.FetchAsync();

// avoid
var result = await fetcher.FetchAsync(new List<SkinItem> { BuildSkinItem("ak-47-redline") });
```

Collection literals:
- Source data → `new List<T> { }`
- Empty collections → `[]`

---

## Helpers

Reusable logic lives in `#region Test Data Builders` as `private static` methods.

- **Data factories** return tuples: `BuildValidFetchResult()` → `(Items, Variants, Prices)`
- **Wrappers** encapsulate the system under test: `BuildSkinItem()`, `BuildSkinPrice()`, `BuildFetchResult()`

```csharp
private static SkinItem BuildSkinItem(string slug) => new()
{
    Id       = Guid.NewGuid(),
    GameId   = "cs2",
    Slug     = slug,
    Name     = slug,
    ItemType = CS2ItemTypes.WeaponSkin,
};

private static SkinPrice BuildSkinPrice(string slug, decimal price) => new()
{
    Slug      = slug,
    GameId    = "cs2",
    Source    = Sources.CS2Sh,
    PriceType = PriceTypes.LowestListing,
    Price     = price,
    Currency  = "USD",
    RecordedAt = DateTime.UtcNow,
};
```

---

## Variable Naming Inside Tests

Follow the `source` → `result` → assertions pattern.

```csharp
var items  = new List<SkinItem> { BuildSkinItem("ak-47-redline") };   // input
var result = await _persister.PersistAsync(fetchResult);               // output

var persistedCount = await _db.SkinItems                              // intermediate — named meaningfully
    .Where(i => i.GameId == "cs2")
    .CountAsync();

Assert.Equal(items.Count, persistedCount);
```

Mocked dependencies are named after their type: `fetcher`, `registry`, `persister`.

---

## Comments

Used sparingly — only when a scenario requires a non-obvious slug format or persistence behaviour clarified.

```csharp
// cs2.sh returns prices keyed by market_hash_name — slug resolution
// happens in FetchResultPersister, not in the fetcher itself.
var result = await fetcher.FetchAsync();
Assert.All(result.Prices, p => Assert.Equal(Guid.Empty, p.VariantId));
```