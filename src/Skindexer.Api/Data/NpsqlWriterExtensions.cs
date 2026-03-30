using Npgsql;
using NpgsqlTypes;

namespace Skindexer.Api.Data;

public static class NpgsqlWriterExtensions
{
    public static async Task WriteNullableAsync<T>(
        this NpgsqlBinaryImporter writer, T? value, NpgsqlDbType dbType, CancellationToken ct)
        where T : struct
    {
        if (value is null) await writer.WriteNullAsync(ct);
        else await writer.WriteAsync(value.Value, dbType, ct);
    }

    public static async Task WriteNullableAsync(
        this NpgsqlBinaryImporter writer, string? value, NpgsqlDbType dbType, CancellationToken ct)
    {
        if (value is null) await writer.WriteNullAsync(ct);
        else await writer.WriteAsync(value, dbType, ct);
    }
}