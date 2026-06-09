// ----------------------------------------------------------------------------------------------
// <copyright file="PersonHashLifecycleHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction;
using Shared.Common.Extensions;
using Shared.Domain.Core.Enums;

namespace Template.Setter.Application.LifecycleAction.Person;

/// <summary>
/// Обработчик действий перехвата для коллекции сущностей "Person", который
/// гарантирует, что при изменении соответствующих сущностей хэш будет пересчитан.
/// </summary>
public class PersonHashLifecycleHandler
    : LifecycleActionHandlerBase<Domain.Entities.Person>
{
    /// <inheritdoc />
    public override LifecyclePhase Phase => LifecyclePhase.BeforeSave;

    /// <inheritdoc />
    public override string Key => "CalculatePersonHashBeforeSavingChanges";

    /// <inheritdoc />
    public override int Order => 0;

    /// <inheritdoc />
    protected override Task ExecuteActionAsync(
        IEnumerable<Domain.Entities.Person> entities,
        CancellationToken cancellationToken)
    {
        entities.ForEach(x => x.UpdateHash());
        return Task.CompletedTask;
    }
}