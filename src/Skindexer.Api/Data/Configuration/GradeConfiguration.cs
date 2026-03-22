using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data.Configuration;

public class GradeConfiguration : IEntityTypeConfiguration<GradeEntity>
{
    public void Configure(EntityTypeBuilder<GradeEntity> builder)
    {
        builder.ToTable("grades");

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
            .IsRequired();

        builder.HasIndex(x => new { x.GameId, x.Slug })
            .IsUnique();

        builder.HasIndex(x => new { x.GameId, x.Order })
            .IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Grade)
            .HasForeignKey(x => x.GradeId)
            .IsRequired(false);
    }
}