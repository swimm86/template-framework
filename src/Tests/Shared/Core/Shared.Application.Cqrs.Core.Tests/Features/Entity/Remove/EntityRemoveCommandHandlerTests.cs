using Microsoft.AspNetCore.Http;
using Shared.Application.Cqrs.Core.Features.Entity.Remove;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Features.Entity.Remove;

/// <summary>
/// Тесты <see cref="EntityRemoveCommandHandler{TEntity}"/>.
/// Проверяют удаление сущности, проброс исключений,
/// обработку мягко-удалённых сущностей и изоляцию удаления.
/// </summary>
public sealed class EntityRemoveCommandHandlerTests
{
    /// <summary>
    /// Создаёт тестируемый обработчик, unit of work и репозиторий.
    /// </summary>
    private static (
        EntityRemoveCommandHandler<TestEntity> Handler,
        FakeUnitOfWork UnitOfWork,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var handler = new EntityRemoveCommandHandler<TestEntity>(
            uow,
            new FakeLoggerFactory(),
            new FakeUserProvider());

        return (handler, uow, repo);
    }

    #region Handle Tests

    /// <summary>
    /// При наличии сущности — вызывает удаление через репозиторий,
    /// сохраняет изменения и возвращает <c>200 OK</c>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_CallsRemoveAndReturnsOk()
    {
        // Arrange
        var (handler, uow, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        repo.RemoveCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Обработчик пробрасывает переданный <see cref="CancellationToken"/>
    /// в <c>SaveChangesAsync</c> без изменений.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_ForwardsCancellationTokenToSaveChanges()
    {
        // Arrange
        var (handler, uow, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        // Act
        await handler.Handle(command, token);

        // Assert
        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }

    /// <summary>
    /// Если <c>RemoveAsync</c> репозитория выбрасывает исключение —
    /// оно пробрасывается наружу без перехвата.
    /// </summary>
    [Fact]
    public async Task Handle_RemoveThrows_PropagatesException()
    {
        // Arrange
        var (handler, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });
        repo.ExceptionToThrowOnRemove = new InvalidOperationException("Remove failed");

        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Remove failed");
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
        var (handler, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        entity.SetIsDeleted();
        repo.AddDirect(entity);

        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repo.RemoveCallCount.Should().Be(0);
    }

    /// <summary>
    /// При наличии нескольких сущностей в репозитории
    /// удаляется только та, чей идентификатор указан в команде.
    /// </summary>
    [Fact]
    public async Task Handle_RemovesOnlyMatchingEntity()
    {
        // Arrange
        var (handler, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId });
        repo.AddDirect(new TestEntity { Id = otherId, Name = "other" });

        var command = new EntityRemoveCommand<TestEntity>(new EntityRemoveRequest { Id = entityId });

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        repo.RemoveCallCount.Should().Be(1);
        repo.Items.Should().ContainSingle(e => e.Id == otherId);
    }

    #endregion
}
