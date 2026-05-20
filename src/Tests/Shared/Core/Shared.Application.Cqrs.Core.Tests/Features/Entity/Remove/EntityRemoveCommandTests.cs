using Shared.Application.Cqrs.Core.Features.Entity.Remove;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Features.Entity.Remove;

/// <summary>
/// Тесты record-типа <see cref="EntityRemoveCommand{TEntity}"/>.
/// Проверяют инициализацию, иммутабельность через <c>with</c>,
/// значения по умолчанию и иерархию типов.
/// </summary>
public sealed class EntityRemoveCommandTests
{
    #region Key and Request Tests

    /// <summary>
    /// Свойство <c>Key</c> команды возвращает идентификатор из запроса.
    /// </summary>
    [Fact]
    public void Command_Key_ShouldEqual_RequestId()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new EntityRemoveRequest { Id = requestId };

        // Act
        var command = new EntityRemoveCommand<TestEntity>(request);

        // Assert
        command.Key.Should().Be(requestId);
        command.Request.Should().Be(request);
    }

    /// <summary>
    /// При использовании <c>with { Request = newRequest }</c>
    /// меняется только <c>Request</c>, а <c>Key</c> остаётся прежним.
    /// </summary>
    [Fact]
    public void Command_With_UpdatedRequest_ShouldChangeRequestButKeepKey()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var originalRequest = new EntityRemoveRequest { Id = originalId };
        var newRequest = new EntityRemoveRequest { Id = newId };

        var command = new EntityRemoveCommand<TestEntity>(originalRequest);

        // Act
        var updated = command with { Request = newRequest };

        // Assert
        updated.Request.Should().Be(newRequest);
        command.Key.Should().Be(originalId);
        command.Request.Should().Be(originalRequest);
    }

    /// <summary>
    /// Для запроса с <c>default(Guid)</c> свойство <c>Key</c>
    /// возвращает <see cref="Guid.Empty"/>.
    /// </summary>
    [Fact]
    public void Command_WithDefaultId_ShouldHaveEmptyGuidKey()
    {
        // Arrange
        var request = new EntityRemoveRequest();

        // Act
        var command = new EntityRemoveCommand<TestEntity>(request);

        // Assert
        command.Key.Should().Be(Guid.Empty);
        command.Request.Id.Should().Be(Guid.Empty);
    }

    #endregion

    #region Type Hierarchy Tests

    /// <summary>
    /// Производный <c>TestEntityRemoveCommand</c> должен быть
    /// совместим по присваиванию с базовым <see cref="EntityRemoveCommand{TEntity}"/>.
    /// </summary>
    [Fact]
    public void Command_IsCorrectRecordType()
    {
        // Arrange
        var command = new TestEntityRemoveCommand(new EntityRemoveRequest { Id = Guid.NewGuid() });

        // Assert
        command.Should().BeAssignableTo<EntityRemoveCommand<TestEntity>>();
    }

    #endregion
}
