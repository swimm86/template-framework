// ----------------------------------------------------------------------------------------------
// <copyright file="BeforeAfterMapApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет <see cref="IMapDescription.BeforeMap"/> и <see cref="IMapDescription.AfterMap"/> к Mapster-сеттеру.
/// </summary>
internal sealed class BeforeAfterMapApplicator : IDescriptionApplicator
{
    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.BeforeMap is not null)
        {
            ApplyAction(
                setter,
                description.SourceType,
                description.DestinationType,
                description.BeforeMap,
                methodName: nameof(TypeAdapterSetter<object, object>.BeforeMapping));
        }

        if (description.AfterMap is not null)
        {
            ApplyAction(
                setter,
                description.SourceType,
                description.DestinationType,
                description.AfterMap,
                methodName: nameof(TypeAdapterSetter<object, object>.AfterMapping));
        }
    }

    private static void ApplyAction(
        object setter,
        Type sourceType,
        Type destinationType,
        Action<object, object> action,
        string methodName)
    {
        var setterType = setter.GetType();
        var actionType = typeof(Action<,>).MakeGenericType(sourceType, destinationType);

        var method = setterType.GetMethods()
            .FirstOrDefault(m =>
                m.Name == methodName
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == actionType);

        if (method is null)
        {
            return;
        }

        var invokeMethod = action.GetType().GetMethod(nameof(Action.Invoke))!;
        var del = invokeMethod.CreateDelegate(actionType, action);

        method.Invoke(setter, [del]);
    }
}
