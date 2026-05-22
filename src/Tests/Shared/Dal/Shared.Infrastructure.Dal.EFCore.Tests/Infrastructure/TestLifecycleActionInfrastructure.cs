using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Ключи тестовых действий жизненного цикла.
/// </summary>
public enum TestEventKey
{
    BeforeSaveEvent,
    AfterSaveEvent,
}

/// <summary>
/// Тестовая реализация <see cref="IEntityLifecycleAction"/>, отслеживающая количество вызовов.
/// </summary>
public sealed class TrackingLifecycleAction(Enum key) : IEntityLifecycleAction
{
    public Enum Key { get; } = key;

    public int ProcessCallCount { get; private set; }

    public Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
    {
        ProcessCallCount++;
        return Task.CompletedTask;
    }

    public void Enable() { }

    public void Disable() { }
}

/// <summary>
/// Тестовая реализация <see cref="IEntityLifecycleAction"/> с callback при выполнении.
/// </summary>
public sealed class CallbackTrackingLifecycleAction(
    Enum key,
    Action onExecute) : IEntityLifecycleAction
{
    public Enum Key { get; } = key;

    public int ProcessCallCount { get; private set; }

    public Task ExecuteAsync(
        LifecycleHookType hookType,
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
    {
        ProcessCallCount++;
        onExecute();
        return Task.CompletedTask;
    }

    public void Enable() { }

    public void Disable() { }
}

/// <summary>
/// Тестовая сущность, реализующая <see cref="IWithLifecycleActions"/> с отслеживанием действий.
/// </summary>
public sealed class TestLifecycleActionEntity : IEntity<Guid>, IWithLifecycleActions
{
    private readonly TrackingLifecycleAction _beforeSaveAction = new(TestEventKey.BeforeSaveEvent);
    private readonly TrackingLifecycleAction _afterSaveAction = new(TestEventKey.AfterSaveEvent);

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int BeforeSaveActionProcessedCount => _beforeSaveAction.ProcessCallCount;

    public int AfterSaveActionProcessedCount => _afterSaveAction.ProcessCallCount;

    public bool ActionsWereReset { get; private set; }

    object IEntity.Id => Id;

    public string[] RequiredToSaveNavigationPropertiesNames => [];

    public bool TryGetAction(LifecycleHookType hookType, Enum key, out IEntityLifecycleAction lifecycleAction)
    {
        if (hookType == LifecycleHookType.BeforeSave && key.Equals(TestEventKey.BeforeSaveEvent))
        {
            lifecycleAction = _beforeSaveAction;
            return true;
        }

        if (hookType == LifecycleHookType.AfterSave && key.Equals(TestEventKey.AfterSaveEvent))
        {
            lifecycleAction = _afterSaveAction;
            return true;
        }

        lifecycleAction = null!;
        return false;
    }

    public void ResetActions() => ActionsWereReset = true;

    public ICollection<Enum> GetAllKeys(LifecycleHookType hookType) =>
        hookType == LifecycleHookType.BeforeSave
            ? [TestEventKey.BeforeSaveEvent]
            : [TestEventKey.AfterSaveEvent];
}

/// <summary>
/// Сущность, добавляющая другую сущность в ChangeTracker внутри BeforeSave-действия.
/// </summary>
public sealed class TestSpawningLifecycleActionEntity : IEntity<Guid>, IWithLifecycleActions
{
    private readonly CallbackTrackingLifecycleAction _beforeSaveAction;
    private readonly TrackingLifecycleAction _afterSaveAction;

    public TestSpawningLifecycleActionEntity(TestLifecycleActionDbContext context)
    {
        _beforeSaveAction = new CallbackTrackingLifecycleAction(
            TestEventKey.BeforeSaveEvent,
            () =>
            {
                SpawnedEntity = new TestLifecycleActionEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "spawned-in-before-save",
                };
                context.DomainEntities.Add(SpawnedEntity);
            });
        _afterSaveAction = new TrackingLifecycleAction(TestEventKey.AfterSaveEvent);
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TestLifecycleActionEntity? SpawnedEntity { get; private set; }

    public int BeforeSaveActionProcessedCount => _beforeSaveAction.ProcessCallCount;

    public int AfterSaveActionProcessedCount => _afterSaveAction.ProcessCallCount;

    object IEntity.Id => Id;

    public string[] RequiredToSaveNavigationPropertiesNames => [];

    public bool TryGetAction(LifecycleHookType hookType, Enum key, out IEntityLifecycleAction lifecycleAction)
    {
        if (hookType == LifecycleHookType.BeforeSave && key.Equals(TestEventKey.BeforeSaveEvent))
        {
            lifecycleAction = _beforeSaveAction;
            return true;
        }

        if (hookType == LifecycleHookType.AfterSave && key.Equals(TestEventKey.AfterSaveEvent))
        {
            lifecycleAction = _afterSaveAction;
            return true;
        }

        lifecycleAction = null!;
        return false;
    }

    public void ResetActions()
    {
    }

    public ICollection<Enum> GetAllKeys(LifecycleHookType hookType) =>
        hookType == LifecycleHookType.BeforeSave
            ? [TestEventKey.BeforeSaveEvent]
            : [TestEventKey.AfterSaveEvent];
}

/// <summary>
/// DbContext для тестов действий жизненного цикла.
/// Помечен <see cref="ManualConfigurationAttribute"/> — не регистрируется автоматически.
/// </summary>
[ManualConfiguration]
public sealed class TestLifecycleActionDbContext(
    DbContextOptions<TestLifecycleActionDbContext> options)
    : DbContextBase(options, new FakeHostEnvironment())
{
    public DbSet<TestLifecycleActionEntity> DomainEntities => Set<TestLifecycleActionEntity>();

    public DbSet<TestSpawningLifecycleActionEntity> SpawningEntities => Set<TestSpawningLifecycleActionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestLifecycleActionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Ignore(x => x.BeforeSaveActionProcessedCount);
            e.Ignore(x => x.AfterSaveActionProcessedCount);
            e.Ignore(x => x.ActionsWereReset);
            e.Ignore(x => x.RequiredToSaveNavigationPropertiesNames);
        });

        modelBuilder.Entity<TestSpawningLifecycleActionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Ignore(x => x.BeforeSaveActionProcessedCount);
            e.Ignore(x => x.AfterSaveActionProcessedCount);
            e.Ignore(x => x.SpawnedEntity);
            e.Ignore(x => x.RequiredToSaveNavigationPropertiesNames);
        });
    }
}
