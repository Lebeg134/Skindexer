using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Grades;

public interface IGradeRepository
{
    Task<IReadOnlyList<GradeResponse>> GetGradesByGameAsync(string gameId, CancellationToken ct = default);
}
