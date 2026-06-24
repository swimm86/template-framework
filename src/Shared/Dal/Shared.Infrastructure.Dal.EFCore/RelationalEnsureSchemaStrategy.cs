// ----------------------------------------------------------------------------------------------
// <copyright file="RelationalEnsureSchemaStrategy.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbUpdater.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Реляционная реализация <see cref="IEnsureSchemaStrategy"/>, использующая
/// реляционные методы EF Core (<c>GetPendingMigrations</c>, <c>EnsureCreated</c>).
/// </summary>
/// <typeparam name="TContext">Тип <see cref="DbContext"/>, для которого инициализируется схема.</typeparam>
/// <param name="contextFactory">Фабрика экземпляров <typeparamref name="TContext"/>.</param>
public sealed class RelationalEnsureSchemaStrategy<TContext>(
    IDbContextFactory<TContext> contextFactory)
    : IEnsureSchemaStrategy
    where TContext : DbContext
{
    /// <inheritdoc />
    public bool EnsureSchemaIfNeeded()
    {
        using var context = contextFactory.CreateDbContext();
        if (context.Database.GetPendingMigrations().Any())
        {
            return false;
        }

        return context.Database.EnsureCreated();
    }
}
