using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Тестовая реализация <see cref="DbUpdaterBase"/>, фиксирующая вызов метода <see cref="Migrate"/>.
/// </summary>
public sealed class TestDbUpdater(
    DbContext dbContext,
    IEnsureSchemaStrategy ensureSchemaStrategy)
    : DbUpdaterBase(dbContext, ensureSchemaStrategy)
{
    public bool MigrateWasCalled { get; private set; }

    public override void Migrate()
    {
        MigrateWasCalled = true;
        base.Migrate();
    }
}
