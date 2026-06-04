// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzJobExecutionIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Интеграционный тест адаптера Quartz: после старта
/// <see cref="QuartzJobSchedulerBootstrapper"/> джобы, зарегистрированные через
/// <c>AddJobs(...)</c>, действительно выполняются.
/// <para>
/// Общая логика вынесена в <see cref="JobExecutionIntegrationTestBase"/> и
/// полностью симметрична <c>HangfireJobExecutionIntegrationTests</c>:
/// оба адаптера прогоняются через идентичный <c>Bootstrapper_StartAsync_RunsOnStartupJob</c>,
/// что закрывает регрессию вида «адаптер не подключён к DI» для Quartz и Hangfire
/// одновременно.
/// </para>
/// <para>
/// Тест выполняется в коллекции <see cref="QuartzIntegrationCollection"/> —
/// см. обоснование изоляции в её XML-doc.
/// </para>
/// </summary>
[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzJobExecutionIntegrationTests : JobExecutionIntegrationTestBase
{
    /// <inheritdoc />
    protected override void RegisterAdapter(IServiceCollection services, ILoggerFactory loggerFactory) =>
        new QuartzDependencyInjector(loggerFactory).Inject(services);
}
