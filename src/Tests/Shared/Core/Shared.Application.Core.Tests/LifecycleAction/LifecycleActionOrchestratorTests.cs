// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionOrchestratorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для <see cref="LifecycleActionOrchestrator"/>.
/// Покрывают контракт оркестратора: добавление/удаление сущностей, глобальные
/// и гранулярные настройки активности, разрешение действий и диспетчеризацию
/// в нужной фазе с правильным порядком и фильтрацией.
/// </summary>
public sealed class LifecycleActionOrchestratorTests
{
    /// <summary>
    /// Тестовая сущность, для которой определены handler-ы BeforeSave и AfterSave.
    /// </summary>
    private sealed class TestEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Вторая тестовая сущность, чтобы проверить изоляцию действий по типу.
    /// </summary>
    private sealed class OtherEntity
        : IEntity
    {
        private Guid Id { get; } = Guid.NewGuid();

        object IEntity.Id => Id;
    }

    /// <summary>
    /// Handler, фиксирующий вызовы и набор сущностей, для которого он был вызван.
    /// </summary>
    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class RecordingHandler(
        LifecyclePhase phase,
        string key,
        int order = 0)
        : ILifecycleActionHandler<TestEntity>
    {
        public List<ICollection<TestEntity>> Calls { get; } = new();

        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            ExecuteAsync(
                entities.OfType<TestEntity>(),
                cancellationToken);

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken)
        {
            Calls.Add(entities.ToArray());
            return Task.CompletedTask;
        }
    }

    private static LifecycleActionOrchestrator BuildOrchestrator(
        IEnumerable<ILifecycleActionHandler>? handlers = null) =>
        new(handlers ?? [], new LifecycleEntityRegistry(), new LifecycleActionGate());

    #region AddEntities / RemoveEntities

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.AddEntities"/> добавляет
    /// сущности в карту отслеживаемых — сущности видны обработчикам.
    /// </summary>
    [Fact]
    public async Task AddEntities_MakesEntitiesEligibleForDispatch()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        var entity = new TestEntity();

        // Act
        orchestrator.AddEntities([entity]);
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Should().ContainSingle().Which.Should().BeSameAs(entity);
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.RemoveEntities"/> исключает
    /// сущности из диспетчеризации.
    /// </summary>
    [Fact]
    public async Task RemoveEntities_PreventsDispatchForThem()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        var entity = new TestEntity();

        // Act
        orchestrator.AddEntities([entity]);
        orchestrator.RemoveEntities([entity]);
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// Добавление одной и той же сущности дважды не приводит к дублям
    /// (карта — множество).
    /// </summary>
    [Fact]
    public async Task AddEntities_SameEntityTwice_NotDuplicated()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        var entity = new TestEntity();

        // Act
        orchestrator.AddEntities([entity]);
        orchestrator.AddEntities([entity]);
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Should().HaveCount(1);
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.RemoveEntities"/> для ранее
    /// не добавленной сущности не приводит к ошибке.
    /// </summary>
    [Fact]
    public void RemoveEntities_UnknownEntity_DoesNotThrow()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();

        // Act
        var act = () => orchestrator.RemoveEntities([new TestEntity()]);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetRequiredProperties

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.GetRequiredProperties"/> возвращает
    /// объединённый список навигационных свойств, объявленных handler-ами указанного типа.
    /// </summary>
    [Fact]
    public void GetRequiredProperties_AggregatesFromHandlers()
    {
        // Arrange
        var handler = new TestNavigationHandler(["Foo", "Bar"]);
        var orchestrator = BuildOrchestrator([handler]);

        // Act
        var result = orchestrator.GetRequiredProperties(typeof(TestEntity));

        // Assert
        result.Should().BeEquivalentTo("Foo", "Bar");
    }

    /// <summary>
    /// Если handler не запрашивает навигационных свойств, результат —
    /// пустой массив.
    /// </summary>
    [Fact]
    public void GetRequiredProperties_NoNavigationRequests_ReturnsEmpty()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);

        // Act
        var result = orchestrator.GetRequiredProperties(typeof(TestEntity));

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// handler-ы, чей <see cref="ILifecycleActionHandler.EntityType"/> не совпадает
    /// с запрошенным, не вносят вклад в <see cref="ILifecycleActionOrchestrator.GetRequiredProperties"/>.
    /// </summary>
    [Fact]
    public void GetRequiredProperties_OnlyForRequestedType()
    {
        // Arrange
        var navigation = new TestNavigationHandler(["Foo"]);
        var other = new OtherNavigationHandler(["Bar"]);
        var orchestrator = BuildOrchestrator([navigation, other]);

        // Act
        var forTestEntity = orchestrator.GetRequiredProperties(typeof(TestEntity));
        var forOther = orchestrator.GetRequiredProperties(typeof(OtherEntity));

        // Assert
        forTestEntity.Should().BeEquivalentTo("Foo");
        forOther.Should().BeEquivalentTo("Bar");
    }

    #endregion

    #region IsActionEnabled

    /// <summary>
    /// По умолчанию (без настроек) действие считается разрешённым.
    /// </summary>
    [Fact]
    public void IsActionEnabled_Default_ReturnsTrue()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisableActions()"/>
    /// глобально отключает все действия.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterDisableActions_GlobalReturnsFalse()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActions();

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.EnableActions()"/> после
    /// <see cref="ILifecycleActionOrchestrator.DisableActions()"/> восстанавливает
    /// разрешение.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterEnableActions_GlobalReturnsTrue()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActions();
        orchestrator.EnableActions();

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisableActions(IReadOnlyList{string})"/>
    /// отключает только перечисленные ключи, остальные остаются активными.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterDisableActionsByKey_OnlyThoseKeysDisabled()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActions(["only-this"]);

        // Act
        var disabled = orchestrator.IsActionEnabled(entity, "only-this", LifecyclePhase.BeforeSave);
        var enabled = orchestrator.IsActionEnabled(entity, "other", LifecyclePhase.BeforeSave);

        // Assert
        disabled.Should().BeFalse();
        enabled.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.EnableActions(IReadOnlyList{string})"/>
    /// повторно включает указанные ключи.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterEnableActionsByKey_RestoresThem()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActions(["k"]);
        orchestrator.EnableActions(["k"]);

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisablePhase(LifecyclePhase)"/>
    /// отключает все действия указанной фазы.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterDisablePhase_ThatPhaseReturnsFalse()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisablePhase(LifecyclePhase.BeforeSave);

        // Act
        var before = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);
        var after = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.AfterSave);

        // Assert
        before.Should().BeFalse();
        after.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.EnablePhase(LifecyclePhase)"/>
    /// повторно включает указанную фазу.
    /// </summary>
    [Fact]
    public void IsActionEnabled_AfterEnablePhase_RestoresThatPhase()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisablePhase(LifecyclePhase.BeforeSave);
        orchestrator.EnablePhase(LifecyclePhase.BeforeSave);

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisableActionForEntity"/>
    /// отключает действия для конкретной сущности (приоритет выше глобальных настроек).
    /// </summary>
    [Fact]
    public void IsActionEnabled_EntitySpecificDisable_OverridesGlobal()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActionForEntity("k", entity);

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.EnableActionForEntity"/>
    /// повторно включает действия для конкретной сущности.
    /// </summary>
    [Fact]
    public void IsActionEnabled_EntitySpecificEnable_OverridesGlobal()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActionForEntity("k", entity);
        orchestrator.EnableActionForEntity("k", entity);

        // Act
        var result = orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DisablePhaseForEntity(LifecyclePhase, IEntity)"/>
    /// отключает фазу только для указанных сущностей, остальные не затрагиваются.
    /// </summary>
    [Fact]
    public void IsActionEnabled_EntitySpecificPhaseDisable_OnlyAffectsThatEntity()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        orchestrator.DisablePhaseForEntity(LifecyclePhase.BeforeSave, entity1);

        // Act
        var e1 = orchestrator.IsActionEnabled(entity1, "k", LifecyclePhase.BeforeSave);
        var e2 = orchestrator.IsActionEnabled(entity2, "k", LifecyclePhase.BeforeSave);

        // Assert
        e1.Should().BeFalse();
        e2.Should().BeTrue();
    }

    #endregion

    #region ResetAllActions

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.ResetAllActions"/> сбрасывает
    /// все настройки активности к дефолтному состоянию.
    /// </summary>
    [Fact]
    public void ResetAllActions_ClearsAllOverrides()
    {
        // Arrange
        var orchestrator = BuildOrchestrator();
        var entity = new TestEntity();
        orchestrator.DisableActions();
        orchestrator.DisableActions(["k"]);
        orchestrator.DisablePhase(LifecyclePhase.BeforeSave);
        orchestrator.DisableActionForEntity("k", entity);
        orchestrator.DisablePhaseForEntity(LifecyclePhase.BeforeSave, entity);

        // Act
        orchestrator.ResetAllActions();

        // Assert
        orchestrator.IsActionEnabled(entity, "k", LifecyclePhase.BeforeSave).Should().BeTrue();
        orchestrator.IsActionEnabled(entity, "any", LifecyclePhase.AfterSave).Should().BeTrue();
    }

    #endregion

    #region DispatchAsync

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DispatchAsync(LifecyclePhase, CancellationToken)"/>
    /// вызывает только handler-ы указанной фазы.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_OnlyCallsHandlersOfRequestedPhase()
    {
        // Arrange
        var before = new RecordingHandler(LifecyclePhase.BeforeSave, "b");
        var after = new RecordingHandler(LifecyclePhase.AfterSave, "a");
        var orchestrator = BuildOrchestrator([before, after]);
        orchestrator.AddEntities([new TestEntity()]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        before.Calls.Should().HaveCount(1);
        after.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// handler-ы сортируются по <see cref="ILifecycleActionHandler.Order"/>
    /// в порядке возрастания.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_OrdersHandlersByOrder()
    {
        // Arrange
        var sequence = new List<string>();
        var first = new OrderRecordingHandler(LifecyclePhase.BeforeSave, "first", order: 1, sequence);
        var second = new OrderRecordingHandler(LifecyclePhase.BeforeSave, "second", order: 2, sequence);
        var zero = new OrderRecordingHandler(LifecyclePhase.BeforeSave, "zero", order: 0, sequence);
        var orchestrator = BuildOrchestrator([first, second, zero]);
        orchestrator.AddEntities([new TestEntity()]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        sequence.Should().Equal("zero", "first", "second");
    }

    /// <summary>
    /// Если для handler-а нет отслеживаемых сущностей нужного типа,
    /// handler не вызывается.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_NoEligibleEntities_HandlerNotCalled()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// Если действие глобально отключено, handler не вызывается.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_GlobalDisabled_HandlerNotCalled()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new TestEntity()]);
        orchestrator.DisableActions();

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// Если фаза отключена, handler этой фазы не вызывается.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_PhaseDisabled_HandlerNotCalled()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new TestEntity()]);
        orchestrator.DisablePhase(LifecyclePhase.BeforeSave);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// Если конкретный ключ отключён, handler с этим ключом не вызывается.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_KeyDisabled_HandlerNotCalled()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "k");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new TestEntity()]);
        orchestrator.DisableActions(["k"]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// Если для конкретной сущности действие отключено, эта сущность
    /// не попадает в коллекцию, переданную в handler.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_EntitySpecificDisabled_EntityExcluded()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        var excluded = new TestEntity();
        var included = new TestEntity();
        orchestrator.AddEntities([excluded, included]);
        orchestrator.DisableActionForEntity("h", excluded);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Should().ContainSingle().Which.Should().BeSameAs(included);
        handler.Calls[0].Should().NotContain(excluded);
    }

    /// <summary>
    /// handler-ы, чей <see cref="ILifecycleActionHandler.EntityType"/> не совпадает
    /// с типом отслеживаемых сущностей, не получают эти сущности.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_TypeMismatch_EntityNotDelivered()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new OtherEntity()]);

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, CancellationToken.None);

        // Assert
        handler.Calls.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DispatchAsync(LifecyclePhase, CancellationToken)"/>
    /// пробрасывает <see cref="CancellationToken"/> в handler.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_PassesCancellationTokenToHandler()
    {
        // Arrange
        var handler = new TokenCapturingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new TestEntity()]);
        using var cts = new CancellationTokenSource();

        // Act
        await orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, cts.Token);

        // Assert
        handler.ReceivedToken.Should().Be(cts.Token);
    }

    /// <summary>
    /// <see cref="ILifecycleActionOrchestrator.DispatchAsync(LifecyclePhase, CancellationToken)"/>
    /// бросает <see cref="OperationCanceledException"/> при отменённом токене
    /// до запуска первого handler.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_CancelledToken_ThrowsBeforeAnyHandler()
    {
        // Arrange
        var handler = new RecordingHandler(LifecyclePhase.BeforeSave, "h");
        var orchestrator = BuildOrchestrator([handler]);
        orchestrator.AddEntities([new TestEntity()]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => orchestrator.DispatchAsync(LifecyclePhase.BeforeSave, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Calls.Should().BeEmpty();
    }

    #endregion

    // ---- Вспомогательные handler-ы, используемые в нескольких тестах ----

    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class TestNavigationHandler(string[] navigation)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = LifecyclePhase.BeforeSave;

        public string Key { get; } = "nav";

        public int Order => 0;

        public string[] RequiredNavigationProperties => navigation;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class OtherNavigationHandler(string[] navigation)
        : ILifecycleActionHandler<OtherEntity>
    {
        public LifecyclePhase Phase { get; } = LifecyclePhase.BeforeSave;

        public string Key { get; } = "other-nav";

        public int Order => 0;

        public string[] RequiredNavigationProperties => navigation;

        Type ILifecycleActionHandler.EntityType => typeof(OtherEntity);

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ExecuteAsync(
            IEnumerable<OtherEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class OrderRecordingHandler(
        LifecyclePhase phase,
        string key,
        int order,
        List<string> sequence)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order { get; } = order;

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            sequence.Add(Key);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Shared.Application.Core.DependencyInjection.Attributes.ManualConfiguration]
    private sealed class TokenCapturingHandler(
        LifecyclePhase phase,
        string key)
        : ILifecycleActionHandler<TestEntity>
    {
        public LifecyclePhase Phase { get; } = phase;

        public string Key { get; } = key;

        public int Order => 0;

        public CancellationToken ReceivedToken { get; private set; }

        Type ILifecycleActionHandler.EntityType => typeof(TestEntity);

        public string[] RequiredNavigationProperties => [];

        public Task ExecuteAsync(
            IEnumerable<IEntity> entities,
            CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
