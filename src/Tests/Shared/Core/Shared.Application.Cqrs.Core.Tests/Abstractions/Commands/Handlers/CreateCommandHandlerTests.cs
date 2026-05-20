using FluentValidation;
using Microsoft.AspNetCore.Http;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Handlers;

/// <summary>
/// Тесты <see cref="CreateCommandHandler{TCommand,TRequest,TEntity,TResponsePayload,TResponse}"/>.
/// Проверяют создание сущности, маппинг, валидацию и проброс исключений.
/// </summary>
public sealed class CreateCommandHandlerTests
{
    /// <summary>
    /// Создаёт тестируемый обработчик, mapper, unit of work, user provider и репозиторий.
    /// </summary>
    private static (
        TestCreateCommandHandler Handler,
        FakeMapper Mapper,
        FakeUnitOfWork UnitOfWork,
        FakeUserProvider UserProvider,
        FakeRepository<TestEntity> Repository)
        CreateSut()
    {
        var mapper = new FakeMapper();
        mapper.RegisterMap<object, TestEntity>(_ => new TestEntity { Id = Guid.NewGuid(), Name = "test" });
        mapper.RegisterMap<TestEntity, object>(e => new { e.Id, e.Name });

        var uow = new FakeUnitOfWork();
        var repo = uow.GetOrCreateRepository<TestEntity>();
        var userProvider = new FakeUserProvider();
        var loggerFactory = new FakeLoggerFactory();
        var validators = Array.Empty<IValidator<TestEntity>>();

        var handler = new TestCreateCommandHandler(loggerFactory, mapper, uow, validators, userProvider);

        return (handler, mapper, uow, userProvider, repo);
    }

    #region Handle Tests

    /// <summary>
    /// При валидном запросе — сущность создаётся через mapper,
    /// добавляется в репозиторий и сохраняется.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRequest_CreatesEntityAndReturnsSuccess()
    {
        // Arrange
        var (handler, _, uow, _, repo) = CreateSut();
        var command = new TestCreateCommand(new object());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        repo.Items.Should().ContainSingle();
        uow.SaveChangesAsyncCallCount.Should().Be(1);
        result.Id.Should().NotBeNull();
    }

    /// <summary>
    /// Ответ команды создания содержит <c>StatusCode = 201 Created</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsStatusCode201()
    {
        // Arrange
        var (handler, _, _, _, _) = CreateSut();
        var command = new TestCreateCommand(new object());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Если валидация фейлится — выбрасывается <see cref="ValidationException"/>,
    /// сущность не создаётся.
    /// </summary>
    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var (handler, mapper, uow, userProvider, _) = CreateSut();
        mapper.RegisterMap<object, TestEntity>(_ => new TestEntity { Id = Guid.NewGuid(), Name = string.Empty });

        var validator = new InlineValidator<TestEntity>();
        validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");

        var sut = new TestCreateCommandHandler(
            new FakeLoggerFactory(), mapper, uow, new[] { validator }, userProvider);
        var command = new TestCreateCommand(new object());

        // Act
        var act = () => sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Если mapper выбрасывает исключение — оно пробрасывается наружу без перехвата.
    /// </summary>
    [Fact]
    public async Task Handle_MapperThrows_PropagatesException()
    {
        // Arrange
        var (handler, mapper, uow, userProvider, _) = CreateSut();
        mapper.RegisterMap<object, TestEntity>(_ => throw new InvalidOperationException("Map failed"));

        var sut = new TestCreateCommandHandler(
            new FakeLoggerFactory(), mapper, uow, [], userProvider);
        var command = new TestCreateCommand(new object());

        // Act
        var act = () => sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Map failed");
    }

    #endregion
}
