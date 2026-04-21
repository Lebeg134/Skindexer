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
    public async Task<IReadOnlyList<SkinPrice>> GetCurrentPricesByGameAsync(string gameId, PriceQueryParams query,
        CancellationToken ct = default)
    {
        var conditions = new List<string> { "i.game_id = {0}" };
        var parameters = new List<object> { gameId };

        if (query.PriceType is not null)
        {
            conditions.Add($"p.price_type = {{{parameters.Count}}}");
            parameters.Add(query.PriceType);
        }

        var where = string.Join(" AND ", conditions);

        var sql = $"""
                   SELECT DISTINCT ON (p.variant_id, p.source, p.price_type)
                       p.variant_id, p.slug, p.source, p.price_type,
                       p.price, p.currency, p.volume, p.recorded_at
                   FROM price_snapshots p
                   INNER JOIN variants v ON v.id = p.variant_id
                   INNER JOIN items i ON i.id = v.item_id
                   WHERE {where}
                   ORDER BY p.variant_id, p.source, p.price_type, p.recorded_at DESC
                   """;

        return await _db.Database
            .SqlQueryRaw<SkinPrice>(sql, parameters.ToArray())
            .ToListAsync(ct);
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
                             "COPY price_snapshots_staging (id, variant_id, slug, source, price_type, price, currency, volume, recorded_at) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                foreach (var p in deduped)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(Guid.NewGuid(), NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(p.VariantId, NpgsqlDbType.Uuid, ct);
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

            // Step 3 — Upsert from staging
            await using (var upsertCmd = conn.CreateCommand())
            {
                upsertCmd.Transaction = transaction;
                upsertCmd.CommandText = """
                                        INSERT INTO price_snapshots (id, variant_id, slug, source, price_type, price, currency, volume, recorded_at)
                                        SELECT id, variant_id, slug, source, price_type, price, currency, volume, recorded_at
                                        FROM price_snapshots_staging
                                        ON CONFLICT (variant_id, source, price_type, recorded_at) DO NOTHING
                                        """;

                var inserted = await upsertCmd.ExecuteNonQueryAsync(ct);
                var skipped = deduped.Count - inserted;

                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "BulkUpsertPrices complete — {Total} processed ({Inserted} inserted, {Skipped} skipped)",
                    deduped.Count, inserted, skipped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkUpsertPrices failed — rolling back");
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}