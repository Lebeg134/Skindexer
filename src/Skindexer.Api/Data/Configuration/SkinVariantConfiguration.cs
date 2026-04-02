using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class SkinVariantConfiguration : IEntityTypeConfiguration<SkinVariantEntity>
{
    public void Configure(EntityTypeBuilder<SkinVariantEntity> builder)
    {
        builder.ToTable("variants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.GameId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasOne(x => x.Item)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Prices)
            .WithOne(x => x.Variant)
            .HasForeignKey(x => x.VariantId);
    }
}