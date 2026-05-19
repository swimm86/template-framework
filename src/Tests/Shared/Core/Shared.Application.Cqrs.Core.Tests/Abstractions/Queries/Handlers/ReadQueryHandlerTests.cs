using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Handlers;

/// <summary>
/// Тесты <see cref="ReadQueryHandler{TQuery,TEntity,TResponse}"/>.
/// Проверяют поиск сущности по ключу, маппинг в DTO,
/// проброс <see cref="CancellationToken"/> в <c>GetAsync</c>,
/// обработку <c>GuardAsync</c> и отсутствующей сущности.
/// </summary>
public sealed class ReadQueryHandlerTests
{
    /// <summary>
    /// Создаёт фабрику логгеров для тестов.
    /// </summary>
    private static FakeLoggerFactory CreateLoggerFactory() => new();

    #region Handle Tests

    /// <summary>
    /// При наличии сущности — маппит её в DTO и возвращает результат.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_MapsToDtoAndReturns()
    {
        // Arrange
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "test-entity" };
        var repository = new FakeRepository<TestEntity>();
        repository.AddDirect(entity);

        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.GetOrCreateRepository<TestEntity>().AddDirect(entity);

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork);
        var query = new TestReadByKeyQuery(entity.Id);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        mapper.MapCallCount.Should().Be(1);
        result.Should().Be(entity);
    }

    /// <summary>
    /// Обработчик пробрасывает переданный <see cref="CancellationToken"/>
    /// в <c>GetAsync</c> репозитория.
    /// </summary>
    [Fact]
    public async Task Handle_ForwardsCancellationTokenToGetAsync()
    {
        // Arrange
        CancellationToken? capturedToken = null;
        var callbackUow = new CallbackUnitOfWork(ct => capturedToken = ct);

        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "test-entity" };
        var repository = (CallbackRepository<TestEntity>)callbackUow.GetRepository<TestEntity>();
        repository.Inner.AddDirect(entity);

        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, callbackUow);
        var query = new TestReadByKeyQuery(entity.Id);

        // Act
        using var cts = new CancellationTokenSource();
        await sut.Handle(query, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token, "GetAsync should receive the CancellationToken from Handle");
    }

    /// <summary>
    /// Если переопределённый <c>GuardAsync</c> выбрасывает исключение —
    /// оно пробрасывается наружу, запрос к репозиторию не выполняется.
    /// </summary>
    [Fact]
    public async Task Handle_GuardAsyncOverrideThrows_Propagates()
    {
        // Arrange
        var mapper = new FakeMapper();
        var unitOfWork = new FakeUnitOfWork();
        var expectedException = new InvalidOperationException("guard-failed");

        var sut = new GuardThrowingReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork, expectedException);
        var query = new TestReadByKeyQuery("any-key");

        // Act
        var act = () => sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("guard-failed");
    }

    /// <summary>
    /// Если сущность не найдена по ключу — выбрасывается <see cref="NotFoundException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => e);

        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.GetOrCreateRepository<TestEntity>();

        var sut = new TestReadQueryHandler(CreateLoggerFactory(), mapper, unitOfWork);
        var query = new TestReadByKeyQuery("nonexistent-key");

        // Act
        var act = () => sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}
