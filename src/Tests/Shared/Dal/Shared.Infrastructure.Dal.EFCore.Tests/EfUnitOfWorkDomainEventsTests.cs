using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Domain.Core.Enums;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты обработки доменных событий в <see cref="EfUnitOfWork{TDbContext}"/>.
/// Проверяет диспетчеризацию BeforeSave/AfterSave событий, управление настройками
/// и сброс состояния сущностей.
/// </summary>
public sealed class EfUnitOfWorkDomainEventsTests
{
    private static TestDomainEventDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDomainEventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestDomainEventDbContext(options);
    }

    private static TestEfDbSettingsDomainEvent CreateSettings(bool transactionsEnabled = false) =>
        new(transactionsEnabled);

    private static TestUnitOfWork CreateUnitOfWork(
        TestDomainEventDbContext context,
        DbSettingsBase? settings = null,
        IBeforeSaveChangesService? beforeSaveService = null)
    {
        settings ??= CreateSettings();
        return new TestUnitOfWork(context, new FakeServices(), settings, beforeSaveService);
    }

    #region BeforeSave / AfterSave dispatching

    /// <summary>Проверяет что BeforeSave события вызываются до сохранения.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_BeforeSaveEventsAreDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "test" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что AfterSave события вызываются после сохранения.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_AfterSaveEventsAreDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "test" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.AfterSaveEventProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что оба события (BeforeSave и AfterSave) вызываются ровно по одному разу.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_EachEventCalledExactlyOnce()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "once" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(1);
        entity.AfterSaveEventProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что при нескольких сущностях события вызываются для каждой.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithMultipleDomainEntities_EventsDispatchedForEach()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity1 = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "first" };
        var entity2 = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "second" };
        context.DomainEntities.Add(entity1);
        context.DomainEntities.Add(entity2);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity1.BeforeSaveEventProcessedCount.Should().Be(1);
        entity1.AfterSaveEventProcessedCount.Should().Be(1);
        entity2.BeforeSaveEventProcessedCount.Should().Be(1);
        entity2.AfterSaveEventProcessedCount.Should().Be(1);
    }

    #endregion

    #region DisableEvents — all events

    /// <summary>Проверяет что DisableEvents() предотвращает вызов BeforeSave событий.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithAllEventsDisabled_BeforeSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "no-events" };
        context.DomainEntities.Add(entity);
        uow.DisableEvents();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(0);
    }

    /// <summary>Проверяет что DisableEvents() предотвращает вызов AfterSave событий.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithAllEventsDisabled_AfterSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "no-events" };
        context.DomainEntities.Add(entity);
        uow.DisableEvents();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.AfterSaveEventProcessedCount.Should().Be(0);
    }

    #endregion

    #region DisableEvents — by entity type

    /// <summary>Проверяет что DisableEvents&lt;T&gt;() блокирует события для конкретного типа сущности.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithEntityTypeEventsDisabled_EventsNotDispatchedForThatType()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "entity-disabled" };
        context.DomainEntities.Add(entity);
        uow.DisableEvents<TestDomainEventEntity>();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(0);
        entity.AfterSaveEventProcessedCount.Should().Be(0);
    }

    /// <summary>Проверяет что DisableEvents&lt;T&gt;(DomainEventType) блокирует только BeforeSave для типа.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithBeforeSaveEventsDisabledForType_OnlyBeforeSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "before-disabled" };
        context.DomainEntities.Add(entity);
        uow.DisableEvents<TestDomainEventEntity>(DomainEventType.BeforeSave);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(0);
        entity.AfterSaveEventProcessedCount.Should().Be(1);
    }

    #endregion

    #region EnableEvents after DisableEvents

    /// <summary>Проверяет что EnableEvents&lt;T&gt;() восстанавливает диспетчеризацию для конкретного типа.</summary>
    [Fact]
    public async Task SaveChangesAsync_AfterReEnablingEvents_EventsDispatchedAgain()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "re-enabled" };
        context.DomainEntities.Add(entity);

        uow.DisableEvents<TestDomainEventEntity>();
        uow.EnableEvents<TestDomainEventEntity>();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveEventProcessedCount.Should().Be(1);
        entity.AfterSaveEventProcessedCount.Should().Be(1);
    }

    #endregion

    #region resetEventSettingsAfterSave

    /// <summary>
    /// Проверяет что resetEventSettingsAfterSave=true (по умолчанию) сбрасывает настройки
    /// событий после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_DefaultResetEventSettingsAfterSave_EventsEnabledAfterSave()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        uow.DisableEvents();
        uow.AreEventsEnabled.Should().BeFalse();

        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "reset" };
        context.DomainEntities.Add(entity);

        // Act — resetEventSettingsAfterSave defaults to true
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetEventSettingsAfterSave: true);

        // Assert — settings should be reset (enabled)
        uow.AreEventsEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет что resetEventSettingsAfterSave=false сохраняет настройки после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithResetEventSettingsAfterSaveFalse_EventsStillDisabledAfterSave()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        uow.DisableEvents();
        uow.AreEventsEnabled.Should().BeFalse();

        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "no-reset" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetEventSettingsAfterSave: false);

        // Assert — settings must NOT be reset
        uow.AreEventsEnabled.Should().BeFalse();
    }

    #endregion

    #region ResetEvents called on entities

    /// <summary>
    /// Проверяет что ResetEvents() вызывается на сущностях в блоке finally после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_AfterSuccessfulSave_ResetEventsCalledOnEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "reset-entity" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert — ResetEvents() is called on the entity in the finally block
        entity.EventsWereReset.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет что ResetEvents() вызывается на сущностях даже при ошибке сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnFailure_ResetEventsStillCalledOnEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var failingService = new TestBeforeSaveChangesService
        {
            OnProcessAsync = () => throw new InvalidOperationException("fail"),
        };
        var uow = CreateUnitOfWork(context, beforeSaveService: failingService);
        var entity = new TestDomainEventEntity { Id = Guid.NewGuid(), Name = "reset-on-fail" };
        context.DomainEntities.Add(entity);

        // Act — will throw
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false));

        // Assert — ResetEvents() must still be called (it's in finally)
        entity.EventsWereReset.Should().BeTrue();
    }

    #endregion

    #region Events disabled — no events on non-IWithDomainEvents entities

    /// <summary>
    /// Проверяет что сущности без IWithDomainEvents не вызывают диспетчеризацию событий.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNonDomainEventEntity_NoEventsDispatched()
    {
        // Arrange — use context with non-domain entity (TestEntityWithCreatedDeleted)
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new TestDbContext(options);
        var uow = new EfUnitOfWork<TestDbContext>(
            context,
            new FakeServices(),
            new TestEfDbSettings(transactionsEnabled: false));

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "no-domain" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region Helper classes

    private sealed class TestUnitOfWork(
        TestDomainEventDbContext dbContext,
        IServiceProvider serviceProvider,
        DbSettingsBase settings,
        IBeforeSaveChangesService? beforeSaveChangesService = null)
        : EfUnitOfWork<TestDomainEventDbContext>(
            dbContext,
            serviceProvider,
            settings,
            beforeSaveChangesService)
    {
        public bool AreEventsEnabled => AreAnyDomainEventsEnabled;
    }

    private sealed class TestEfDbSettingsDomainEvent
        : Settings.EfDbSettingsBase<TestDomainEventDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public TestEfDbSettingsDomainEvent(bool transactionsEnabled = false)
        {
            ConnectionString = "Server=localhost;Database=test;";
            TransactionsEnabled = transactionsEnabled;
        }
    }

    private sealed class FakeServices
        : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    #endregion
}
