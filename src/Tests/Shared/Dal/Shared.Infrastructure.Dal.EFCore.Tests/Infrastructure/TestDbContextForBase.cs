using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Infrastructure.Dal.EFCore.Conventions;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

[ManualConfiguration]
public class TestDbContextForBase(
    DbContextOptions<TestDbContextForBase> options, IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    public DbSet<TestEfEntity> TestEntities => Set<TestEfEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEfEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }
}
