using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class RarityGroupConfiguration : IEntityTypeConfiguration<RarityGroupEntity>
{
    public void Configure(EntityTypeBuilder<RarityGroupEntity> builder)
    {
        builder.ToTable("rarity_groups");

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

        builder.HasIndex(x => new { x.GameId, x.Slug })
            .IsUnique();
    }
}