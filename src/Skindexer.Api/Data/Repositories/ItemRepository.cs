using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Skindexer.Api.Features.Items;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly SkindexerDbContext _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<ItemRepository> _logger;

    public ItemRepository(SkindexerDbContext db, NpgsqlDataSource dataSource, ILogger<ItemRepository> logger)
    {
        _db = db;
        _logger = logger;
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<SkinItem>> GetItemsByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Items
            .Where(i => i.GameId == gameId)
            .Select(i => new SkinItem
            {
                Id = i.Id,
                GameId = i.GameId,
                ItemType = i.ItemType,
                Slug = i.Slug,
                Name = i.Name,
                ImageUrl = i.ImageUrl,
                IsTradeable = i.IsTradeable,
                IsMarketable = i.IsMarketable,
                Metadata = i.Metadata,
                AddedToGameAt = i.AddedToGameAt,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task UpsertItemsAsync(IReadOnlyList<SkinItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return;

        var deduped = items
            .GroupBy(i => (i.GameId, i.Slug))
            .Select(g => g.Last())
            .ToList();

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            // Step 1 — Create temp staging table (dropped automatically on commit)
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = """
                                  CREATE TEMP TABLE items_staging (
                                      id               uuid,
                                      game_id          varchar(64),
                                      item_type        varchar(64),
                                      slug             varchar(256),
                                      name             varchar(256),
                                      image_url        text,
                                      is_tradeable     boolean,
                                      is_marketable    boolean,
                                      metadata         text,
                                      added_to_game_at timestamptz,
                                      created_at       timestamptz,
                                      updated_at       timestamptz
                                  ) ON COMMIT DROP
                                  """;
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Step 2 — Binary COPY into staging
            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY items_staging (id, game_id, item_type, slug, name, image_url, is_tradeable, is_marketable, metadata, added_to_game_at, created_at, updated_at) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                var now = DateTime.UtcNow;

                foreach (var item in deduped)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(item.Id, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(item.GameId, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(item.ItemType, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(item.Slug, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(item.Name, NpgsqlDbType.Varchar, ct);
                    await writer.WriteNullableAsync(item.ImageUrl, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(item.IsTradeable, NpgsqlDbType.Boolean, ct);
                    await writer.WriteAsync(item.IsMarketable, NpgsqlDbType.Boolean, ct);
                    await writer.WriteAsync(JsonSerializer.Serialize(item.Metadata), NpgsqlDbType.Text, ct);
                    await writer.WriteNullableAsync(item.AddedToGameAt, NpgsqlDbType.TimestampTz, ct);
                    await writer.WriteAsync(now, NpgsqlDbType.TimestampTz, ct); // created_at — only used on INSERT
                    await writer.WriteAsync(now, NpgsqlDbType.TimestampTz, ct); // updated_at
                }

                await writer.CompleteAsync(ct);
            }

            // Step 3 — Upsert from staging, track inserted vs updated rows
            int inserted = 0, updated = 0;

            await using (var upsertCmd = conn.CreateCommand())
            {
                upsertCmd.Transaction = transaction;
                upsertCmd.CommandText = """
                                        INSERT INTO items (id, game_id, item_type, slug, name, image_url, is_tradeable, is_marketable, metadata, added_to_game_at, created_at, updated_at)
                                        SELECT id, game_id, item_type, slug, name, image_url, is_tradeable, is_marketable, metadata::jsonb, added_to_game_at, created_at, updated_at
                                        FROM items_staging
                                        ON CONFLICT (game_id, slug) DO UPDATE SET
                                            item_type        = EXCLUDED.item_type,
                                            name             = EXCLUDED.name,
                                            image_url        = EXCLUDED.image_url,
                                            is_tradeable     = EXCLUDED.is_tradeable,
                                            is_marketable    = EXCLUDED.is_marketable,
                                            metadata         = EXCLUDED.metadata,
                                            added_to_game_at = EXCLUDED.added_to_game_at,
                                            updated_at       = NOW()
                                        RETURNING (xmax = 0) AS inserted
                                        """;

                await using var reader = await upsertCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    if (reader.GetBoolean(0)) inserted++;
                    else updated++;
                }
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "UpsertItems complete — {Total} rows processed ({Inserted} inserted, {Updated} updated)",
                deduped.Count, inserted, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertItems failed — rolling back");
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetSlugToItemIdMapAsync(string gameId,
        CancellationToken ct = default)
    {
        return await _db.Items
            .Where(i => i.GameId == gameId)
            .ToDictionaryAsync(i => i.Slug, i => i.Id, ct);
    }
}