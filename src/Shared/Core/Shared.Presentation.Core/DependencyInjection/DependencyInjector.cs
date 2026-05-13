// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Presentation.Core.Exceptions.Extensions;
using Shared.Presentation.Core.Extensions;
using Shared.Presentation.Core.Swagger.Extensions;

namespace Shared.Presentation.Core.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя: <c>Shared.Presentation.Core</c>.
/// </summary>
/// <remarks>
/// Данный инжектор регистрирует основные сервисы слоя представления: Swagger, FluentValidation,
/// обработку исключений и средства исследования эндпоинтов.
/// <para><inheritdoc cref="DependencyInjectorBase" path="/remarks"/></para>
/// </remarks>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddEndpointsApiExplorer()
            .AddSwagger()
            .AddFluentValidation()
            .AddExceptionHandling();
    }
}
