namespace Skindexer.Api.Features.Grades;

public static class GetGradesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/grades", async (string gameId, IGradeRepository repository, CancellationToken ct) =>
        {
            var grades = await repository.GetGradesByGameAsync(gameId, ct);
            return Results.Ok(grades);
        });
    }
}