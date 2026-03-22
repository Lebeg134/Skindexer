using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Grades;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Data.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly SkindexerDbContext _db;

    public GradeRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GradeResponse>> GetGradesByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Grades
            .Where(g => g.GameId == gameId)
            .OrderBy(g => g.Order)
            .Select(g => new GradeResponse
            {
                Slug = g.Slug,
                DisplayName = g.Name,
                Order = g.Order
            })
            .ToListAsync(ct);
    }
}