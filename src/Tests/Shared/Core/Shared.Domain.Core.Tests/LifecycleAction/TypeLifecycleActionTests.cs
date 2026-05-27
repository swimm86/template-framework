using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.LifecycleAction;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.LifecycleAction;

/// <summary>
/// Тесты для доменных событий, основанных на типе, проверяющие передачу делегатов и параметров.
/// </summary>
public sealed class TypeLifecycleActionTests
{
    /// <summary>
    /// Проверяет, что ProcessActionAsync вызывает делегат с переданными ServiceProvider и коллекцией сущностей.
    /// </summary>
    [Fact]
    public async Task ProcessActionAsync_CallsDelegateWithServiceProviderAndEntities()
    {
        // Arrange
        IServiceProvider? receivedServiceProvider = null;
        ICollection<IWithLifecycleActions>? receivedEntities = null;
        var stub = new TypeLifecycleActionStub(
            TestEnum.BeforeCreate,
            (sp, entities, _) =>
            {
                receivedServiceProvider = sp;
                receivedEntities = entities;
                return Task.CompletedTask;
            });

        var serviceProvider = new TestServiceProvider();
        var entities = new List<IWithLifecycleActions>();

        // Act
        await stub.CallExecuteActionAsync(serviceProvider, entities, CancellationToken.None);

        // Assert
        receivedServiceProvider.Should().BeSameAs(serviceProvider);
        receivedEntities.Should().BeSameAs(entities);
    }

    /// <summary>
    /// Проверяет, что коллекция сущностей передаётся в делегат без изменений.
    /// </summary>
    [Fact]
    public async Task Entities_CollectionIsPassedToDelegate()
    {
        // Arrange
        ICollection<IWithLifecycleActions>? receivedEntities = null;
        var stub = new TypeLifecycleActionStub(
            TestEnum.BeforeCreate,
            (_, entities, _) =>
            {
                receivedEntities = entities;
                return Task.CompletedTask;
            });

        var entities = new List<IWithLifecycleActions> { new TestEntityWithLifecycleActions() };

        // Act
        await stub.CallExecuteActionAsync(null!, entities, CancellationToken.None);

        // Assert
        receivedEntities.Should().NotBeNull();
        receivedEntities.Should().HaveCount(1);
    }

    /// <summary>
    /// Проверяет, что DisableEntitiesActions отключает действия у переданных сущностей.
    /// </summary>
    [Fact]
    public async Task DisableEntitiesActions_DisablesEntities()
    {
        // Arrange
        var entityActionExecuted = false;
        var entityAction = new TypeLifecycleActionStub(
            TestEnum.BeforeCreate,
            (_, _, _) =>
            {
                entityActionExecuted = true;
                return Task.CompletedTask;
            });
        entityAction.Enable();

        var stub = new TypeLifecycleActionStub(
            TestEnum.BeforeCreate,
            (_, _, _) => Task.CompletedTask);
        stub.Enable();

        var entity = new TestEntityWithLifecycleActions();
        entity.AddAction(LifecycleHookType.BeforeSave, TestEnum.BeforeCreate, entityAction);

        // Act
        stub.CallDisableEntitiesActions(LifecycleHookType.BeforeSave, [entity]);

        // Assert
        entity.TryGetAction(
            LifecycleHookType.BeforeSave,
            TestEnum.BeforeCreate,
            out var storedAction);
        storedAction.Should().NotBeNull();
        storedAction.Should().BeSameAs(entityAction);

        await storedAction.ExecuteAsync(
            LifecycleHookType.BeforeSave,
            null!,
            [entity],
            CancellationToken.None);

        entityActionExecuted.Should().BeFalse();
    }

    private sealed class TestEntityWithLifecycleActions
        : IWithLifecycleActions
    {
        public string[] RequiredToSaveNavigationPropertiesNames => [];

        private readonly Dictionary<(LifecycleHookType, Enum), IEntityLifecycleAction> _actions = new();

        public void AddAction(LifecycleHookType hookType, Enum key, IEntityLifecycleAction lifecycleAction)
            => _actions[(hookType, key)] = lifecycleAction;

        public bool TryGetAction(
            LifecycleHookType hookType,
            Enum key,
            out IEntityLifecycleAction lifecycleAction)
        {
            if (_actions.TryGetValue((hookType, key), out var e))
            {
                lifecycleAction = e;
                return true;
            }

            lifecycleAction = null!;
            return false;
        }

        public void ResetActions() { }

        public ICollection<Enum> GetAllKeys(LifecycleHookType hookType)
            => _actions.Where(x => x.Key.Item1 == hookType).Select(x => x.Key.Item2).ToList();
    }
}
