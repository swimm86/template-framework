using FluentValidation;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

/// <summary>
/// Тесты <see cref="UpdateCommandHandler{TCommand,TRequest,TEntity,TPayload,TResponse}"/>.
/// Проверяют обновление сущности, обработку отсутствующей сущности,
/// формирование <c>ConstructOptions</c> с <c>WithTracking = true</c>,
/// проброс <see cref="CancellationToken"/> и корректность ответа.
/// </summary>
public sealed class UpdateCommandHandlerTests
{
    /// <summary>
    /// Создаёт тестируемый обработчик, mapper, unit of work, user provider и репозиторий.
    /// </summary>
    private static (
        TestUpdateCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestUpdateCommandHandler(loggerFactory, mapper, uow, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    #region Handle Tests

    /// <summary>
    /// При наличии сущности — обновляет её через mapper,
    /// проставляет <c>UpdatedByUserId</c> и сохраняет изменения.
    /// </summary>
    [Fact]
    public async Task Handle_EntityFound_UpdatesEntity()
    {
        // Arrange
        var (handler, mapper, uow, userProvider, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "original" };
        repo.AddDirect(entity);

        var command = new TestUpdateCommand(entityId, new object());

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        mapper.MapInPlaceCallCount.Should().Be(1);
        entity.UpdatedByUserId.Should().Be(userProvider.UserId);
        uow.SaveChangesAsyncCallCount.Should().Be(1);
    }

    /// <summary>
    /// Если сущность не найдена — выбрасывается <see cref="NotFoundException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestUpdateCommand(Guid.NewGuid(), new object());

        // Act
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// <c>ConstructOptions</c> для команды обновления возвращает
    /// <c>WithTracking = true</c> (требуется для отслеживания изменений).
    /// </summary>
    [Fact]
    public void Handle_ConstructOptions_ReturnsWithTrackingTrue()
    {
        // Arrange
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestUpdateCommand(Guid.NewGuid(), new object());

        // Act
        var options = handler.ConstructOptions(command);

        // Assert
        options.WithTracking.Should().BeTrue();
    }

    /// <summary>
    /// Обработчик пробрасывает переданный <see cref="CancellationToken"/>
    /// в <c>SaveChangesAsync</c> без изменений.
    /// </summary>
    [Fact]
    public async Task Handle_ForwardsCancellationTokenToSaveChanges()
    {
        // Arrange
        var (handler, _, uow, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "test" });

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new TestUpdateCommand(entityId, new object());

        // Act
        await handler.Handle(command, token);

        // Assert
        uow.LastSaveChangesCancellationToken.Should().Be(token);
    }

    /// <summary>
    /// Ответ команды обновления содержит корректные <c>Key</c>,
    /// <c>StatusCode = 200</c> и не-null <c>Payload</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsCorrectResponse()
    {
        // Arrange
        var (handler, _, _, _, repo) = CreateSut();
        var entityId = Guid.NewGuid();
        repo.AddDirect(new TestEntity { Id = entityId, Name = "updated" });

        var command = new TestUpdateCommand(entityId, new object());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Key.Should().Be(entityId);
        result.StatusCode.Should().Be(200);
        result.Payload.Should().NotBeNull();
    }

    #endregion
}
