// ----------------------------------------------------------------------------------------------
// <copyright file="TypeLifecycleAction.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.LifecycleAction;

/// <summary>
/// Действие перехвата для коллекции сущностей одного типа.
/// </summary>
public class TypeLifecycleAction
    : EntityLifecycleActionBase
{
    private readonly Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> _action;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TypeLifecycleAction"/>.
    /// </summary>
    /// <param name="key">Ключ действия.</param>
    /// <param name="action">Действие, которое реализует перехват.</param>
    public TypeLifecycleAction(
        Enum key,
        Func<IServiceProvider, ICollection<IWithLifecycleActions>, CancellationToken, Task> action)
        : base(key)
    {
        _action = action;
    }

    /// <inheritdoc />
    protected override Task ExecuteActionAsync(
        IServiceProvider serviceProvider,
        ICollection<IWithLifecycleActions> entities,
        CancellationToken cancellationToken)
        => _action(serviceProvider, entities, cancellationToken);
}
