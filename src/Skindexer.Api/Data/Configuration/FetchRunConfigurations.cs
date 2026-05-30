using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class FetchRunConfiguration : IEntityTypeConfiguration<FetchRunEntity>
{
    public void Configure(EntityTypeBuilder<FetchRunEntity> builder)
    {
        builder.ToTable("fetch_runs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FetcherId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.TriggeredBy)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false);

        builder.HasIndex(x => new { x.FetcherId, x.StartedAt });
    }
}