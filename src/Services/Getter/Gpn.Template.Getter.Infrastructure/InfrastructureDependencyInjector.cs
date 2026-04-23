// ----------------------------------------------------------------------------------------------
// <copyright file="InfrastructureDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Getter.Infrastructure;

/// <summary>
/// Класс для внедрения зависимостей Infrastructure-слоя в Getter
/// В этот проект подключаются все инфраструктурные зависимости (mapper, dal и т.д.).
/// Зависимости подключаются автоматически при запуске приложения. Самостоятельно их подключать не нужно.
/// </summary>
/// <param name="logger">Логгер.</param>
public class InfrastructureDependencyInjector(
    ILogger<InfrastructureDependencyInjector> logger
    ) : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection;
    }
}
