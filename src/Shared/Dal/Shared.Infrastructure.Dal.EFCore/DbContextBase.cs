// ----------------------------------------------------------------------------------------------
// <copyright file="DbContextBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Conventions;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore;

/// <summary>
/// Базовый класс для <see cref="DbContext"/>.
/// </summary>
public abstract class DbContextBase(
    DbContextOptions options,
    IQueryEvaluator evaluator,
    IHostEnvironment environment
) : DbContext(options)
{
    /// <summary>
    /// Выполняет операцию.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которой будет выполнена операция.</typeparam>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Реализация операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    public TResult Execute<TEntity, TResult>(
        Func<IRepository<TEntity>, TResult> process,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        var repository = GetRepository<TEntity>();
        return repository.Execute(() => process(repository), useTransaction);
    }

    /// <summary>
    /// Выполняет операцию асинхронно.
    /// </summary>
    /// <typeparam name="TEntity"> Тип сущности, для которой будет выполнена операция. </typeparam>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Асинхрорнная реализация операции.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    public Task<TResult> ExecuteAsync<TEntity, TResult>(
        Func<IRepository<TEntity>, Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false)
        where TEntity : class, IEntity
    {
        var repository = GetRepository<TEntity>();
        return repository.ExecuteAsync(() => process(repository), token, useTransaction);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetCallingAssembly());
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new ColumnsNamesConvention());
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (environment.IsDevelopment())
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Возвращает репозиторий с сущностями типа <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности, для которого создается репозиторий.</typeparam>
    /// <returns>Репозиторий с сущностями типа <typeparamref name="TEntity"/>.</returns>
    private EfRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity =>
        new EfRepository<TEntity>(this, evaluator);
}
