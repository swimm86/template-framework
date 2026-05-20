using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

[ManualConfiguration]
public sealed class IntegrationTestUnitOfWorkDbContext(
    DbContextOptions<IntegrationTestUnitOfWorkDbContext> options,
    IHostEnvironment environment)
    : DbContextBase(options, environment)
{
    public DbSet<TestEntityWithCreatedDeleted> Entities => Set<TestEntityWithCreatedDeleted>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntityWithCreatedDeleted>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(256);
        });
    }
}
