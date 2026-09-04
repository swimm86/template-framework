// ----------------------------------------------------------------------------------------------
// <copyright file="EfUnitOfWorkTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.LifecycleAction;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;
using Shared.Testing.Doubles.Repository;

namespace Shared.Infrastructure.Dal.EFCore.Tests;

/// <summary>
/// Тесты для класса <see cref="EfUnitOfWork{TDbContext}"/>.
/// Проверяет транзакции, сохранение изменений, доменные события,
/// управление репозиториями и отслеживанием сущностей.
/// </summary>
public sealed class EfUnitOfWorkTests
{
    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestDbContext(options);
    }

    private static TestEfDbSettings CreateSettings(bool transactionsEnabled = true)
    {
        return new TestEfDbSettings(transactionsEnabled);
    }

    private static FakeServiceProvider CreateServiceProvider()
    {
        return new FakeServiceProvider();
    }

    private static EfUnitOfWork<TestDbContext> CreateUnitOfWork(
        TestDbContext context,
        DbSettingsBase settings,
        IBeforeSaveChangesService? beforeSaveService = null,
        FakeServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= CreateServiceProvider();
        return new EfUnitOfWork<TestDbContext>(
            context,
            serviceProvider,
            settings,
            new LifecycleActionOrchestrator([], new LifecycleEntityRegistry(), new LifecycleActionGate()),
            beforeSaveService);
    }

    #region Constructor Tests

    /// <summary>
    /// Проверяет что конструктор включает транзакции когда TransactionsEnabled=true.
    /// </summary>
    [Fact]
    public async Task Constructor_TransactionsEnabled_CommitTransactionSucceeds()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);

        // Act
        var uow = CreateUnitOfWork(context, settings);

        // Assert
        var act = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Проверяет что конструктор отключает транзакции когда TransactionsEnabled=false.
    /// </summary>
    [Fact]
    public async Task Constructor_TransactionsDisabled_CommitTransactionThrows()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);

        // Act
        var uow = CreateUnitOfWork(context, settings);

        // Assert
        var act = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region SaveChangesAsync Tests

    /// <summary>
    /// Проверяет что SaveChangesAsync сохраняет изменения в БД.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithEntity_PersistsToDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "test" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert
        result.Should().Be(1);
        context.Entities.Should().ContainSingle(e => e.Name == "test");
    }

    /// <summary>
    /// Проверяет что SaveChangesAsync уважает CancellationToken.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "test" };
        context.Entities.Add(entity);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => uow.SaveChangesAsync(cts.Token, commitTransaction: false);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Проверяет что SaveChangesAsync с commitTransaction=false не коммитит транзакцию и оставляет её открытой.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_CommitTransactionFalse_LeavesTransactionOpen()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "test" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert
        result.Should().Be(1);
        var act = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Проверяет что SaveChangesAsync с дефолтным commitTransaction коммитит транзакцию.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_DefaultCommitTransaction_CommitsAndPersists()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "commit-default" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        context.Entities.Should().ContainSingle(e => e.Name == "commit-default");
    }

    #endregion

    #region SaveChangesAsync Transaction Tests

    /// <summary>
    /// Проверяет что SaveChangesAsync коммитит транзакцию при успехе.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnSuccess_CommitsTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "commit-test" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        context.Entities.Should().ContainSingle(e => e.Name == "commit-test");
    }

    /// <summary>
    /// Проверяет что SaveChangesAsync откатывает транзакцию при ошибке.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnFailure_RollbacksTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var beforeSaveService = new TestBeforeSaveChangesService
        {
            OnProcessAsync = () => throw new InvalidOperationException("save failed"),
        };
        var uow = CreateUnitOfWork(context, settings, beforeSaveService);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "rollback-test" };
        context.Entities.Add(entity);

        // Act
        var act = () => uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        context.Entities.Should().NotContain(e => e.Name == "rollback-test");
    }

    #endregion

    #region SaveChangesAsync BeforeSave Tests

    /// <summary>
    /// Проверяет что IBeforeSaveChangesService вызывается перед сохранением.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithBeforeSaveService_CallsProcessAsync()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var beforeSaveService = new TestBeforeSaveChangesService();
        var uow = CreateUnitOfWork(context, settings, beforeSaveService);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "before-save" };
        context.Entities.Add(entity);

        // Act
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert
        beforeSaveService.ProcessAsyncCallCount.Should().Be(1);
        beforeSaveService.LastDbContext.Should().BeSameAs(context);
    }

    /// <summary>
    /// Проверяет что null IBeforeSaveChangesService не вызывает ошибок.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNullBeforeSaveService_NoOp()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings, beforeSaveService: null);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "null-service" };
        context.Entities.Add(entity);

        // Act
        var result = await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region GetRepository Tests

    /// <summary>
    /// Проверяет что GetRepository возвращает зарегистрированный репозиторий.
    /// </summary>
    [Fact]
    public void GetRepository_WithRegisteredRepository_ReturnsInstance()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var serviceProvider = CreateServiceProvider();

        var fakeRepo = new FakeRepository<TestEntityWithCreatedDeleted>();
        serviceProvider.Register<IRepository<TestEntityWithCreatedDeleted>>(fakeRepo);

        var uow = CreateUnitOfWork(context, settings, serviceProvider: serviceProvider);

        // Act
        var result = uow.GetRepository<TestEntityWithCreatedDeleted>();

        // Assert
        result.Should().BeSameAs(fakeRepo);
    }

    /// <summary>
    /// Проверяет что GetRepository выбрасывает исключение если репозиторий не зарегистрирован.
    /// </summary>
    [Fact]
    public void GetRepository_WithUnregisteredRepository_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var serviceProvider = CreateServiceProvider();
        var uow = CreateUnitOfWork(context, settings, serviceProvider: serviceProvider);

        // Act
        var act = () => uow.GetRepository<TestEntityWithCreatedDeleted>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region ClearTracking Tests

    /// <summary>
    /// Проверяет что ClearTracking очищает change tracker.
    /// </summary>
    [Fact]
    public void ClearTracking_ClearsChangeTracker()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "tracked" };
        context.Entities.Add(entity);

        context.ChangeTracker.Entries().Should().NotBeEmpty();

        // Act
        uow.ClearTracking();

        // Assert
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region EnableTransaction / DisableTransaction Tests

    /// <summary>
    /// Проверяет что EnableTransaction активирует транзакцию — CommitTransactionAsync начинает работать.
    /// </summary>
    [Fact]
    public async Task EnableTransaction_CommitTransactionSucceeds()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        var actBefore = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actBefore.Should().ThrowAsync<InvalidOperationException>();

        // Act
        uow.EnableTransaction();

        // Assert
        var actAfter = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfter.Should().NotThrowAsync();
    }

    /// <summary>
    /// Проверяет что EnableTransaction возвращает IUnitOfWork для fluent chaining.
    /// </summary>
    [Fact]
    public void EnableTransaction_ReturnsIUnitOfWorkForFluentChaining()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        // Act
        var result = uow.EnableTransaction();

        // Assert
        result.Should().BeSameAs(uow);
    }

    /// <summary>
    /// Проверяет что DisableTransaction деактивирует транзакцию — CommitTransactionAsync бросает исключение.
    /// </summary>
    [Fact]
    public async Task DisableTransaction_CommitTransactionThrows()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        var actBefore = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actBefore.Should().NotThrowAsync();

        // Act
        uow.DisableTransaction();

        // Assert
        var actAfter = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfter.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Проверяет что DisableTransaction возвращает IUnitOfWork для fluent chaining.
    /// </summary>
    [Fact]
    public void DisableTransaction_ReturnsIUnitOfWorkForFluentChaining()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        // Act
        var result = uow.DisableTransaction();

        // Assert
        result.Should().BeSameAs(uow);
    }

    #endregion

    #region CommitTransactionAsync Tests

    /// <summary>
    /// Проверяет что CommitTransactionAsync выбрасывает исключение когда транзакции отключены.
    /// </summary>
    [Fact]
    public async Task CommitTransactionAsync_WhenDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        // Act
        var act = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region RollbackTransactionAsync Tests

    /// <summary>
    /// Проверяет что RollbackTransactionAsync выбрасывает исключение когда транзакции отключены.
    /// </summary>
    [Fact]
    public async Task RollbackTransactionAsync_WhenDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        // Act
        var act = () => uow.RollbackTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region SaveChanges (sync) Tests

    /// <summary>
    /// Проверяет что SaveChanges делегирует SaveChangesAsync.
    /// </summary>
    [Fact]
    public void SaveChanges_DelegatesToSaveChangesAsync()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: false);
        var uow = CreateUnitOfWork(context, settings);

        var entity = new TestEntityWithCreatedDeleted { Id = Guid.NewGuid(), Name = "sync-save" };
        context.Entities.Add(entity);

        // Act
        var result = uow.SaveChanges(commitTransaction: false);

        // Assert
        result.Should().Be(1);
        context.Entities.Should().ContainSingle(e => e.Name == "sync-save");
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Проверяет что Dispose освобождает текущую транзакцию.
    /// </summary>
    [Fact]
    public async Task Dispose_CommitTransactionThrowsAfterDispose()
    {
        // Arrange
        using var context = CreateContext();
        var settings = CreateSettings(transactionsEnabled: true);
        var uow = CreateUnitOfWork(context, settings);

        var actBefore = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actBefore.Should().NotThrowAsync();

        // Act
        uow.Dispose();

        // Assert
        var actAfter = () => uow.CommitTransactionAsync(TestContext.Current.CancellationToken);
        await actAfter.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Простая реализация IServiceProvider для регистрации и резолва сервисов в тестах.
    /// </summary>
    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Регистрирует сервис заданного типа.
        /// </summary>
        /// <typeparam name="T">Тип сервиса.</typeparam>
        /// <param name="instance">Экземпляр сервиса.</param>
        public void Register<T>(T instance)
        {
            _services[typeof(T)] = instance!;
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    #endregion
}
