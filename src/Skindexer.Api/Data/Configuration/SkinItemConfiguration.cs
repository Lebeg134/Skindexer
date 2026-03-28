using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class SkinItemConfiguration : IEntityTypeConfiguration<SkinItemEntity>
{
    public void Configure(EntityTypeBuilder<SkinItemEntity> builder)
    {
        builder.ToTable("items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GameId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.CollectionId);
        builder.Property(x => x.GradeId);

        builder.HasIndex(x => new { x.GameId, x.Slug })
            .IsUnique();

        builder.HasMany(x => x.Prices)
            .WithOne(x => x.Item)
            .HasForeignKey(x => x.ItemId);

        builder.HasOne(x => x.Collection)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CollectionId)
            .IsRequired(false);

        builder.HasOne(x => x.Grade)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.GradeId)
            .IsRequired(false);
    }
}