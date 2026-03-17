namespace Skindexer.Contracts.Models;

public class FetchResult
{
    public string GameId { get; init; } = default!;
    public string Source { get; init; } = default!;
    public IReadOnlyList<SkinItem> Items { get; init; } = [];
    public IReadOnlyList<SkinPrice> Prices { get; init; } = [];
    public DateTime FetchedAt { get; init; } = DateTime.UtcNow;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static FetchResult Success(string gameId, string source,
        IReadOnlyList<SkinItem> items, IReadOnlyList<SkinPrice> prices) => new()
    {
        GameId = gameId,
        Source = source,
        Items = items,
        Prices = prices,
        IsSuccess = true
    };

    public static FetchResult Failure(string gameId, string source, string error) => new()
    {
        GameId = gameId,
        Source = source,
        IsSuccess = false,
        ErrorMessage = error
    };
    
    public static FetchResult Partial(string gameId, string source,
        IReadOnlyList<SkinItem> items, IReadOnlyList<SkinPrice> prices,
        IReadOnlyList<string> warnings) => new()
    {
        GameId = gameId,
        Source = source,
        Items = items,
        Prices = prices,
        IsSuccess = true,
        Warnings = warnings
    };
}
