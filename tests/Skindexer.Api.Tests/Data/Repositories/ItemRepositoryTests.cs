using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Repositories;
using Skindexer.Contracts.Models;
using Testcontainers.PostgreSql;

namespace Skindexer.Api.Tests.Data.Repositories;

public class ItemRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("skindexer_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private SkindexerDbContext _db = null!;
    private NpgsqlDataSource _dataSource = null!;
    private ItemRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _dataSource = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString())
            .EnableDynamicJson()
            .Build();

        var options = new DbContextOptionsBuilder<SkindexerDbContext>()
            .UseNpgsql(_dataSource)
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new SkindexerDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _repository = new ItemRepository(_db, NullLogger<ItemRepository>.Instance, _dataSource);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    #region Test Data Builders

    private static SkinItem BuildItem(
        string slug = "ak-47-redline",
        string gameId = "cs2",
        string name = "AK-47 | Redline",
        bool isTradeable = true,
        bool isMarketable = true,
        Dictionary<string, object?>? metadata = null) => new()
    {
        Id = Guid.NewGuid(),
        GameId = gameId,
        Slug = slug,
        Name = name,
        ImageUrl = null,
        IsTradeable = isTradeable,
        IsMarketable = isMarketable,
        Metadata = metadata ?? [],
        AddedToGameAt = null,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task UpsertItemsAsync_EmptyList_DoesNothing()
    {
        await _repository.UpsertItemsAsync([], CancellationToken.None);

        var count = await _db.Items.CountAsync();
        Assert.Equal(0, count);
    }

    #endregion

    #region Insert

    [Fact]
    public async Task UpsertItemsAsync_NewItems_InsertsAllRows()
    {
        var items = new List<SkinItem>
        {
            BuildItem(slug: "ak-47-redline", name: "AK-47 | Redline"),
            BuildItem(slug: "m4a4-howl", name: "M4A4 | Howl"),
            BuildItem(slug: "awp-dragon-lore", name: "AWP | Dragon Lore"),
        };

        await _repository.UpsertItemsAsync(items, CancellationToken.None);

        var stored = await _db.Items.OrderBy(i => i.Slug).ToListAsync();
        Assert.Equal(3, stored.Count);
        Assert.Contains(stored, i => i.Slug == "ak-47-redline");
        Assert.Contains(stored, i => i.Slug == "m4a4-howl");
        Assert.Contains(stored, i => i.Slug == "awp-dragon-lore");
    }

    [Fact]
    public async Task UpsertItemsAsync_NullableFields_InsertsWithoutError()
    {
        var item = BuildItem();

        await _repository.UpsertItemsAsync([item], CancellationToken.None);

        var stored = await _db.Items.SingleAsync();
        Assert.Null(stored.ImageUrl);
        Assert.Null(stored.AddedToGameAt);
    }

    [Fact]
    public async Task UpsertItemsAsync_WithMetadata_PersistsMetadataCorrectly()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["weapon_type"] = "rifle",
            ["rarity"] = "covert",
            ["has_stattrak"] = true,
        };

        var item = BuildItem(metadata: metadata);

        await _repository.UpsertItemsAsync([item], CancellationToken.None);

        var stored = await _db.Items.SingleAsync();
        Assert.Equal("rifle", stored.Metadata["weapon_type"]?.ToString());
        Assert.Equal("covert", stored.Metadata["rarity"]?.ToString());
    }

    [Fact]
    public async Task UpsertItemsAsync_MultipleGames_ItemsStoredUnderCorrectGame()
    {
        var items = new List<SkinItem>
        {
            BuildItem(slug: "ak-47-redline", gameId: "cs2"),
            BuildItem(slug: "ak-47-redline", gameId: "tf2"),
        };

        await _repository.UpsertItemsAsync(items, CancellationToken.None);

        var cs2Items = await _db.Items.Where(i => i.GameId == "cs2").ToListAsync();
        var tf2Items = await _db.Items.Where(i => i.GameId == "tf2").ToListAsync();
        Assert.Single(cs2Items);
        Assert.Single(tf2Items);
    }

    #endregion
    
    #region GetItemsByGame

    [Fact]
    public async Task GetItemsByGameAsync_NoItems_ReturnsEmpty()
    {
        var result = await _repository.GetItemsByGameAsync("cs2", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsByGameAsync_FiltersByGameId()
    {
        var items = new List<SkinItem>
        {
            BuildItem(slug: "ak-47-redline", gameId: "cs2"),
            BuildItem(slug: "ak-47-redline", gameId: "tf2"),
        };

        await _repository.UpsertItemsAsync(items, CancellationToken.None);

        var result = await _repository.GetItemsByGameAsync("cs2", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("cs2", result[0].GameId);
    }

    #endregion
}