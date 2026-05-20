using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Interfaces;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions;

/// <summary>
/// Тесты для <see cref="EntityRequestHandler{TRequest,TResponse,TEntity}"/>: получение репозитория,
/// опции запроса, переопределение методов ProcessEntity/ProcessResponse/ProcessEntities.
/// </summary>
public sealed class EntityRequestHandlerTests
{
    private static FakeLoggerFactory LoggerFactory => new();
    private static FakeUnitOfWork UnitOfWork => new();

    /// <summary>
    /// Обращение к <see cref="EntityRequestHandler{TEntity,TRequest,TResponse}.Repository"/> вызывает <see cref="IUnitOfWork.GetRepository{T}"/>.
    /// </summary>
    [Fact]
    public void Repository_CallsUnitOfWorkGetRepository()
    {
        // Arrange
        var uow = new CountingUnitOfWork();
        var handler = new TestEntityRequestHandler(uow, LoggerFactory);

        // Act
        _ = handler.Repository;

        // Assert
        uow.GetRepositoryCallCount.Should().Be(1);
    }

    /// <summary>
    /// Двойное обращение к <see cref="EntityRequestHandler{TEntity,TRequest,TResponse}.Repository"/> вызывает GetRepository дважды.
    /// </summary>
    [Fact]
    public void Repository_DoubleAccess_CallsGetRepositoryTwice()
    {
        // Arrange
        var uow = new CountingUnitOfWork();
        var handler = new TestEntityRequestHandler(uow, LoggerFactory);

        // Act
        _ = handler.Repository;
        _ = handler.Repository;

        // Assert
        uow.GetRepositoryCallCount.Should().Be(2);
    }

    /// <summary>
    /// По умолчанию <see cref="QueryOptions{TEntity}.WithTracking"/> = false.
    /// </summary>
    [Fact]
    public void ConstructOptions_DefaultWithTracking_IsFalse()
    {
        // Arrange
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest());

        // Assert
        options.WithTracking.Should().BeFalse();
    }

    /// <summary>
    /// По умолчанию <see cref="QueryOptions{TEntity}.AsSplitQuery"/> = false.
    /// </summary>
    [Fact]
    public void ConstructOptions_DefaultAsSplitQuery_IsFalse()
    {
        // Arrange
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest());

        // Assert
        options.AsSplitQuery.Should().BeFalse();
    }

    /// <summary>
    /// Переопределённый <c>IsTrackingEnabled</c> возвращает true в <see cref="QueryOptions{TEntity}.WithTracking"/>.
    /// </summary>
    [Fact]
    public void ConstructOptions_OverriddenWithTracking_IsTrue()
    {
        // Arrange
        var handler = new TestEntityRequestHandlerWithTracking(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest());

        // Assert
        options.WithTracking.Should().BeTrue();
    }

    /// <summary>
    /// Переопределённый <c>UseSplitQuery</c> возвращает true в <see cref="QueryOptions{TEntity}.AsSplitQuery"/>.
    /// </summary>
    [Fact]
    public void ConstructOptions_OverriddenAsSplitQuery_IsTrue()
    {
        // Arrange
        var handler = new TestEntityRequestHandlerWithSplitQuery(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest());

        // Assert
        options.AsSplitQuery.Should().BeTrue();
    }

    /// <summary>
    /// Для сущности <see cref="IWithDeleted"/> в фильтры добавляется условие IsDeleted == false.
    /// </summary>
    [Fact]
    public void ConstructOptions_IWithDeletedEntity_AddsIsDeletedFilter()
    {
        // Arrange
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest());

        // Assert
        options.Filters.Should().HaveCount(1);
    }

    /// <summary>
    /// Для сущности без <see cref="IWithDeleted"/> фильтр IsDeleted не добавляется.
    /// </summary>
    [Fact]
    public void ConstructOptions_NonIWithDeletedEntity_NoFilter()
    {
        // Arrange
        var handler = new TestEntityWithoutDeletedHandler(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityWithoutDeletedRequest());

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// При <c>withDeletable: true</c> фильтр IsDeleted пропускается.
    /// </summary>
    [Fact]
    public void ConstructOptions_WithDeletableTrue_SkipsIsDeletedFilter()
    {
        // Arrange
        var handler = new TestEntityRequestHandler(UnitOfWork, LoggerFactory);

        // Act
        var options = handler.ConstructOptions(new GetTestEntityRequest(), withDeletable: true);

        // Assert
        options.Filters.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="EntityRequestHandler{TEntity,TRequest,TResponse}.ProcessEntityAsync"/> по умолчанию NoOp, переопределение вызывается.
    /// </summary>
    [Fact]
    public async Task ProcessEntityAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        // Arrange
        var handler = new TestEntityRequestHandlerWithProcessEntity(UnitOfWork, LoggerFactory);

        // Act
        await handler.CallProcessEntityAsync(
            new TestEntity(),
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        // Assert
        handler.ProcessEntityCalled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="EntityRequestHandler{TEntity,TRequest,TResponse}.ProcessResponseAsync"/> по умолчанию NoOp, переопределение вызывается.
    /// </summary>
    [Fact]
    public async Task ProcessResponseAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        // Arrange
        var handler = new TestEntityRequestHandlerWithProcessResponse(UnitOfWork, LoggerFactory);

        // Act
        await handler.CallProcessResponseAsync(
            new TestEntity(),
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        // Assert
        handler.ProcessResponseCalled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="EntityRequestHandler{TEntity,TRequest,TResponse}.ProcessEntitiesAsync"/> по умолчанию NoOp, переопределение вызывается.
    /// </summary>
    [Fact]
    public async Task ProcessEntitiesAsync_DefaultIsNoOp_OverrideIsCalled()
    {
        // Arrange
        var handler = new TestEntityRequestHandlerWithProcessEntities(UnitOfWork, LoggerFactory);

        // Act
        await handler.CallProcessEntitiesAsync(
            new List<TestEntity> { new() },
            new GetTestEntityRequest(),
            TestContext.Current.CancellationToken);

        // Assert
        handler.ProcessEntitiesCalled.Should().BeTrue();
    }
}
