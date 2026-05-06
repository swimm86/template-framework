// ----------------------------------------------------------------------------------------------
// <copyright file="EfCoreDependencyInjectorBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Extensions;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.Dal.EFCore.DependencyInjection.Base;

/// <summary>
/// Базовый регистратор зависимостей подсистемы доступа к данным на EF Core (контексты и оценка запросов).
/// </summary>
/// <inheritdoc cref="DependencyInjectorBase" path="/remarks"/>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public abstract class EfCoreDependencyInjectorBase(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IQueryEvaluator, EfQueryEvaluator>()
            .AddDbContexts();
    }
}
