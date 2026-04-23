// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionsDependencyInjection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Presentation.Core.Exceptions.Interfaces;

namespace Shared.Presentation.Core.Exceptions.Extensions;

/// <summary>
///  Содержит методы расширения <see cref="IServiceCollection"/>.
/// </summary>
public static class ExceptionsDependencyInjection
{
    /// <summary>
    /// Добавление обработчиков ошибок.
    /// </summary>
    /// <param name="services"> Коллекция сервисов <see cref="IServiceCollection"/>. </param>
    /// <returns> Коллекция сервисов <see cref="IServiceCollection"/>. </returns>
    public static IServiceCollection AddExceptionHandling(
        this IServiceCollection services)
    {
        return services
            .AddProblemDetails()
            .AddExceptionMappers()
            .AddSingleton<IExceptionMapperResolver, ExceptionMapperResolver>()
            .AddExceptionHandler<ExceptionHandler>();
    }

    private static IServiceCollection AddExceptionMappers(
        this IServiceCollection services)
    {
        var interfaceType = typeof(IExceptionMapper);
        var typesToRegister = AssemblyHelper.GetDerivedTypesFromAssemblies<IExceptionMapper>(
            excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ToArray();
        typesToRegister.ForEach(type => services.AddSingleton(interfaceType, type));
        return services;
    }
}
