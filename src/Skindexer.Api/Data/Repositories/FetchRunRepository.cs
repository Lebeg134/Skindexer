using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Features.FetchRuns;
using Skindexer.Fetchers.Models;

namespace Skindexer.Api.Data.Repositories;

public class FetchRunRepository(SkindexerDbContext db) : IFetchRunRepository
{
    public async Task<Guid> StartRunAsync(PersistOptions options, CancellationToken ct = default)
    {
        var run = new FetchRunEntity
        {
            Id          = Guid.NewGuid(),
            FetcherId   = options.FetcherId,
            StartedAt   = DateTime.UtcNow,
            Status      = "running",
            TriggeredBy = options.TriggeredBy
        };

        db.FetchRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run.Id;
    }

    public async Task CompleteRunAsync(Guid runId, PersistCounts counts, CancellationToken ct = default)
    {
        await db.FetchRuns
            .Where(x => x.Id == runId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.FinishedAt, DateTime.UtcNow)
                .SetProperty(x => x.Status, "success")
                .SetProperty(x => x.ItemsUpserted, counts.ItemsUpserted)
                .SetProperty(x => x.VariantsUpserted, counts.VariantsUpserted)
                .SetProperty(x => x.PricesInserted, counts.PricesInserted), ct);
    }

    public async Task FailRunAsync(Guid runId, string? errorMessage, CancellationToken ct = default)
    {
        await db.FetchRuns
            .Where(x => x.Id == runId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.FinishedAt, DateTime.UtcNow)
                .SetProperty(x => x.Status, "failed")
                .SetProperty(x => x.ErrorMessage, errorMessage), ct);
    }
}