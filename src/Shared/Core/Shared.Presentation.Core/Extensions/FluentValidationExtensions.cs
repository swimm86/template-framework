// ----------------------------------------------------------------------------------------------
// <copyright file="FluentValidationExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using FluentValidation.AspNetCore;
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

        // Добавляем автоматическую валидацию и загружаем валидаторы из сборок
        return services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true);
    }
}
