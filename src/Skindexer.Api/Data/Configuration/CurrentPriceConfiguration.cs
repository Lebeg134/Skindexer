using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class CurrentPriceConfiguration : IEntityTypeConfiguration<CurrentPriceEntity>
{
    public void Configure(EntityTypeBuilder<CurrentPriceEntity> builder)
    {
        builder.ToTable("current_prices");

        builder.HasKey(e => new { e.VariantId, e.Source, e.PriceType });

        builder.Property(e => e.GameId).HasMaxLength(64);
        builder.Property(e => e.Slug).HasMaxLength(256);
        builder.Property(e => e.Source).HasMaxLength(128);
        builder.Property(e => e.PriceType).HasMaxLength(64);
        builder.Property(e => e.Price).HasPrecision(18, 8);
        builder.Property(e => e.Currency).HasMaxLength(16);

        builder.HasIndex(e => e.GameId);
    }
}