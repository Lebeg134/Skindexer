using Microsoft.EntityFrameworkCore;
using Npgsql;
using Skindexer.Api.Data;
using Testcontainers.PostgreSql;

namespace Skindexer.Api.Tests.Data.Repositories.Fixtures;


public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("skindexer_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;
    public DbContextOptions<SkindexerDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();

        DataSource = new NpgsqlDataSourceBuilder(Postgres.GetConnectionString())
            .EnableDynamicJson()
            .Build();

        Options = new DbContextOptionsBuilder<SkindexerDbContext>()
            .UseNpgsql(DataSource)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new SkindexerDbContext(Options);
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await Postgres.DisposeAsync();
    }
}