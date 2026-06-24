using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Простейшая синхронная реализация <see cref="IDbContextFactory{TContext}"/>,
/// создающая контекст с предопределёнными опциями. Используется в тестах,
/// где требуется <see cref="IDbContextFactory{TContext}"/>, но не нужен
/// полноценный DI-контейнер.
/// </summary>
/// <typeparam name="TContext">Тип создаваемого <see cref="DbContext"/>.</typeparam>
internal sealed class TestDbContextFactory<TContext>(
    DbContextOptions<TContext> options)
    : IDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <inheritdoc />
    public TContext CreateDbContext() => (TContext)Activator.CreateInstance(typeof(TContext), options)!;
}
