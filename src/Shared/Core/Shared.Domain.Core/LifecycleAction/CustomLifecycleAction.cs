// ----------------------------------------------------------------------------------------------
// <copyright file="CustomLifecycleAction.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.LifecycleAction;

/// <summary>
/// Кастомное действие перехвата (primary constructor).
/// </summary>
public class CustomLifecycleAction(
    Enum key,
    Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
    : EntityLifecycleActionBase(key)
{
    /// <inheritdoc />
    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken) =>
        action(serviceProvider, entities, cancellationToken);
}
