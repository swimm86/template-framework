using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public class TestIncludeDbContext : DbContext
{
    public TestIncludeDbContext(DbContextOptions<TestIncludeDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestParentEntity> Parents => Set<TestParentEntity>();
    public DbSet<TestChildEntity> Children => Set<TestChildEntity>();
    public DbSet<TestGrandChildEntity> GrandChildren => Set<TestGrandChildEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestParentEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.Child)
                .WithOne()
                .HasForeignKey<TestParentEntity>(x => x.ChildId);
            e.HasMany(x => x.Children)
                .WithOne(x => x.Parent)
                .HasForeignKey(x => x.ParentId);
        });

        modelBuilder.Entity<TestChildEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.GrandChild)
                .WithOne()
                .HasForeignKey<TestChildEntity>(x => x.GrandChildId);
        });

        modelBuilder.Entity<TestGrandChildEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
