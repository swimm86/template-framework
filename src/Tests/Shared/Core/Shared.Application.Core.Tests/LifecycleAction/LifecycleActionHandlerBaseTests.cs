// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionHandlerBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для базового класса <see cref="LifecycleActionHandlerBase{TEntity}"/>.
/// Проверяют абстрактный контракт: фильтрацию по типу, пустые коллекции,
/// порядок вызова и проброс <see cref="CancellationToken"/>.
/// </summary>
public sealed class LifecycleActionHandlerBaseTests
{
    /// <summary>
    /// Тестовая сущность для проверки фильтрации по типу в handler.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Другая сущность, не совпадающая с типом handler, для проверки фильтрации.
    /// </summary>
    private sealed class OtherEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, фиксирующий вызовы и передаваемые в <c>ExecuteActionAsync</c> сущности и токен.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class RecordingHandler
        : LifecycleActionHandlerBase<TestEntity>
    {
        public List<ICollection<TestEntity>> Calls { get; } = new();

        public List<CancellationToken> Tokens { get; } = new();

        public override LifecyclePhase Phase => LifecyclePhase.BeforeSave;

        public override string Key => "recording";

        public override int Order => 0;

        protected override Task ExecuteActionAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken)
        {
            Calls.Add(entities.ToArray());
            Tokens.Add(cancellationToken);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// <see cref="ILifecycleActionHandler.EntityType"/> должен возвращать
    /// <c>typeof(TEntity)</c> для типизированного handler.
    /// </summary>
    [Fact]
    public void EntityType_ReturnsTEntityType()
    {
        // Arrange
        ILifecycleActionHandler handler = new RecordingHandler();

        // Act
        var type = handler.EntityType;

        // Assert
        type.Should().Be<TestEntity>();
    }

    /// <summary>
    /// Неgeneric-метод <see cref="ILifecycleActionHandler.ExecuteAsync"/>
    /// фильтрует сущности по типу <see cref="TestEntity"/> (обобщение handler)
    /// и передаёт только подходящие в <c>ExecuteActionAsync</c>.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NonGeneric_FiltersEntitiesByType()
    {
        // Arrange
        var handler = new RecordingHandler();
        ICollection< IEntity> mixed =
        [
            new TestEntity(),
            new OtherEntity(),
            new TestEntity(),
        ];

        // Act
        await handler.ExecuteAsync(mixed, CancellationToken.None);

        // Assert
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Should().HaveCount(2);
        handler.Calls[0].Should().AllBeOfType<TestEntity>();
    }

    /// <summary>
    /// Если в коллекции нет сущностей нужного типа, типизированный
    /// <c>ExecuteActionAsync</c> не вызывается (no-op).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_EmptyFilteredCollection_DoesNotCallExecuteAction()
    {
        // Arrange
        var handler = new RecordingHandler();
        ICollection<IEntity> onlyOthers = [new OtherEntity(), new OtherEntity()];

        // Act
        await handler.ExecuteAsync(onlyOthers, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="CancellationToken"/> пробрасывается из неgeneric-метода
    /// в <c>ExecuteActionAsync</c> без изменений.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToExecuteAction()
    {
        // Arrange
        var handler = new RecordingHandler();
        using var cts = new CancellationTokenSource();

        // Act
        await handler.ExecuteAsync((ICollection<TestEntity>)[new TestEntity()], cts.Token);

        // Assert
        handler.Tokens.Should().ContainSingle().Which.Should().Be(cts.Token);
    }

    /// <summary>
    /// Дефолтное значение <see cref="ILifecycleActionHandler.RequiredNavigationProperties"/>
    /// должно быть пустым массивом (handler не запрашивает навигации по умолчанию).
    /// </summary>
    [Fact]
    public void RequiredNavigationProperties_Default_IsEmptyArray()
    {
        // Arrange
        var handler = new RecordingHandler();

        // Act
        var required = handler.RequiredNavigationProperties;

        // Assert
        required.Should().BeEmpty();
    }
}
