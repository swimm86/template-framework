using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

/// <summary>
/// Тесты <see cref="DeleteCommandHandler{TCommand,TEntity}"/>.
/// Проверяют удаление сущности, обработку отсутствующей и мягко-удалённой сущности,
/// проброс <see cref="CancellationToken"/>.
/// </summary>
public sealed class DeleteCommandHandlerTests
{
    /// <summary>
    /// Создаёт тестируемый обработчик, unit of work, user provider и репозиторий.
    /// </summary>
    private static (
        TestDeleteCommandHandler Handler,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();

        var handler = new TestDeleteCommandHandler(uow, loggerFactory, userProvider);

        return (handler, uow, userProvider, repo);
    }

    #region Handle Tests

    /// <summary>
    /// При наличии сущности — вызывает удаление через репозиторий,
    /// сохраняет изменения и возвращает <c>200 OK</c>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_CallsRemoveAndSaveChanges()
    {
        // Arrange
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        var command = new TestDeleteCommand(entityId);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        repo.RemoveCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Если сущность не найдена — выбрасывается <see cref="NotFoundException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var (handler, _, _, _) = CreateSut();
        var command = new TestDeleteCommand(Guid.NewGuid());

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Обработчик пробрасывает переданный <see cref="CancellationToken"/>
    /// в <c>SaveChangesAsync</c> без изменений.
    /// </summary>
    [Fact]
    public async Task Handle_ForwardsCancellationTokenToSaveChanges()
    {
        // Arrange
        var (handler, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestDeleteCommand(entityId);

        // Act
        await handler.Handle(command, token);

        // Assert
        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }

    /// <summary>
    /// Попытка удалить уже мягко-удалённую сущность
    /// приводит к <see cref="NotFoundException"/> (фильтр <c>IsDeleted</c>
    /// в <c>ConstructOptions</c> исключает сущность из выборки).
    /// </summary>
    [Fact]
    public async Task Handle_AlreadySoftDeletedEntity_ThrowsNotFoundException()
    {
        // Arrange
        var (handler, _, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        entity.SetIsDeleted();
        repo.AddDirect(entity);

        var command = new TestDeleteCommand(entityId);

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repo.RemoveCallCount.Should().Be(0);
    }

    #endregion
}
