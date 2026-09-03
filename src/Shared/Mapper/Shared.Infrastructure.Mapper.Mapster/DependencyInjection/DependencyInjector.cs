// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.DependencyInjection;
using Shared.Infrastructure.Mapper.Mapster.Adapters;

namespace Shared.Infrastructure.Mapper.Mapster.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Mapper.Mapster</c>.
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase{TypeAdapterConfig, Mapper}" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase{TypeAdapterConfig, Mapper}(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
internal class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase<TypeAdapterConfig, Mapper>(loggerFactory)
{
    /// <inheritdoc />
    protected override TypeAdapterConfig BuildConfig(IReadOnlyCollection<IMappingProfile> profiles)
    {
        var config = new TypeAdapterConfig();
        var builder = new MapsterProfileBuilder();
        foreach (var profile in profiles)
        {
            builder.Apply(config, profile);
        }

        return config;
    }

    /// <inheritdoc />
    protected override IServiceCollection RegisterConfig(
        IServiceCollection serviceCollection,
        TypeAdapterConfig config)
    {
        return serviceCollection
            .AddSingleton(config)
            .AddSingleton<MapsterMapper.IMapper, ServiceMapper>();
    }
}
