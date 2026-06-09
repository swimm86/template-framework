// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWorkLifecycleActionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты обработки действий жизненного цикла в <see cref="EfUnitOfWork{TDbContext}"/>.
/// Проверяют диспетчеризацию BeforeSave/AfterSave действий через
/// <see cref="ILifecycleActionOrchestrator"/>, автоматическую регистрацию
/// сущностей в оркестраторе через ChangeTracker и сброс состояния после сохранения.
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
        IBeforeSaveChangesService? beforeSaveService = null,
        params ILifecycleActionHandler[] handlers)
    {
        settings ??= CreateSettings();
        var orchestrator = new LifecycleActionOrchestrator(
            handlers,
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());
        return new TestUnitOfWork(context, new FakeServices(), settings, orchestrator, beforeSaveService);
    }

    #region BeforeSave / AfterSave dispatching

    /// <summary>
    /// Проверяет, что BeforeSave-обработчик вызывается до сохранения, ровно один раз
    /// для добавленной сущности.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithBeforeSaveHandler_HandlerIsCalledOnce()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var after = new TestAfterSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before, after]);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "test" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.ExecuteCallCount.Should().Be(1);
        after.ExecuteCallCount.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что для нескольких сущностей одного типа обработчик вызывается
    /// ровно один раз и получает все сущности в коллекции.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_MultipleEntities_HandlerReceivesAll()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new RecordingBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before]);
        var entity1 = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "first" };
        var entity2 = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "second" };
        context.DomainEntities.Add(entity1);
        context.DomainEntities.Add(entity2);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.ExecuteCallCount.Should().Be(1);
        before.LastEntities.Should().HaveCount(2);
    }

    #endregion

    #region Global DisableActions

    /// <summary>
    /// <c>orchestrator.DisableActions()</c> глобально
    /// предотвращает вызов обработчиков в обеих фазах.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_GlobalActionsDisabled_NoHandlerCalled()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var after = new TestAfterSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before, after]);
        uow.Orchestrator.DisableActions();
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "no-actions" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.ExecuteCallCount.Should().Be(0);
        after.ExecuteCallCount.Should().Be(0);
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisableActions(IReadOnlyList{string})"/>
    /// отключает только указанные ключи.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_KeyDisabled_OnlyThatKeyHandlerNotCalled()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var after = new TestAfterSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before, after]);
        uow.Orchestrator.DisableActions([TestActionKeys.BeforeSaveEvent]);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "before-only-disabled" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.ExecuteCallCount.Should().Be(0);
        after.ExecuteCallCount.Should().Be(1);
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisablePhase(LifecyclePhase)"/>
    /// предотвращает вызов обработчиков только для указанной фазы.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_PhaseDisabled_OnlyThatPhaseHandlerNotCalled()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var after = new TestAfterSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before, after]);
        uow.Orchestrator.DisablePhase(LifecyclePhase.BeforeSave);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "before-disabled" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.ExecuteCallCount.Should().Be(0);
        after.ExecuteCallCount.Should().Be(1);
    }

    #endregion

    #region resetLifecycleActionSettingsAfterSave

    /// <summary>
    /// При <c>resetLifecycleActionSettingsAfterSave=true</c> (по умолчанию) —
    /// глобальное отключение сбрасывается после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_DefaultReset_GloballyDisabledCleared()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before]);
        uow.Orchestrator.DisableActions();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetLifecycleActionSettingsAfterSave: true);

        // Assert
        uow.Orchestrator.IsActionEnabled(new TestLifecycleActionEntity(), "any", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    /// <summary>
    /// При <c>resetLifecycleActionSettingsAfterSave=false</c> — глобальное отключение
    /// сохраняется после сохранения.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_NoReset_GloballyDisabledPreserved()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before]);
        uow.Orchestrator.DisableActions();

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false, resetLifecycleActionSettingsAfterSave: false);

        // Assert
        uow.Orchestrator.IsActionEnabled(new TestLifecycleActionEntity(), "any", LifecyclePhase.BeforeSave).Should().BeFalse();
    }

    #endregion

    #region ResetAllActions

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.ResetAllActions"/> вызывается
    /// в finally-блоке SaveChanges, очищая все настройки.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnSuccess_ResetsAllActionSettings()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new TestBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before]);
        uow.Orchestrator.DisableActions(["k"]);
        uow.Orchestrator.DisablePhase(LifecyclePhase.BeforeSave);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        var probe = new TestLifecycleActionEntity();
        uow.Orchestrator.IsActionEnabled(probe, "k", LifecyclePhase.BeforeSave).Should().BeTrue();
        uow.Orchestrator.IsActionEnabled(probe, "any", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.ResetAllActions"/> вызывается и при
    /// ошибке сохранения — настройки очищаются даже если <c>SaveChanges</c> бросил исключение.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnFailure_StillResetsAllActionSettings()
    {
        // Arrange
        await using var context = CreateContext();
        var failingService = new TestBeforeSaveChangesService
        {
            OnProcessAsync = () => throw new InvalidOperationException("fail"),
        };
        var before = new TestBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, beforeSaveService: failingService, handlers: [before]);
        uow.Orchestrator.DisableActions(["k"]);

        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "fail" };
        context.DomainEntities.Add(entity);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false));

        // Assert
        var probe = new TestLifecycleActionEntity();
        uow.Orchestrator.IsActionEnabled(probe, "k", LifecyclePhase.BeforeSave).Should().BeTrue();
    }

    #endregion

    #region Automatic entity tracking

    /// <summary>
    /// Сущности, добавленные в <see cref="DbContext"/>, автоматически попадают в карту
    /// отслеживаемых оркестратора и попадают в обработчик.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_EntityTrackedAutomatically_AvailableToHandlers()
    {
        // Arrange
        await using var context = CreateContext();
        var before = new RecordingBeforeSaveHandler();
        var uow = CreateUnitOfWork(context, handlers: [before]);
        var entity = new TestLifecycleActionEntity { Id = Guid.NewGuid(), Name = "tracked" };
        context.DomainEntities.Add(entity);

        // Act
        await uow.SaveChangesAsync(CancellationToken.None, commitTransaction: false);

        // Assert
        before.LastEntities.Should().ContainSingle().Which.Should().BeSameAs(entity);
    }

    #endregion

    #region Helper classes

    /// <summary>
    /// Подкласс <see cref="EfUnitOfWork{TDbContext}"/>, экспортирующий orchestrator
    /// для тонкой настройки в тестах.
    /// </summary>
    private sealed class TestUnitOfWork(
        TestLifecycleActionDbContext dbContext,
        IServiceProvider serviceProvider,
        DbSettingsBase settings,
        ILifecycleActionOrchestrator orchestrator,
        IBeforeSaveChangesService? beforeSaveChangesService = null)
        : EfUnitOfWork<TestLifecycleActionDbContext>(
            dbContext,
            serviceProvider,
            settings,
            orchestrator,
            beforeSaveChangesService)
    {
        public ILifecycleActionOrchestrator Orchestrator => orchestrator;
    }

    /// <summary>
    /// Handler, фиксирующий коллекцию сущностей, для которой он был вызван.
    /// </summary>
    private sealed class RecordingBeforeSaveHandler
        : LifecycleActionHandlerBase<TestLifecycleActionEntity>
    {
        public ICollection<TestLifecycleActionEntity>? LastEntities { get; private set; }

        public int ExecuteCallCount { get; private set; }

        public override LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public override string Key => "recording-before-save";

        public override int Order => 0;

        protected override Task ExecuteActionAsync(
            IEnumerable<TestLifecycleActionEntity> entities,
            CancellationToken cancellationToken)
        {
            LastEntities = entities.ToArray();
            ExecuteCallCount++;
            return Task.CompletedTask;
        }
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
