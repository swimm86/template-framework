using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Тестовая реализация <see cref="IEnsureSchemaStrategy"/>, подсчитывающая вызовы
/// и возвращающая заданный результат. Используется для unit-тестов <c>DbUpdaterBase</c>,
/// которые проверяют делегирование, а не реальную работу с базой данных.
/// </summary>
internal sealed class StubEnsureSchemaStrategy : IEnsureSchemaStrategy
{
    /// <summary>
    /// Значение, возвращаемое методом <see cref="EnsureSchemaIfNeeded"/>.
    /// </summary>
    public bool ReturnValue { get; set; } = true;

    /// <summary>
    /// Количество вызовов <see cref="EnsureSchemaIfNeeded"/>.
    /// </summary>
    public int CallCount { get; private set; }

    /// <inheritdoc />
    public bool EnsureSchemaIfNeeded()
    {
        CallCount++;
        return ReturnValue;
    }
}

/// <summary>
/// Реляционная реализация <see cref="IEnsureSchemaStrategy"/>, делегирующая
/// реальному <c>RelationalEnsureSchemaStrategy&lt;TContext&gt;</c> из <c>Shared.Infrastructure.Dal.EFCore</c>.
/// Используется в интеграционных тестах с SQLite для проверки сквозного сценария.
/// </summary>
internal sealed class TestRelationalEnsureSchemaStrategy<TContext>(
    IDbContextFactory<TContext> contextFactory)
    : IEnsureSchemaStrategy
    where TContext : DbContext
{
    public int CallCount { get; private set; }

    public bool EnsureSchemaIfNeeded()
    {
        CallCount++;
        using var context = contextFactory.CreateDbContext();
        if (context.Database.GetPendingMigrations().Any())
        {
            return false;
        }

        return context.Database.EnsureCreated();
    }
}
