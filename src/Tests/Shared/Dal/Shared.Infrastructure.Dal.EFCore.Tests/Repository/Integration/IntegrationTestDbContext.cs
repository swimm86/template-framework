using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

public sealed class IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options)
    : DbContext(options)
{
    public DbSet<TestEntityWithCreatedDeleted> Entities => Set<TestEntityWithCreatedDeleted>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntityWithCreatedDeleted>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(256);
        });
    }
}
