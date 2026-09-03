// ----------------------------------------------------------------------------------------------
// <copyright file="IncludedBasesApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

/// <summary>
/// Применяет <see cref="IMapDescription.IncludedBases"/> и <see cref="IMapDescription.HasReverseMap"/>
/// к Mapster-сеттеру.
/// </summary>
internal sealed class IncludedBasesApplicator
    : IDescriptionApplicator
{
    private static readonly MethodInfo InheritsExtensionMethod = typeof(TypeAdapterSetterExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m =>
            m is { Name: nameof(TypeAdapterSetter<object, object>.Inherits), IsGenericMethodDefinition: true }
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType.IsGenericParameter
            && m.GetParameters()[1].ParameterType == typeof(Type)
            && m.GetParameters()[2].ParameterType == typeof(Type));

    /// <inheritdoc />
    public void Apply(object setter, IMapDescription description)
    {
        if (description.HasReverseMap)
        {
            ApplyReverseMap(setter);
        }

        if (description.IncludedBases.Count > 0)
        {
            ApplyIncludedBases(setter, description);
        }
    }

    private static void ApplyReverseMap(object setter)
    {
        var twoWays = setter.GetType().GetMethod(
            nameof(TypeAdapterSetter<object, object>.TwoWays),
            Type.EmptyTypes);
        twoWays?.Invoke(setter, null);
    }

    private static void ApplyIncludedBases(object setter, IMapDescription description)
    {
        var closed = InheritsExtensionMethod.MakeGenericMethod(setter.GetType());
        foreach (var (srcBase, destBase) in description.IncludedBases)
        {
            closed.Invoke(null, [setter, srcBase, destBase]);
        }
    }
}
