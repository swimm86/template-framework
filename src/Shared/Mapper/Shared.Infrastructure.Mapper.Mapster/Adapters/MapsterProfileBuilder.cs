// ----------------------------------------------------------------------------------------------
// <copyright file="MapsterProfileBuilder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Mapster;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Adapters.Applicators;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters;

/// <summary>
/// Адаптер, применяющий провайдеро-независимое описание профиля (<see cref="IMappingProfile"/>)
/// к конкретному <see cref="TypeAdapterConfig"/>.
/// </summary>
/// <remarks>
/// <para>
/// Поскольку <see cref="IMapDescription"/> хранит типы в виде <see cref="Type"/> без generic-параметров,
/// вся работа с generic-методами Mapster делается через reflection в applicator-классах.
/// </para>
/// <para>
/// Для обратного преобразования (mapping) (<c>HasReverseMap</c>) используется <c>TwoWays()</c> на forward-конфиге.
/// </para>
/// </remarks>
internal sealed class MapsterProfileBuilder
    : MapperProfileBuilderBase<TypeAdapterConfig>
{
    private static readonly MethodInfo NewConfigGenericMethod = typeof(TypeAdapterConfig)
        .GetMethods()
        .First(m =>
            m is { Name: nameof(TypeAdapterConfig.NewConfig), IsGenericMethodDefinition: true }
            && m.GetParameters().Length == 0);

    /// <inheritdoc />
    protected override ICollection<IDescriptionApplicator> Applicators =>
    [
        new MemberApplicator(),
        new CtorParamApplicator(),
        new ConstructUsingApplicator(),
        new ConverterApplicator(),
        new BeforeAfterMapApplicator(),
        new IncludedBasesApplicator(),
    ];

    /// <inheritdoc />
    protected override object CreateMappingContext(
        TypeAdapterConfig config,
        Type sourceType,
        Type destinationType)
    {
        var newConfigMethod = NewConfigGenericMethod.MakeGenericMethod(sourceType, destinationType);
        return newConfigMethod.Invoke(config, null)
            ?? throw new InvalidOperationException(
                $"{nameof(TypeAdapterConfig.NewConfig)}<{sourceType.Name}, {destinationType.Name}>() returned null.");
    }
}
