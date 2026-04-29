// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Application.Core.DependencyInjection;

namespace Shared.Infrastructure.Job.Quartz;

/// <summary>
/// Класс для внедрения зависимостей Quartz.
/// </summary>
/// <param name="logger">Экземпляр <see cref="ILogger{QuartzDependencyInjector}"/> для работы с логированием.</param>
public class QuartzDependencyInjector(
    ILogger<QuartzDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}