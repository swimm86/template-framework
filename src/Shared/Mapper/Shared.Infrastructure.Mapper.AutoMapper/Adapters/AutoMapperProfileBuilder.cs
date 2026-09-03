// ----------------------------------------------------------------------------------------------
// <copyright file="AutoMapperProfileBuilder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using AutoMapper;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.Applicators;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder;
using Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters;

/// <summary>
/// Адаптер, преобразующий провайдеро-независимое описание профиля (<see cref="IMappingProfile"/>)
/// в конкретный <see cref="global::AutoMapper.Profile"/>.
/// </summary>
/// <remarks>
/// Поскольку <see cref="IMapDescription"/> хранит типы в виде <see cref="Type"/> без generic-параметров,
/// работа с generic-методами AutoMapper делается через reflection в applicator-классах.
/// </remarks>
internal sealed class AutoMapperProfileBuilder
    : MapperProfileBuilderBase<Profile>
{
    private static readonly MethodInfo CreateMapGenericMethod = typeof(Profile)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .First(m =>
            m is { Name: nameof(Profile.CreateMap), IsGenericMethodDefinition: true }
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

    /// <summary>
    /// Строит AutoMapper <see cref="Profile"/> на основе декларативного описания.
    /// </summary>
    /// <param name="profile">Провайдеро-независимое описание профиля.</param>
    /// <returns>AutoMapper-овский профиль с применёнными настройками.</returns>
    public Profile Build(IMappingProfile profile)
    {
        var autoMapperProfile = new AdapterProfile();

        Apply(autoMapperProfile, profile);

        return autoMapperProfile;
    }

    /// <inheritdoc />
    protected override object CreateMappingContext(
        Profile autoMapperProfile,
        Type sourceType,
        Type destinationType)
    {
        var createMapMethod = CreateMapGenericMethod.MakeGenericMethod(sourceType, destinationType);
        return createMapMethod.Invoke(autoMapperProfile, null)
               ?? throw new InvalidOperationException(
                   $"{nameof(Profile.CreateMap)}<{sourceType.Name}, {destinationType.Name}>() returned null.");
    }
}
