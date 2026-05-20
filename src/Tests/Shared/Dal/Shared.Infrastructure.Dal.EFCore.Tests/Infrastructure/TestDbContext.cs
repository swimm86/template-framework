using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.DependencyInjection.Attributes;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Тестовый DbContext наследующий DbContextBase для корректной работы EfUnitOfWork.
/// </summary>
[ManualConfiguration]
public sealed class TestDbContext
    : DbContextBase
{
    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        IHostEnvironment environment)
        : base(options, environment)
    {
    }

    public TestDbContext(
        DbContextOptions<TestDbContext> options)
        : base(options, new FakeHostEnvironment())
    {
    }

    public DbSet<TestEntityWithCreatedDeleted> Entities => Set<TestEntityWithCreatedDeleted>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntityWithCreatedDeleted>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
