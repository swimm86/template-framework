// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters;
using Shared.Infrastructure.Mapper.Core.DependencyInjection;

namespace Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Infrastructure.Mapper.AutoMapper</c>.
/// </summary>
/// <inheritdoc path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc path="/param[@name='loggerFactory']"/></param>
internal sealed class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase<Profile[], Mapper>(loggerFactory)
{
    /// <inheritdoc />
    protected override Profile[] BuildConfig(IReadOnlyCollection<IMappingProfile> profiles)
    {
        var builder = new AutoMapperProfileBuilder();
        return profiles.Select(builder.Build).ToArray();
    }

    /// <inheritdoc />
    protected override IServiceCollection RegisterConfig(
        IServiceCollection serviceCollection,
        Profile[] config)
    {
        return serviceCollection.AddAutoMapper(cfg => cfg.AddProfiles(config));
    }
}
