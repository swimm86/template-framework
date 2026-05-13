// ----------------------------------------------------------------------------------------------
// <copyright file="FluentValidationExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Helpers;

namespace Shared.Presentation.Core.Extensions;

/// <summary>
/// Содержит методы расширения для интеграции FluentValidation с коллекцией сервисов Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Добавляет FluentValidation в коллекцию сервисов и автоматически находит валидаторы в сборках приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов <see cref="IServiceCollection"/>, в которую будет добавлен FluentValidation.</param>
    /// <returns>Исходную коллекцию сервисов с добавленной поддержкой FluentValidation.</returns>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        // Получаем все сборки, содержащие типы, наследованные от AbstractValidator<T>
        var applicationAssemblies =
            AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(typeof(AbstractValidator<>));
        return services
            // Включаем автоматическую валидацию, но настраиваем её для выброса исключений вместо добавления ошибок в ModelState
            .AddFluentValidationAutoValidation()
            // Добавляем валидаторы из всех сборок
            .AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true)
            // Настраиваем обработку ошибок API
            .Configure<ApiBehaviorOptions>(options =>
            {
                // Настраиваем обработчик ошибок валидации через наш ExceptionHandler
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors
                            .Select(error => new FluentValidation.Results.ValidationFailure(e.Key, error.ErrorMessage)))
                        .ToList();
                    throw new ValidationException(errors);
                };
            });
    }
}
