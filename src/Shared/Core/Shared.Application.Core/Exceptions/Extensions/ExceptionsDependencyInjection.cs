// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionsDependencyInjection.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application.Core.Exceptions.Extensions;

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
    public static IServiceCollection AddExceptionsHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<ExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
