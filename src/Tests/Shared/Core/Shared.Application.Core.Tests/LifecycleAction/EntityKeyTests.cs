// ----------------------------------------------------------------------------------------------
// <copyright file="EntityKeyTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="EntityKey"/>: value-семантика record struct,
/// фабрика <see cref="EntityKey.Of"/>, безопасность относительно <c>null</c>.
/// </summary>
public sealed class EntityKeyTests
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
    /// Вторая тестовая сущность с собственной реализацией <see cref="IEntity"/>.
    /// </summary>
    private sealed class OtherEntity
        : IEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// <see cref="EntityKey.Of"/> для одной и той же сущности возвращает
    /// структурно равные ключи (value-семантика record struct).
    /// </summary>
    [Fact]
    public void Of_SameInstance_ProducesEqualKey()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        var key1 = EntityKey.Of(entity);
        var key2 = EntityKey.Of(entity);

        // Assert
        key1.Should().Be(key2);
    }

    /// <summary>
    /// Два разных экземпляра одной сущности с одинаковым <c>(Type, Id)</c>
    /// дают одинаковый ключ — это и есть основная мотивация
    /// <see cref="EntityKey"/> (стабильная идентичность при подмене инстанса).
    /// </summary>
    [Fact]
    public void Of_DifferentInstancesSameTypeAndId_ProducesEqualKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity { Id = id };
        var b = new TestEntity { Id = id };

        // Act
        var keyA = EntityKey.Of(a);
        var keyB = EntityKey.Of(b);

        // Assert
        keyA.Should().Be(keyB, "EntityKey определяется через (Type, Id), а не ссылочную идентичность");
    }

    /// <summary>
    /// Разные типы при одинаковом <c>Id</c> дают **неравные** ключи —
    /// Type участвует в сравнении.
    /// </summary>
    [Fact]
    public void Of_DifferentTypesSameId_NotEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity { Id = id };
        var b = new OtherEntity { Id = id };

        // Act
        var keyA = EntityKey.Of(a);
        var keyB = EntityKey.Of(b);

        // Assert
        keyA.Should().NotBe(keyB);
    }

    /// <summary>
    /// Один тип, разные <c>Id</c> — ключи неравны.
    /// </summary>
    [Fact]
    public void Of_SameTypeDifferentIds_NotEqual()
    {
        // Arrange
        var a = new TestEntity();
        var b = new TestEntity();

        // Act
        var keyA = EntityKey.Of(a);
        var keyB = EntityKey.Of(b);

        // Assert
        keyA.Should().NotBe(keyB);
    }

    /// <summary>
    /// Структурно равные ключи дают одинаковый <c>GetHashCode</c>,
    /// что критично для корректной работы <c>Dictionary&lt;EntityKey, ...&gt;</c>.
    /// </summary>
    [Fact]
    public void EqualsAndHashCode_AreConsistent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity { Id = id };
        var b = new TestEntity { Id = id };

        var keyA = EntityKey.Of(a);
        var keyB = EntityKey.Of(b);

        // Assert
        keyA.Equals(keyB).Should().BeTrue();
        keyA.GetHashCode().Should().Be(keyB.GetHashCode());
    }

    /// <summary>
    /// <see cref="EntityKey.Of"/> использует runtime-тип сущности
    /// (<c>entity.GetType()</c>), а не статический <c>IEntity</c>.
    /// </summary>
    [Fact]
    public void Of_TypeProperty_EqualsEntitiesRuntimeType()
    {
        // Arrange
        IEntity entity = new TestEntity();

        // Act
        var key = EntityKey.Of(entity);

        // Assert
        key.Type.Should().Be<TestEntity>();
    }

    /// <summary>
    /// <see cref="EntityKey.Of"/> фиксирует runtime-тип сущности
    /// (<c>entity.GetType()</c>), а не compile-time-тип ссылки.
    /// Для наследника это даёт <c>DerivedEntity</c>, не базовый тип —
    /// это сознательный выбор, позволяющий различать per-entity
    /// настройки между базой и потомками.
    /// </summary>
    [Fact]
    public void Of_DerivedEntity_StoresRuntimeTypeNotDeclaredType()
    {
        // Arrange
        IEntity baseRef = new DerivedEntity();

        // Act
        var key = EntityKey.Of(baseRef);

        // Assert
        key.Type.Should().Be<DerivedEntity>();
        key.Type.Should().NotBe(typeof(IEntity));
    }

    /// <summary>
    /// Производная сущность для проверки runtime-типа.
    /// </summary>
    private sealed class DerivedEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// <see cref="EntityKey.Of"/> для <c>null</c> выбрасывает
    /// <see cref="ArgumentNullException"/> — null-safety контракт.
    /// </summary>
    [Fact]
    public void Of_NullEntity_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => EntityKey.Of(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Конструктор <see cref="EntityKey"/> сохраняет переданные
    /// <c>Type</c> и <c>Id</c> без изменений.
    /// </summary>
    [Fact]
    public void Ctor_StoresTypeAndId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var key = new EntityKey(typeof(TestEntity), id);

        // Assert
        key.Type.Should().Be<TestEntity>();
        key.Id.Should().Be(id);
    }

    /// <summary>
    /// Документирует поведение <see cref="EntityKey.Of"/> для сущности
    /// с <c>null</c>-значением <c>Id</c>: <see cref="IEntity.Id"/> возвращает
    /// <c>null</c>, и ключ создаётся с <see cref="EntityKey.Id"/> = <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Это пограничный кейс: формально <see cref="EntityKey.Of"/> НЕ бросает
    /// исключение, потому что <c>Id</c> имеет тип <see cref="object"/>
    /// и теоретически может быть <c>null</c>. На практике это означает, что
    /// сущности без инициализированного <c>Id</c> будут иметь один и тот же
    /// ключ в реестре — последняя запись выиграет.
    /// </para>
    /// <para>
    /// Этот тест фиксирует текущее поведение. Если в будущем потребуется
    /// бросать <see cref="ArgumentException"/> для <c>null</c> Id,
    /// тест нужно обновить.
    /// </para>
    /// </remarks>
    [Fact]
    public void Of_EntityWithNullId_CreatesKeyWithNullId()
    {
        // Arrange
        var entity = new NullIdEntity();

        // Act
        var key = EntityKey.Of(entity);

        // Assert
        key.Type.Should().Be<NullIdEntity>();
        key.Id.Should().BeNull("EntityKey допускает null-Id — это сознательный компромисс, "
            + "документирующий поведение для не-fully-initialized entities");
    }

    /// <summary>
    /// <see cref="EntityKey.Of"/> для двух разных экземпляров
    /// с <c>null</c> Id даёт одинаковый ключ (LastWriteWins в реестре).
    /// </summary>
    [Fact]
    public void Of_TwoEntitiesWithNullId_ProduceEqualKeys()
    {
        // Arrange
        var first = new NullIdEntity();
        var second = new NullIdEntity();

        // Act
        var keyFirst = EntityKey.Of(first);
        var keySecond = EntityKey.Of(second);

        // Assert
        keyFirst.Should().Be(keySecond,
            "EntityKey использует value-семантику: одинаковые (Type, Id=null) дают равные ключи");
    }

    /// <summary>
    /// Тестовая сущность, у которой <see cref="IEntity.Id"/> всегда
    /// возвращает <c>null</c> (имитация не-fully-initialized entity).
    /// </summary>
    /// <remarks>
    /// Подавление <c>CS8767</c> обосновано: <see cref="IEntity.Id"/> объявлен
    /// как non-nullable, но мы сознательно симулируем null-сценарий для
    /// проверки <see cref="EntityKey"/>. Это не production-код.
    /// </remarks>
    private sealed class NullIdEntity
        : IEntity
    {
        object IEntity.Id => null!;
    }
}
