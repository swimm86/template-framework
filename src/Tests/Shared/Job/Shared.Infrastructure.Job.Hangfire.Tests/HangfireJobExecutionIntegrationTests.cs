// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireJobExecutionIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Testing.Job;

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Интеграционный тест адаптера Hangfire: после старта
/// <see cref="HangfireJobSchedulerBootstrapper"/> джобы, зарегистрированные через
/// <c>AddJobs(...)</c>, действительно выполняются.
/// <para>
/// Общая логика вынесена в <see cref="JobExecutionIntegrationTestBase"/> и
/// полностью симметрична <c>QuartzJobExecutionIntegrationTests</c>:
/// оба адаптера прогоняются через идентичный <c>Bootstrapper_StartAsync_RunsOnStartupJob</c>,
/// что закрывает регрессию вида «адаптер не подключён к DI» для Quartz и Hangfire
/// одновременно.
/// </para>
/// <para>
/// Тест выполняется в коллекции <see cref="HangfireIntegrationCollection"/>:
/// Hangfire использует глобальный <c>JobStorage.Current</c> и кэширует
/// <see cref="ILoggerFactory"/> в <c>AspNetCoreLogProvider</c>, поэтому
/// параллельный запуск или повторная инициализация после dispose приводят к
/// <c>ObjectDisposedException</c>.
/// </para>
/// </summary>
[Collection(HangfireIntegrationCollection.Name)]
public sealed class HangfireJobExecutionIntegrationTests : JobExecutionIntegrationTestBase
{
    /// <inheritdoc />
    protected override void RegisterAdapter(IServiceCollection services, ILoggerFactory loggerFactory) =>
        new HangfireDependencyInjector(loggerFactory).Inject(services);
}
