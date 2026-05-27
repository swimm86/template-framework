using Shared.Domain.Core.Base;
using Shared.Domain.Core.LifecycleAction;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

/// <summary>
/// Тестовая сущность с дублирующимися ключами действий перехвата.
/// </summary>
public sealed class TestBaseEntityWithDuplicateActionKeys
    : BaseEntity<Guid>
{
    protected override IEntityLifecycleAction[] BeforeSaveActions =>
    [
        new EntityLifecycleAction(TestEventKey.Before, (_, _) => Task.CompletedTask),
        new EntityLifecycleAction(TestEventKey.Before, (_, _) => Task.CompletedTask),
    ];
}
