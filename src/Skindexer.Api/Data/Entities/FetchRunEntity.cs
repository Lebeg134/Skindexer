namespace Skindexer.Api.Data.Entities;

public class FetchRunEntity
{
    public Guid Id { get; init; }
    public string FetcherId { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = default!;
    public int? ItemsUpserted { get; set; }
    public int? VariantsUpserted { get; set; }
    public int? PricesInserted { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBy { get; set; } = default!;
}