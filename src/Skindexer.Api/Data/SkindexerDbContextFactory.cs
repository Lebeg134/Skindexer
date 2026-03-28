using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Skindexer.Api.Data;

public class SkindexerDbContextFactory : IDesignTimeDbContextFactory<SkindexerDbContext>
{
    public SkindexerDbContext CreateDbContext(string[] args)
    {
        // Design-time only — used by `dotnet ef` CLI, never at runtime.
        // Must match the local dev DB in docker-compose.yml.
        var options = new DbContextOptionsBuilder<SkindexerDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=skindexer;Username=skindexer;Password=skindexer_dev")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SkindexerDbContext(options);
    }
}