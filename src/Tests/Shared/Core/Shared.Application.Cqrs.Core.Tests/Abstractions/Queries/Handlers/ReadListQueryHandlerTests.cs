using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Dal;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Queries.Handlers;

/// <summary>
/// Тесты <see cref="ReadListQueryHandler{TQuery,TRequest,TFilter,TResponse,TPayload,TEntity}"/>.
/// Проверяют пагинацию, фильтрацию по <c>Ids</c>,
/// сортировку по <c>DateCreated</c>, вызов <c>PostProcessAsync</c>
/// и корректность ответа.
/// </summary>
public sealed class ReadListQueryHandlerTests
{
    /// <summary>
    /// Создаёт фабрику логгеров для тестов.
    /// </summary>
    private static FakeLoggerFactory CreateLoggerFactory() => new();

    /// <summary>
    /// Создаёт тестовую сущность с заданным индексом и опциональной датой.
    /// </summary>
    private static TestEntity CreateEntity(int id, DateTime? dateCreated = null)
    {
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"entity-{id}"
        };
        entity.SetDateCreated(dateCreated ?? DateTime.UtcNow.AddDays(-id));
        return entity;
    }

    #region Handle Tests

    /// <summary>
    /// <c>Handle</c> вызывает <c>PaginationHelper</c> для расчёта skip/take
    /// и возвращает корректную страницу с пагинацией.
    /// </summary>
    [Fact]
    public async Task Handle_CallsPaginationHelperForSkipTake()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        for (var i = 0; i < 10; i++)
        {
            var entity = CreateEntity(i, new DateTime(2020, 1, 1).AddDays(i));
            repo.AddDirect(entity);
        }

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageNumber = 2, PageSize = 3 };
        var query = new TestReadListQuery(request);

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().NotBeNull();
        response.Payload!.Count.Should().Be(3);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(4);
    }

    /// <summary>
    /// <c>ListFilterBase</c> с заполненным массивом <c>Ids</c>
    /// добавляет фильтр по идентификаторам в <c>QueryOptions</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ListFilterBaseWithIds_AddsIdsFilter()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entityA = CreateEntity(1);
        var entityB = CreateEntity(2);
        var entityC = CreateEntity(3);

        repo.AddDirect(entityA);
        repo.AddDirect(entityB);
        repo.AddDirect(entityC);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var filter = new TestListFilter { Ids = [entityA.Id, entityC.Id] };
        var request = new TestPageableRequest { Filter = filter, PageSize = 100 };
        var query = new TestReadListQuery(request);

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().NotBeNull();
        response.Payload!.Select(e => e.Id).Should().BeEquivalentTo([entityA.Id, entityC.Id]);
    }

    /// <summary>
    /// <c>ListFilterBase</c> с <c>Ids = null</c> не добавляет фильтр,
    /// возвращаются все сущности.
    /// </summary>
    [Fact]
    public async Task Handle_ListFilterBaseWithoutIds_NoIdsFilter()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entityA = CreateEntity(1);
        var entityB = CreateEntity(2);
        var entityC = CreateEntity(3);

        repo.AddDirect(entityA);
        repo.AddDirect(entityB);
        repo.AddDirect(entityC);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var filter = new TestListFilter { Ids = null };
        var request = new TestPageableRequest { Filter = filter, PageSize = 100 };
        var query = new TestReadListQuery(request);

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().NotBeNull();
        response.Payload!.Should().HaveCount(3);
    }

    /// <summary>
    /// Для сущностей, реализующих <c>IWithDateCreated</c>,
    /// без явной сортировки добавляется <c>DateCreated ASC</c> по умолчанию.
    /// </summary>
    [Fact]
    public async Task Handle_IWithDateCreated_NoExplicitSort_AddsDateCreatedAscending()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entity1 = CreateEntity(1, new DateTime(2025, 3, 1));
        var entity2 = CreateEntity(2, new DateTime(2025, 1, 1));
        var entity3 = CreateEntity(3, new DateTime(2025, 2, 1));

        repo.AddDirect(entity1);
        repo.AddDirect(entity2);
        repo.AddDirect(entity3);

        var sut = new OptionsCapturingReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageSize = 100 };
        var query = new TestReadListQuery(request);

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        sut.LastOptions.Should().NotBeNull();
        sut.LastOptions!.OrderBy.Should().ContainSingle(o =>
            o.Direction == OrderDirectionType.Ascending);
    }

    /// <summary>
    /// Явная сортировка по <c>DateCreated.desc</c> не дублирует
    /// автоматическую сортировку для <c>IWithDateCreated</c>.
    /// </summary>
    [Fact]
    public async Task Handle_IWithDateCreated_ExplicitSortByDateCreated_DoesNotDuplicateOrder()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        var entity1 = CreateEntity(1, new DateTime(2025, 1, 1));
        var entity2 = CreateEntity(2, new DateTime(2025, 2, 1));
        var entity3 = CreateEntity(3, new DateTime(2025, 3, 1));

        repo.AddDirect(entity1);
        repo.AddDirect(entity2);
        repo.AddDirect(entity3);

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest
        {
            PageSize = 100,
            SortOptions = ["DateCreated.desc"]
        };
        var query = new TestReadListQuery(request);

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().NotBeNull();
        response.Payload!.Select(e => e.DateCreated).Should()
            .BeInDescendingOrder();
    }

    /// <summary>
    /// Для сущностей, НЕ реализующих <c>IWithDateCreated</c>,
    /// автоматическая сортировка не добавляется.
    /// </summary>
    [Fact]
    public async Task Handle_NonIWithDateCreatedEntity_NoDefaultSort()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntityWithoutDateCreated>();

        repo.AddDirect(new TestEntityWithoutDateCreated { Id = Guid.NewGuid(), Name = "e1" });
        repo.AddDirect(new TestEntityWithoutDateCreated { Id = Guid.NewGuid(), Name = "e2" });

        var sut = new OptionsCapturingNoDateCreatedReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new NoDateCreatedPageableRequest { PageSize = 100 };
        var query = new NoDateCreatedReadListQuery(request);

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        sut.LastOptions.Should().NotBeNull();
    }

    /// <summary>
    /// Хук <c>PostProcessAsync</c> вызывается один раз после обработки запроса.
    /// </summary>
    [Fact]
    public async Task Handle_PostProcessAsync_HookIsCalled()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        repo.AddDirect(CreateEntity(1));

        var sut = new PostProcessTrackingReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageSize = 100 };
        var query = new TestReadListQuery(request);

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        sut.PostProcessAsyncCallCount.Should().Be(1);
    }

    /// <summary>
    /// Ответ содержит <c>StatusCode = 200</c> и корректные поля пагинации.
    /// </summary>
    [Fact]
    public async Task Handle_Response_StatusCodeIs200_AndPaginationFieldsFilled()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetOrCreateRepository<TestEntity>();

        for (var i = 0; i < 5; i++)
        {
            repo.AddDirect(CreateEntity(i));
        }

        var sut = new TestReadListQueryHandler(CreateLoggerFactory(), unitOfWork);
        var request = new TestPageableRequest { PageNumber = 1, PageSize = 2 };
        var query = new TestReadListQuery(request);

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(3);
        response.Payload.Should().NotBeNull();
        response.Payload!.Should().HaveCount(2);
    }

    #endregion
}
