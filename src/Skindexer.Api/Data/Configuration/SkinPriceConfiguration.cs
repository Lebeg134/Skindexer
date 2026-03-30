using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class SkinPriceConfiguration : IEntityTypeConfiguration<SkinPriceEntity>
{
    public void Configure(EntityTypeBuilder<SkinPriceEntity> builder)
    {
        builder.ToTable("price_snapshots");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.PriceType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.RecordedAt)
            .IsRequired();
        
        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ItemId, x.Source, x.PriceType, x.RecordedAt })
            .IsUnique();
    }
}