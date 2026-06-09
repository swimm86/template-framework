// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleEntityRegistryTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="LifecycleEntityRegistry"/>: идемпотентность
/// <see cref="ILifecycleEntityRegistry.Track"/>, удаление через
/// <see cref="ILifecycleEntityRegistry.Untrack"/>, стабильность
/// идентичности через <see cref="EntityKey"/>, <see cref="ILifecycleEntityRegistry.Clear"/>.
/// </summary>
public sealed class LifecycleEntityRegistryTests
{
    /// <summary>
    /// Тестовая сущность.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Вторая тестовая сущность с собственным типом.
    /// </summary>
    private sealed class OtherEntity
        : IEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Track"/> добавляет сущности
    /// в снимок <see cref="ILifecycleEntityRegistry.Snapshot"/>.
    /// </summary>
    [Fact]
    public void Track_NewEntities_AppearInSnapshot()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        var a = new TestEntity();
        var b = new TestEntity();

        // Act
        registry.Track([a, b]);

        // Assert
        registry.Snapshot().Should().BeEquivalentTo(new IEntity[] { a, b });
    }

    /// <summary>
    /// Повторный <see cref="ILifecycleEntityRegistry.Track"/> той же сущности
    /// не приводит к дубликатам (семантика множества).
    /// </summary>
    [Fact]
    public void Track_SameEntityTwice_NotDuplicated()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        var entity = new TestEntity();

        // Act
        registry.Track([entity]);
        registry.Track([entity]);

        // Assert
        registry.Snapshot().Should().HaveCount(1);
    }

    /// <summary>
    /// Разные экземпляры с одинаковым <c>(Type, Id)</c> трактуются
    /// как один и тот же ключ; последний записанный выигрывает.
    /// </summary>
    [Fact]
    public void Track_DifferentInstancesSameTypeAndId_LastWriteWins()
    {
        // Arrange
        var id = Guid.NewGuid();
        var first = new TestEntity { Id = id };
        var second = new TestEntity { Id = id };
        var registry = new LifecycleEntityRegistry();

        // Act
        registry.Track([first]);
        registry.Track([second]);

        // Assert
        var snapshot = registry.Snapshot();
        snapshot.Should().HaveCount(1);
        snapshot.Single().Should().BeSameAs(second);
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Track"/> принимает разные типы
    /// сущностей с одинаковым <c>Id</c> независимо — Type участвует в ключе.
    /// </summary>
    [Fact]
    public void Track_DifferentTypes_TrackedIndependently()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity { Id = id };
        var b = new OtherEntity { Id = id };
        var registry = new LifecycleEntityRegistry();

        // Act
        registry.Track([a, b]);

        // Assert
        registry.Snapshot().Should().BeEquivalentTo(new IEntity[] { a, b });
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Track"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при <c>null</c> коллекции.
    /// </summary>
    [Fact]
    public void Track_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();

        // Act
        Action act = () => registry.Track(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Track"/> выбрасывает
    /// <see cref="ArgumentNullException"/> при элементе <c>null</c>
    /// внутри коллекции.
    /// </summary>
    [Fact]
    public void Track_CollectionContainingNull_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();

        // Act
        Action act = () => registry.Track([null]!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Untrack"/> удаляет сущности
    /// из снимка.
    /// </summary>
    [Fact]
    public void Untrack_RemovesEntityFromSnapshot()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        var a = new TestEntity();
        var b = new TestEntity();
        registry.Track([a, b]);

        // Act
        registry.Untrack([a]);

        // Assert
        registry.Snapshot().Should().BeEquivalentTo(new IEntity[] { b });
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Untrack"/> для незарегистрированной
    /// сущности — безопасная no-op.
    /// </summary>
    [Fact]
    public void Untrack_UnknownEntity_DoesNotThrow()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        var tracked = new TestEntity();
        var stranger = new TestEntity();
        registry.Track([tracked]);

        // Act
        Action act = () => registry.Untrack(new IEntity[] { stranger });

        // Assert
        act.Should().NotThrow();
        registry.Snapshot().Should().HaveCount(1);
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Untrack"/> удаляет по
    /// <c>(Type, Id)</c>, а не по ссылочной идентичности —
    /// можно снять регистрацию, передав другой экземпляр с тем же Id.
    /// </summary>
    [Fact]
    public void Untrack_DifferentInstanceSameTypeAndId_RemovesTracked()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tracked = new TestEntity { Id = id };
        var substitute = new TestEntity { Id = id };
        var registry = new LifecycleEntityRegistry();
        registry.Track([tracked]);

        // Act
        registry.Untrack([substitute]);

        // Assert
        registry.Snapshot().Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Snapshot"/> пустого реестра
    /// возвращает пустую коллекцию.
    /// </summary>
    [Fact]
    public void Snapshot_EmptyRegistry_ReturnsEmpty()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();

        // Act
        var snapshot = registry.Snapshot();

        // Assert
        snapshot.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Snapshot"/> возвращает
    /// <see cref="IReadOnlyCollection{T}"/>, не допускающий мутации.
    /// </summary>
    [Fact]
    public void Snapshot_ReturnsReadOnlyCollection()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        registry.Track([new TestEntity()]);

        // Act
        var snapshot = registry.Snapshot();

        // Assert: компилятор + рантайм — тип IReadOnlyCollection
        snapshot.Should().BeAssignableTo<IReadOnlyCollection<IEntity>>();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Clear"/> очищает все
    /// отслеживаемые сущности.
    /// </summary>
    [Fact]
    public void Clear_EmptiesAllTrackedEntities()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();
        registry.Track([new TestEntity(), new TestEntity(), new TestEntity()]);

        // Act
        registry.Clear();

        // Assert
        registry.Snapshot().Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="ILifecycleEntityRegistry.Clear"/> на пустом реестре
    /// — безопасная no-op.
    /// </summary>
    [Fact]
    public void Clear_OnEmpty_DoesNotThrow()
    {
        // Arrange
        var registry = new LifecycleEntityRegistry();

        // Act
        Action act = () => registry.Clear();

        // Assert
        act.Should().NotThrow();
        registry.Snapshot().Should().BeEmpty();
    }
}
