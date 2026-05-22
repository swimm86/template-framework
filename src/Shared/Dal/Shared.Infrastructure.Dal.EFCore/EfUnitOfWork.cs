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
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.LifecycleAction.Settings;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore;

/// <inheritdoc />
public class EfUnitOfWork<TDbContext>
    : IUnitOfWork
    where TDbContext : DbContextBase
{
    private readonly LifecycleActionSettings _lifecycleActionSettings = new();

    private EntityEntry<IWithLifecycleActions>[] EntriesWithLifecycleActions =>
        DbContext.ChangeTracker.Entries<IWithLifecycleActions>().ToArray();

    /// <summary>
    /// DbContext.
    /// </summary>
    protected readonly TDbContext DbContext;

    /// <summary>
    /// <inheritdoc cref="IServiceProvider"/>.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <inheritdoc cref="IBeforeSaveChangesService"/>.
    /// </summary>
    private readonly IBeforeSaveChangesService? _beforeSaveChangesService;

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
    /// Признак того, что включено хотя бы одно действие перехвата жизненного цикла.
    /// </summary>
    protected bool AreAnyLifecycleActionsEnabled => _lifecycleActionSettings.AnyEnabled;

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="dbContext"><see cref="TDbContext"/>.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="settings">Настройки.</param>
    /// <param name="beforeSaveChangesService"><see cref="IBeforeSaveChangesService"/>.</param>
    public EfUnitOfWork(
        TDbContext dbContext,
        IServiceProvider serviceProvider,
        DbSettingsBase settings,
        IBeforeSaveChangesService? beforeSaveChangesService = null)
    {
        DbContext = dbContext;
        _beforeSaveChangesService = beforeSaveChangesService;
        _serviceProvider = serviceProvider;

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
        var entryTypeGroups = EntriesWithLifecycleActions
            .GroupBy(e => e.Entity.GetType())
            .ToArray();

        try
        {
            await ProcessLifecycleActionsAsync(
                entryTypeGroups,
                LifecycleHookType.BeforeSave,
                cancellationToken);

            await ProcessBeforeSaveChangesActionsAsync(cancellationToken);

            var result = await DbContext.SaveChangesAsync(cancellationToken);

            if (commitTransaction)
            {
                await CommitTransactionAsync(cancellationToken);
            }

            await ProcessLifecycleActionsAsync(
                entryTypeGroups,
                LifecycleHookType.AfterSave,
                cancellationToken);

            return result;
        }
        catch
        {
            if (commitTransaction)
            {
                await RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (_lifecycleActionSettings.AnyEnabled)
            {
                entryTypeGroups.SelectMany(x => x).ForEach(e => e.Entity.ResetActions());
            }

            if (resetLifecycleActionSettingsAfterSave)
            {
                ResetLifecycleActionSettings();
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
    public IUnitOfWork DisableLifecycleActions() =>
        SwitchLifecycleActions(false);

    /// <inheritdoc />
    public IUnitOfWork EnableLifecycleActions() =>
        SwitchLifecycleActions(true);

    /// <inheritdoc />
    public IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType? hookType = null)
        where TEntity : IEntity, IWithLifecycleActions =>
        SwitchLifecycleActions<TEntity>(hookType, false);

    /// <inheritdoc />
    public IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType? hookType = null)
        where TEntity : IEntity, IWithLifecycleActions =>
        SwitchLifecycleActions<TEntity>(hookType, true);

    /// <inheritdoc />
    public IUnitOfWork DisableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions =>
        SwitchLifecycleActions<TEntity>(hookType, actionKeyFlags, false);

    /// <inheritdoc />
    public IUnitOfWork EnableLifecycleActions<TEntity>(LifecycleHookType hookType, Enum actionKeyFlags)
        where TEntity : IEntity, IWithLifecycleActions =>
        SwitchLifecycleActions<TEntity>(hookType, actionKeyFlags, true);

    /// <inheritdoc />
    public IUnitOfWork ResetLifecycleActionSettings()
    {
        EnableLifecycleActions();

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

    private IUnitOfWork SwitchLifecycleActions(bool enable)
    {
        _lifecycleActionSettings.Switch(enable);

        return this;
    }

    private IUnitOfWork SwitchLifecycleActions<TEntity>(LifecycleHookType? hookType, bool enable)
        where TEntity : IEntity, IWithLifecycleActions
    {
        if (hookType.HasValue)
        {
            _lifecycleActionSettings.Switch(typeof(TEntity), hookType.Value, enable);
        }
        else
        {
            _lifecycleActionSettings.Switch(typeof(TEntity), enable);
        }

        return this;
    }

    private IUnitOfWork SwitchLifecycleActions<TEntity>(LifecycleHookType hookType, Enum flags, bool enable)
        where TEntity : IEntity, IWithLifecycleActions
    {
        _lifecycleActionSettings.Switch(typeof(TEntity), hookType, flags, enable);

        return this;
    }

    private Task ProcessBeforeSaveChangesActionsAsync(CancellationToken cancellationToken) =>
        _beforeSaveChangesService is not null
            ? _beforeSaveChangesService.ProcessAsync(DbContext, cancellationToken)
            : Task.CompletedTask;

    private async Task ProcessLifecycleActionsAsync(
        IGrouping<Type, EntityEntry<IWithLifecycleActions>>[] entryGroups,
        LifecycleHookType hookType,
        CancellationToken cancellationToken)
    {
        if (!_lifecycleActionSettings.AnyEnabled)
        {
            return;
        }

        foreach (var entryGroup in entryGroups)
        {
            if (!_lifecycleActionSettings.AnyElementEnabled(entryGroup.Key, hookType))
            {
                continue;
            }

            await ProcessLifecycleActionsAsync(entryGroup, hookType, cancellationToken);
        }
    }

    private async Task ProcessLifecycleActionsAsync(
        IGrouping<Type, EntityEntry<IWithLifecycleActions>> typeEntryGroup,
        LifecycleHookType hookType,
        CancellationToken cancellationToken)
    {
        var entities = typeEntryGroup.Select(e => e.Entity).ToArray();

        var keys = entities
            .SelectMany(x => x.GetAllKeys(hookType))
            .Distinct()
            .Where(key => _lifecycleActionSettings.AnyElementEnabled(typeEntryGroup.Key, hookType, key))
            .ToArray();

        if (!keys.Any())
        {
            return;
        }

        await IncludeRequiredNavigationPropertiesAsync(typeEntryGroup, entities, cancellationToken);

        foreach (var key in keys)
        {
            await entities.ForeachAsync(
                x => x.ProcessLifecycleActionAsync(
                    hookType,
                    key,
                    _serviceProvider,
                    entities,
                    cancellationToken),
                cancellationToken);
        }
    }

    private Task IncludeRequiredNavigationPropertiesAsync(
        IGrouping<Type, EntityEntry<IWithLifecycleActions>> typeEntryGroup,
        IWithLifecycleActions[] entities,
        CancellationToken cancellationToken)
    {
        // Получаем обязательные для действий перехвата незагруженные навигационные свойства.
        var navigationGroups = typeEntryGroup
            .SelectMany(entry => entry.Navigations
                .Where(nav =>
                    entry.Entity.RequiredToSaveNavigationPropertiesNames.Contains(nav.Metadata.Name)
                    && !nav.IsLoaded))
            .GroupBy(nav => nav.Metadata.Name)
            .ToArray();

        // Загружаем навигационные свойства пачками: по запросу на каждое навигационное свойство для всего типа.
        return navigationGroups.ForeachAsync(
            async navigationGroup =>
            {
                var navigation = navigationGroup.First();

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
        IEnumerable<IWithLifecycleActions> entities,
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
        IEnumerable<IWithLifecycleActions> entities,
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
