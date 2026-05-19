using FluentValidation;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

/// <summary>
/// Тесты <see cref="CloneCommandHandler{TCommand,TRequest,TEntity,TResponsePayload,TResponse}"/>.
/// Проверяют клонирование сущности через mapper,
/// обработку отсутствующей сущности и проброс <see cref="CancellationToken"/>.
/// </summary>
public sealed class CloneCommandHandlerTests
{
    /// <summary>
    /// Создаёт тестируемый обработчик, mapper, unit of work, user provider и репозиторий.
    /// </summary>
    private static (
        TestCloneCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, TestEntity>(e => new TestEntity { Id = Guid.NewGuid(), Name = $"Clone of {e.Name}" });
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestCloneCommandHandler(mapper, uow, loggerFactory, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    #region Handle Tests

    /// <summary>
    /// При наличии сущности — клонирует её через mapper,
    /// добавляет клон в репозиторий, сохраняет изменения и возвращает <c>201 Created</c>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_ClonesViaMapperAndAdds()
    {
        // Arrange
        var (handler, mapper, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "source" });

        var command = new TestCloneCommand(entityId, new object());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        mapper.MapCallCount.Should().Be(2);
        repo.AddCallCount.Should().Be(1);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.StatusCode.Should().Be(201);
    }

    /// <summary>
    /// Если сущность не найдена — выбрасывается <see cref="NotFoundException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestCloneCommand(Guid.NewGuid(), new object());

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
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        var (handler, _, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "source" });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestCloneCommand(entityId, new object());

        // Act
        await handler.Handle(command, token);

        // Assert
        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }

    #endregion
}
