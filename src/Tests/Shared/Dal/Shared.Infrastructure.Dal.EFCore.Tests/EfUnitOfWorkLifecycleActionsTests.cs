using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты обработки действий жизненного цикла в <see cref="EfUnitOfWork{TDbContext}"/>.
/// Проверяет диспетчеризацию BeforeSave/AfterSave действий, управление настройками
/// и сброс состояния сущностей.
/// </summary>
public sealed class EfUnitOfWorkLifecycleActionsTests
{
    private static TestLifecycleActionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestLifecycleActionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestLifecycleActionDbContext(options);
    }

    private static TestEfDbSettingsLifecycleAction CreateSettings(bool transactionsEnabled = false) =>
        new(transactionsEnabled);

    private static TestUnitOfWork CreateUnitOfWork(
        TestLifecycleActionDbContext context,
        DbSettingsBase? settings = null,
        IBeforeSaveChangesService? beforeSaveService = null)
    {
        settings ??= CreateSettings();
        return new TestUnitOfWork(context, new FakeServices(), settings, beforeSaveService);
    }

    #region BeforeSave / AfterSave dispatching

    /// <summary>Проверяет что BeforeSave действия вызываются до сохранения.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithLifecycleActions_BeforeSaveActionsAreDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "test" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что AfterSave действия вызываются после сохранения.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithLifecycleActions_AfterSaveActionsAreDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "test" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.AfterSaveActionProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что оба действия (BeforeSave и AfterSave) вызываются ровно по одному разу.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithLifecycleActions_EachActionCalledExactlyOnce()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "once" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(1);
        entity.AfterSaveActionProcessedCount.Should().Be(1);
    }

    /// <summary>Проверяет что при нескольких сущностях действия вызываются для каждой.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithMultipleDomainEntities_ActionsDispatchedForEach()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity1 = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "first" };
        var entity2 = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "second" };
        context.DomainEntities.Add(entity1);
        context.DomainEntities.Add(entity2);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity1.BeforeSaveActionProcessedCount.Should().Be(1);
        entity1.AfterSaveActionProcessedCount.Should().Be(1);
        entity2.BeforeSaveActionProcessedCount.Should().Be(1);
        entity2.AfterSaveActionProcessedCount.Should().Be(1);
    }

    #endregion

    #region DisableLifecycleActions — all actions

    /// <summary>Проверяет что DisableLifecycleActions() предотвращает вызов BeforeSave действий.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithAllLifecycleActionsDisabled_BeforeSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "no-actions" };
        context.DomainEntities.Add(entity);
        uow.DisableLifecycleActions();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(0);
    }

    /// <summary>Проверяет что DisableLifecycleActions() предотвращает вызов AfterSave действий.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithAllLifecycleActionsDisabled_AfterSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "no-actions" };
        context.DomainEntities.Add(entity);
        uow.DisableLifecycleActions();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.AfterSaveActionProcessedCount.Should().Be(0);
    }

    #endregion

    #region DisableLifecycleActions — by entity type

    /// <summary>Проверяет что DisableLifecycleActions&lt;T&gt;() блокирует действия для конкретного типа сущности.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithEntityTypeLifecycleActionsDisabled_ActionsNotDispatchedForThatType()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "entity-disabled" };
        context.DomainEntities.Add(entity);
        uow.DisableLifecycleActions<TestLifecycleActionEntity>();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(0);
        entity.AfterSaveActionProcessedCount.Should().Be(0);
    }

    /// <summary>Проверяет что DisableLifecycleActions&lt;T&gt;(LifecycleHookType) блокирует только BeforeSave для типа.</summary>
    [Fact]
    public async Task SaveChangesAsync_WithBeforeSaveLifecycleActionsDisabledForType_OnlyBeforeSaveNotDispatched()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "before-disabled" };
        context.DomainEntities.Add(entity);
        uow.DisableLifecycleActions<TestLifecycleActionEntity>(LifecycleHookType.BeforeSave);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(0);
        entity.AfterSaveActionProcessedCount.Should().Be(1);
    }

    #endregion

    #region EnableLifecycleActions after DisableLifecycleActions

    /// <summary>Проверяет что EnableLifecycleActions&lt;T&gt;() восстанавливает диспетчеризацию для конкретного типа.</summary>
    [Fact]
    public async Task SaveChangesAsync_AfterReEnablingLifecycleActions_ActionsDispatchedAgain()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "re-enabled" };
        context.DomainEntities.Add(entity);

        uow.DisableLifecycleActions<TestLifecycleActionEntity>();
        uow.EnableLifecycleActions<TestLifecycleActionEntity>();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        entity.BeforeSaveActionProcessedCount.Should().Be(1);
        entity.AfterSaveActionProcessedCount.Should().Be(1);
    }

    #endregion

    #region ResetLifecycleActionSettings preserves per-type overrides

    /// <summary>
    /// Проверяет что <see cref="IUnitOfWork.ResetLifecycleActionSettings"/> полностью сбрасывает
    /// все настройки, включая per-type override от <c>DisableLifecycleActions&lt;T&gt;()</c>.
    /// </summary>
    [Fact]
    public async Task ResetLifecycleActionSettings_ClearsPerTypeOverride()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "per-type-reset" };
        context.DomainEntities.Add(entity);

        uow.DisableLifecycleActions<TestLifecycleActionEntity>();
        uow.ResetLifecycleActionSettings();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert — reset полностью очищает все per-type настройки
        entity.BeforeSaveActionProcessedCount.Should().Be(1);
        entity.AfterSaveActionProcessedCount.Should().Be(1);
    }

    #endregion

    #region resetLifecycleActionSettingsAfterSave

    /// <summary>
    /// Проверяет что resetLifecycleActionSettingsAfterSave=true (по умолчанию) сбрасывает настройки
    /// действий после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_DefaultResetLifecycleActionSettingsAfterSave_LifecycleActionsEnabledAfterSave()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        uow.DisableLifecycleActions();
        uow.AreLifecycleActionsEnabled.Should().BeFalse();

        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "reset" };
        context.DomainEntities.Add(entity);

        // Act — resetLifecycleActionSettingsAfterSave defaults to true
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetLifecycleActionSettingsAfterSave: true);

        // Assert — settings should be reset (enabled)
        uow.AreLifecycleActionsEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет что resetLifecycleActionSettingsAfterSave=false сохраняет настройки после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithResetLifecycleActionSettingsAfterSaveFalse_LifecycleActionsStillDisabledAfterSave()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        uow.DisableLifecycleActions();
        uow.AreLifecycleActionsEnabled.Should().BeFalse();

        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "no-reset" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetLifecycleActionSettingsAfterSave: false);

        // Assert — settings must NOT be reset
        uow.AreLifecycleActionsEnabled.Should().BeFalse();
    }

    #endregion

    #region entryTypeGroups snapshot limitation

    /// <summary>
    /// Подтверждает ограничение: entryTypeGroups снимается до SaveChanges,
    /// поэтому сущность, добавленная в ChangeTracker внутри BeforeSave-действия,
    /// не попадает в AfterSave-диспетчеризацию в том же вызове SaveChanges.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_EntityAddedInBeforeSaveAction_AfterSaveNotDispatchedForNewEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var parent = new TestSpawningLifecycleActionEntity(context)
        {
            Id = Guid.NewGuid(),
            Name = "parent",
        };
        context.SpawningEntities.Add(parent);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert — родитель обработан полностью
        parent.BeforeSaveActionProcessedCount.Should().Be(1);
        parent.AfterSaveActionProcessedCount.Should().Be(1);

        parent.SpawnedEntity.Should().NotBeNull();
        context.DomainEntities.Should().ContainSingle(e => e.Id == parent.SpawnedEntity!.Id);

        // Ограничение snapshot: AfterSave не вызван для сущности, добавленной в BeforeSave
        parent.SpawnedEntity!.AfterSaveActionProcessedCount.Should().Be(0);
        parent.SpawnedEntity.BeforeSaveActionProcessedCount.Should().Be(0);
    }

    #endregion

    #region ResetActions called on entities

    /// <summary>
    /// Проверяет что ResetActions() вызывается на сущностях в блоке finally после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_AfterSuccessfulSave_ResetActionsCalledOnEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "reset-entity" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert — ResetActions() is called on the entity in the finally block
        entity.ActionsWereReset.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет что ResetActions() вызывается на сущностях даже при ошибке сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnFailure_ResetActionsStillCalledOnEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var failingService = new TestBeforeSaveChangesService
        {
            OnProcessAsync = () => throw new InvalidOperationException("fail"),
        };
        var uow = CreateUnitOfWork(context, beforeSaveService: failingService);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "reset-on-fail" };
        context.DomainEntities.Add(entity);

        // Act — will throw
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false));

        // Assert — ResetActions() must still be called (it's in finally)
        entity.ActionsWereReset.Should().BeTrue();
    }

    #endregion

    #region Lifecycle actions disabled — no actions on non-IWithLifecycleActions entities

    /// <summary>
    /// Проверяет что сущности без IWithLifecycleActions не вызывают диспетчеризацию действий.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNonLifecycleActionEntity_NoActionsDispatched()
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
        TestLifecycleActionDbContext dbContext,
        IServiceProvider serviceProvider,
        DbSettingsBase settings,
        IBeforeSaveChangesService? beforeSaveChangesService = null)
        : EfUnitOfWork<TestLifecycleActionDbContext>(
            dbContext,
            serviceProvider,
            settings,
            beforeSaveChangesService)
    {
        public bool AreLifecycleActionsEnabled => AreAnyLifecycleActionsEnabled;
    }

    private sealed class TestEfDbSettingsLifecycleAction
        : Settings.EfDbSettingsBase<TestLifecycleActionDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public TestEfDbSettingsLifecycleAction(bool transactionsEnabled = false)
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
