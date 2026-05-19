using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestEfEntity> TestEntities => Set<TestEfEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEfEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }
}
