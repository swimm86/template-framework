using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Event.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Ключи тестовых доменных событий.
/// </summary>
public enum TestEventKey
{
    BeforeSaveEvent,
    AfterSaveEvent,
}

/// <summary>
/// Тестовая реализация <see cref="IDomainEvent"/>, отслеживающая количество вызовов.
/// </summary>
public sealed class TrackingDomainEvent(Enum key) : IDomainEvent
{
    public Enum Key { get; } = key;

    public int ProcessCallCount { get; private set; }

    public Task ProcessAsync(
        DomainEventType eventType,
        IServiceProvider serviceProvider,
        ICollection<IWithDomainEvents> entities,
        CancellationToken cancellationToken)
    {
        ProcessCallCount++;
        return Task.CompletedTask;
    }

    public void Enable() { }

    public void Disable() { }
}

/// <summary>
/// Тестовая сущность, реализующая <see cref="IWithDomainEvents"/> с отслеживанием событий.
/// </summary>
public sealed class TestDomainEventEntity : IEntity<Guid>, IWithDomainEvents
{
    private readonly TrackingDomainEvent _beforeSaveEvent = new(TestEventKey.BeforeSaveEvent);
    private readonly TrackingDomainEvent _afterSaveEvent = new(TestEventKey.AfterSaveEvent);

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int BeforeSaveEventProcessedCount => _beforeSaveEvent.ProcessCallCount;

    public int AfterSaveEventProcessedCount => _afterSaveEvent.ProcessCallCount;

    public bool EventsWereReset { get; private set; }

    object IEntity.Id => Id;

    public string[] RequiredToSaveNavigationPropertiesNames => [];

    public bool TryGetEvent(DomainEventType domainEventType, Enum key, out IDomainEvent domainEvent)
    {
        if (domainEventType == DomainEventType.BeforeSave && key.Equals(TestEventKey.BeforeSaveEvent))
        {
            domainEvent = _beforeSaveEvent;
            return true;
        }

        if (domainEventType == DomainEventType.AfterSave && key.Equals(TestEventKey.AfterSaveEvent))
        {
            domainEvent = _afterSaveEvent;
            return true;
        }

        domainEvent = null!;
        return false;
    }

    public void ResetEvents() => EventsWereReset = true;

    public ICollection<Enum> GetAllKeys(DomainEventType domainEventType) =>
        domainEventType == DomainEventType.BeforeSave
            ? [TestEventKey.BeforeSaveEvent]
            : [TestEventKey.AfterSaveEvent];
}

/// <summary>
/// DbContext для тестов доменных событий.
/// Помечен <see cref="ManualConfigurationAttribute"/> — не регистрируется автоматически.
/// </summary>
[ManualConfiguration]
public sealed class TestDomainEventDbContext(
    DbContextOptions<TestDomainEventDbContext> options)
    : DbContextBase(options, new FakeHostEnvironment())
{
    public DbSet<TestDomainEventEntity> DomainEntities => Set<TestDomainEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestDomainEventEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Ignore(x => x.BeforeSaveEventProcessedCount);
            e.Ignore(x => x.AfterSaveEventProcessedCount);
            e.Ignore(x => x.EventsWereReset);
            e.Ignore(x => x.RequiredToSaveNavigationPropertiesNames);
        });
    }
}
