// ----------------------------------------------------------------------------------------------
// <copyright file="AutoMapperDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;
using IMapper = Shared.Application.Core.Mapping.Interfaces.IMapper;

namespace Shared.Infrastructure.Mapper.AutoMapper;

/// <summary>
/// Класс, предназначенный для интеграции Automapper в DI через <see cref="IServiceCollection"/>.
/// </summary>
public class AutoMapperDependencyInjector(
    ILogger<AutoMapperDependencyInjector> logger
) : DependencyInjectorBase(logger)
{
    /// <summary>
    /// Инициализирует зависимости для маппера (вызывается неявно).
    /// </summary>
    /// <param name="serviceCollection"><see cref="IServiceCollection"/>.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        // TODO: проверить
        var profileType = typeof(Profile);
        var mapperProfilesTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(type => profileType.IsAssignableFrom(type) && !type.IsAbstract).ToArray();
        return serviceCollection
            .AddAutoMapper(mapperProfilesTypes)
            .AddSingleton<IMapper, Mapper>();
    }
}
