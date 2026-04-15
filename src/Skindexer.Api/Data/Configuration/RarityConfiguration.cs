using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class RarityConfiguration : IEntityTypeConfiguration<RarityEntity>
{
    public void Configure(EntityTypeBuilder<RarityEntity> builder)
    {
        builder.ToTable("rarities");

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

        builder.Property(x => x.Order)
            .IsRequired(false);

        builder.HasIndex(x => new { x.GameId, x.Slug })
            .IsUnique();

        builder.HasOne(x => x.RarityGroup)
            .WithMany(x => x.Rarities)
            .HasForeignKey(x => x.RarityGroupId)
            .IsRequired(false);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Rarity)
            .HasForeignKey(x => x.RarityId)
            .IsRequired(false);
    }
}