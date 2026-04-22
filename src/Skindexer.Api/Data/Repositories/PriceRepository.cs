using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Features.Prices;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Data.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly SkindexerDbContext _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PriceRepository> _logger;

    public PriceRepository(SkindexerDbContext db, NpgsqlDataSource dataSource, ILogger<PriceRepository> logger)
    {
        _db = db;
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SkinPrice>> GetCurrentPricesByGameAsync(
        string gameId, PriceQueryParams query, CancellationToken ct = default)
    {
        var q = _db.CurrentPrices.Where(p => p.GameId == gameId);

        if (query.PriceType is not null)
            q = q.Where(p => p.PriceType == query.PriceType);

        if (query.Source is not null)
            q = q.Where(p => p.Source == query.Source);

        var rows = await q.ToListAsync(ct);

        return rows.Select(p => new SkinPrice
        {
            VariantId = p.VariantId,
            GameId = p.GameId,
            Slug = p.Slug,
            Source = p.Source,
            PriceType = p.PriceType,
            Price = p.Price,
            Currency = p.Currency,
            Volume = p.Volume,
            RecordedAt = p.RecordedAt
        }).ToList();
    }

    public async Task InsertPricesAsync(IReadOnlyList<SkinPrice> prices, CancellationToken ct = default)
    {
        if (prices.Count == 0) return;

        // Batch in chunks of 500 to keep SaveChanges calls manageable
        foreach (var batch in prices.Chunk(500))
        {
            var entities = batch.Select(p => new SkinPriceEntity
            {
                Id = Guid.NewGuid(),
                VariantId = p.VariantId,
                Slug = p.Slug,
                Source = p.Source,
                PriceType = p.PriceType,
                Price = p.Price,
                Currency = p.Currency,
                Volume = p.Volume,
                RecordedAt = p.RecordedAt,
            });

            await _db.Prices.AddRangeAsync(entities, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task UpsertPricesAsync(IReadOnlyList<SkinPrice> prices, CancellationToken ct = default)
    {
        if (prices.Count == 0) return;

        var deduped = prices
            .GroupBy(p => (p.VariantId, p.Source, p.PriceType, p.RecordedAt))
            .Select(g => g.Last())
            .ToList();

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            // Step 1 — Create temp staging table
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = """
                                  CREATE TEMP TABLE price_snapshots_staging (
                                      id          uuid,
                                      variant_id  uuid,
                                      game_id     varchar(64),
                                      slug        varchar(256),
                                      source      varchar(128),
                                      price_type  varchar(64),
                                      price       numeric(18,8),
                                      currency    varchar(16),
                                      volume      integer,
                                      recorded_at timestamptz
                                  ) ON COMMIT DROP
                                  """;
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Step 2 — Binary COPY into staging
            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY price_snapshots_staging (id, variant_id, game_id, slug, source, price_type, price, currency, volume, recorded_at) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                foreach (var p in deduped)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(Guid.NewGuid(), NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(p.VariantId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(p.GameId, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(p.Slug, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(p.Source, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(p.PriceType, NpgsqlDbType.Varchar, ct);
                    await writer.WriteAsync(p.Price, NpgsqlDbType.Numeric, ct);
                    await writer.WriteAsync(p.Currency, NpgsqlDbType.Varchar, ct);
                    await writer.WriteNullableAsync(p.Volume, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(p.RecordedAt, NpgsqlDbType.TimestampTz, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // Step 3 — Upsert from staging into price_snapshots
            await using (var upsertCmd = conn.CreateCommand())
            {
                upsertCmd.Transaction = transaction;
                upsertCmd.CommandText = """
                                        INSERT INTO price_snapshots (id, variant_id, game_id, slug, source, price_type, price, currency, volume, recorded_at)
                                        SELECT id, variant_id, game_id, slug, source, price_type, price, currency, volume, recorded_at
                                        FROM price_snapshots_staging
                                        ON CONFLICT (variant_id, source, price_type, recorded_at) DO NOTHING
                                        """;

                var inserted = await upsertCmd.ExecuteNonQueryAsync(ct);
                var skipped = deduped.Count - inserted;

                _logger.LogInformation(
                    "BulkUpsertPrices — snapshots: {Total} processed ({Inserted} inserted, {Skipped} skipped)",
                    deduped.Count, inserted, skipped);
            }

            // Step 4 — Upsert latest price per (variant, source, price_type) into current_prices
            await using (var currentCmd = conn.CreateCommand())
            {
                currentCmd.Transaction = transaction;
                currentCmd.CommandText = """
                                         INSERT INTO current_prices (variant_id, game_id, slug, source, price_type, price, currency, volume, recorded_at)
                                         SELECT DISTINCT ON (variant_id, source, price_type)
                                             variant_id, game_id, slug, source, price_type, price, currency, volume, recorded_at
                                         FROM price_snapshots_staging
                                         ORDER BY variant_id, source, price_type, recorded_at DESC
                                         ON CONFLICT (variant_id, source, price_type) DO UPDATE SET
                                             price       = EXCLUDED.price,
                                             volume      = EXCLUDED.volume,
                                             recorded_at = EXCLUDED.recorded_at,
                                             slug        = EXCLUDED.slug
                                         WHERE EXCLUDED.recorded_at > current_prices.recorded_at
                                         """;

                var currentUpdated = await currentCmd.ExecuteNonQueryAsync(ct);

                _logger.LogInformation(
                    "BulkUpsertPrices — current_prices: {Updated} rows upserted",
                    currentUpdated);
            }

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkUpsertPrices failed — rolling back");
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}