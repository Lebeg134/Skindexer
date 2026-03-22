using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data.Entities;

namespace Skindexer.Api.Data;

public class SkindexerDbContext : DbContext
{
    public SkindexerDbContext(DbContextOptions<SkindexerDbContext> options) : base(options) { }

    public DbSet<SkinItemEntity> Items => Set<SkinItemEntity>();
    public DbSet<SkinPriceEntity> Prices => Set<SkinPriceEntity>();
    public DbSet<CollectionEntity> Collections => Set<CollectionEntity>();
    public DbSet<GradeEntity> Grades => Set<GradeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkindexerDbContext).Assembly);
    }
}