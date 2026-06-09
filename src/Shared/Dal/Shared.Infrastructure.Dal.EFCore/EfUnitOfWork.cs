// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWork.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <inheritdoc />
public class EfUnitOfWork<TDbContext>
    : IUnitOfWork
    where TDbContext : DbContextBase
{
    /// <summary>
    /// DbContext.
    /// </summary>
    protected readonly TDbContext DbContext;

    /// <inheritdoc cref="IServiceProvider"/>.
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc cref="IBeforeSaveChangesService"/>
    private readonly IBeforeSaveChangesService? _beforeSaveChangesService;

    /// <inheritdoc cref="ILifecycleActionOrchestrator"/>
    private readonly ILifecycleActionOrchestrator _lifecycleActionOrchestrator;

    /// <summary>
    /// Признак того, что необходимо использовать транзакцию.
    /// </summary>
    private bool _useTransaction;

    /// <summary>
    /// Признак того, что необходимо использовать транзакцию.
    /// </summary>
    protected bool UseTransaction => _useTransaction;

    /// <summary>
    /// Текущая транзакция.
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Текущая транзакция.
    /// </summary>
    protected IDbContextTransaction? CurrentDbTransaction => _currentTransaction;

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="dbContext"><see cref="TDbContext"/>.</param>
    /// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
    /// <param name="settings">Настройки.</param>
    /// <param name="lifecycleActionOrchestrator"><inheritdoc cref="ILifecycleActionOrchestrator" path="/summary"/></param>
    /// <param name="beforeSaveChangesService"><inheritdoc cref="IBeforeSaveChangesService" path="/summary"/></param>
    public EfUnitOfWork(
        TDbContext dbContext,
        IServiceProvider serviceProvider,
        DbSettingsBase settings,
        ILifecycleActionOrchestrator lifecycleActionOrchestrator,
        IBeforeSaveChangesService? beforeSaveChangesService = null)
    {
        ArgumentNullException.ThrowIfNull(lifecycleActionOrchestrator);

        DbContext = dbContext;
        _serviceProvider = serviceProvider;
        _lifecycleActionOrchestrator = lifecycleActionOrchestrator;
        _beforeSaveChangesService = beforeSaveChangesService;

        DbContext.ChangeTracker.Tracked += OnEntityTracked;
        DbContext.ChangeTracker.StateChanged += OnEntityStateChanged;

        _useTransaction = DbContext.Database.CanConnect() && settings.TransactionsEnabled;
        if (_useTransaction)
        {
            EnableTransaction();
        }
    }

    /// <inheritdoc />
    public int SaveChanges(
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true)
    {
        return SaveChangesAsync(
                cancellationToken: CancellationToken.None,
                commitTransaction,
                resetLifecycleActionSettingsAfterSave)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken,
        bool commitTransaction = true,
        bool resetLifecycleActionSettingsAfterSave = true)
    {
        try
        {
            await IncludeRequiredNavigationPropertiesAsync(cancellationToken);
            await _lifecycleActionOrchestrator.DispatchAsync(
                LifecyclePhase.BeforeSave,
                cancellationToken);

            await ProcessBeforeSaveChangesActionsAsync(cancellationToken);

            var result = await DbContext.SaveChangesAsync(cancellationToken);

            if (commitTransaction)
            {
                await CommitTransactionAsync(cancellationToken);
            }

            await _lifecycleActionOrchestrator.DispatchAsync(
                LifecyclePhase.AfterSave,
                cancellationToken);

            return result;
        }
        catch
        {
            if (commitTransaction)
            {
                await RollbackTransactionAsync(cancellationToken);
                ClearTracking();
            }

            throw;
        }
        finally
        {
            if (resetLifecycleActionSettingsAfterSave)
            {
                _lifecycleActionOrchestrator.ResetAllActions();
            }

            if (commitTransaction)
            {
                await ResetTransactionAsync(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public void ClearTracking()
    {
        DbContext.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntity =>
        _serviceProvider.GetRequiredService<IRepository<TEntity>>();

    /// <inheritdoc />
    public IUnitOfWork EnableTransaction()
    {
        _useTransaction = true;
        StartTransaction();

        return this;
    }

    /// <inheritdoc />
    public IUnitOfWork DisableTransaction()
    {
        _useTransaction = false;
        DisposeTransaction();

        return this;
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken token)
    {
        ThrowIfTransactionOperationInvalid("commit");

        await _currentTransaction!.CommitAsync(token);
        await ResetTransactionAsync(token);
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken token)
    {
        ThrowIfTransactionOperationInvalid("rollback");

        await _currentTransaction!.RollbackAsync(token);
        await ResetTransactionAsync(token);
    }

    /// <inheritdoc />
    public Task ResetTransactionAsync(CancellationToken token)
    {
        if (_useTransaction)
        {
            DisposeTransaction();
            StartTransaction();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeTransaction();
    }

    private Task ProcessBeforeSaveChangesActionsAsync(CancellationToken cancellationToken) =>
        _beforeSaveChangesService is not null
            ? _beforeSaveChangesService.ProcessAsync(DbContext, cancellationToken)
            : Task.CompletedTask;

    /// <summary>
    /// Обработчик события отслеживания новой сущности в ChangeTracker.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        if (e.Entry.Entity is IEntity entity)
        {
            _lifecycleActionOrchestrator.AddEntities([entity]);
        }
    }

    /// <summary>
    /// Обработчик события изменения состояния сущности в ChangeTracker.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnEntityStateChanged(
        object? sender,
        EntityStateChangedEventArgs e)
    {
        if (e.Entry is { State: EntityState.Detached, Entity: IEntity entity })
        {
            _lifecycleActionOrchestrator.RemoveEntities([entity]);
        }
    }

    /// <summary>
    /// Загружает необходимые навигационные свойства для всех отслеживаемых сущностей.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    private async Task IncludeRequiredNavigationPropertiesAsync(
        CancellationToken cancellationToken)
    {
        foreach (var entryGroup in DbContext.ChangeTracker.Entries<IEntity>().GroupBy(x => x.Entity.GetType()))
        {
            var requiredNavigationNames = _lifecycleActionOrchestrator.GetRequiredProperties(entryGroup.Key);
            await IncludeRequiredNavigationPropertiesAsync(
                entryGroup,
                requiredNavigationNames,
                cancellationToken);
        }
    }

    /// <summary>
    /// Загружает указанные навигационные свойства для группы сущностей одного типа.
    /// </summary>
    /// <param name="typeEntryGroup">Группа сущностей одного типа.</param>
    /// <param name="requiredNavigationNames">Имена навигационных свойств для загрузки.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения асинхронной операции.</returns>
    private Task IncludeRequiredNavigationPropertiesAsync(
        IGrouping<Type, EntityEntry<IEntity>> typeEntryGroup,
        string[] requiredNavigationNames,
        CancellationToken cancellationToken)
    {
        var navigationGroups = typeEntryGroup
            .SelectMany(entry => entry.Navigations
                .Where(nav =>
                    requiredNavigationNames.Contains(nav.Metadata.Name)
                    && !nav.IsLoaded))
            .GroupBy(nav => nav.Metadata.Name)
            .ToArray();

        // Загружаем навигационные свойства пачками: по запросу на каждое навигационное свойство для всего типа.
        return navigationGroups.ForeachAsync(
            async navigationGroup =>
            {
                var navigation = navigationGroup.First();

                var entities = typeEntryGroup.Select(x => x.Entity).ToArray();
                await IncludeNavigationPropertyCollectionByTypeAsync(
                    typeEntryGroup.Key,
                    navigation.Metadata.ClrType,
                    navigation.Metadata.Name,
                    entities,
                    cancellationToken);

                navigationGroup.ForEach(nav => nav.IsLoaded = true);
            },
            cancellationToken);
    }

    private Task IncludeNavigationPropertyCollectionByTypeAsync(
        Type type,
        Type navType,
        string name,
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken)
    {
        var method = GetType()
            .GetMethod(
                nameof(IncludeNavigationPropertyCollectionAsync),
                BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(type, navType);

        return (method.Invoke(this, parameters: [type, name, entities, cancellationToken]) as Task)!;
    }

    private Task IncludeNavigationPropertyCollectionAsync<TEntity, TNavigation>(
        Type type,
        string name,
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entityCollection = entities.OfType<TEntity>().ToArray();
        var param = Expression.Parameter(type, "x");
        var prop = Expression.Property(param, name);
        var selector = Expression.Lambda<Func<TEntity, TNavigation>>(prop, param);

        return DbContext.Set<TEntity>()
            .AsTracking()
            .Where(x => entityCollection.Contains(x))
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    private void StartTransaction()
    {
        _currentTransaction =
            DbContext.Database.CurrentTransaction ??
            DbContext.Database.BeginTransaction();
    }

    private void DisposeTransaction()
    {
        try
        {
            _currentTransaction?.Dispose();
        }
        finally
        {
            _currentTransaction = null;
        }
    }

    private void ThrowIfTransactionOperationInvalid(string operation)
    {
        if (_useTransaction && _currentTransaction != null)
        {
            return;
        }

        var reason = _useTransaction ? "not initialized" : "disabled";
        throw new InvalidOperationException($"Can't {operation} {reason} transaction");
    }
}
