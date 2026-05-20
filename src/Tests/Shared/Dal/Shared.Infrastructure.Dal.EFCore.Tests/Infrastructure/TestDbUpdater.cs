using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class TestDbUpdater(
    DbContext dbContext)
    : DbUpdaterBase(dbContext)
{
    public bool MigrateWasCalled { get; private set; }

    public override void Migrate()
    {
        MigrateWasCalled = true;
        base.Migrate();
    }
}
