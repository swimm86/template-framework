// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireScheduledJobAdapter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Interfaces;
using Shared.Application.Core.Job.Pipeline;
using Shared.Application.Core.Job.Pipeline.Interfaces;
using HangfireJob = Hangfire.Common.Job;

namespace Shared.Infrastructure.Job.Hangfire;

/// <summary>
/// Hangfire-адаптер для запуска <see cref="IScheduledJob"/>. Это аналог <c>QuartzScheduledJobAdapter</c>
/// на стороне Hangfire: обе абстракции транслируют вызов от провайдера (Quartz <c>IJob</c> / Hangfire
/// <c>BackgroundJob</c>) в наш <see cref="IScheduledJob"/> + pipeline (<see cref="IScheduledJobExecutor"/>).
/// </summary>
/// <remarks>
/// <para>
/// Имя класса — <c>Adapter</c> (а не <c>Bridge</c>), как и в Quartz-варианте, потому что роль
/// та же самая: «переходник» между API планировщика и нашим <see cref="IScheduledJob"/>.
/// </para>
/// <para>
/// <b>Почему метод <see cref="RunScheduledJobAsync"/> существует:</b> Hangfire требует, чтобы тело
/// лямбды в <c>BackgroundJob.Schedule</c> / <c>RecurringJob.AddOrUpdate</c> было
/// <c>MethodCallExpression</c> на конкретный метод, а не <c>InvocationExpression</c> на
/// захваченную локальную <c>Func</c>-переменную. Этот класс предоставляет ровно один
/// публичный метод, который Hangfire видит как сериализуемый MethodCall.
/// </para>
/// <para>
/// <b>Жизненный цикл:</b> transient. Каждый invoke Hangfire создаёт новый экземпляр через свой
/// <c>AspNetCoreJobActivator</c> (см. <c>HangfireDependencyInjector</c>). Резолв конкретной
/// <see cref="IScheduledJob"/> — по <see cref="Type.AssemblyQualifiedName"/>, переданному в аргументе.
/// </para>
/// </remarks>
internal sealed class HangfireScheduledJobAdapter(
    IServiceProvider serviceProvider,
    IScheduledJobExecutor executor,
    ILogger<HangfireScheduledJobAdapter> logger)
{
    /// <summary>
    /// Создаёт <see cref="HangfireJob"/>, который Hangfire сможет сериализовать
    /// (MethodCall-выражение на <see cref="RunScheduledJobAsync"/>) для cron/onstartup
    /// расписаний. Используется <c>HangfireJobScheduler</c> при регистрации фоновой задачи.
    /// </summary>
    /// <param name="typeName">AssemblyQualifiedName типа, реализующего <see cref="IScheduledJob"/>.</param>
    /// <param name="serviceKey">Ключ keyed-сервиса или <c>null</c>.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <returns>Сериализуемая <see cref="HangfireJob"/>-обёртка.</returns>
    public static HangfireJob CreateHangfireJob(
        string typeName,
        string? serviceKey,
        RetryOptions? retryOptions)
    {
        Expression<Func<HangfireScheduledJobAdapter, Task>> expression =
            bridge => bridge.RunScheduledJobAsync(typeName, serviceKey, retryOptions, CancellationToken.None);
        return HangfireJob.FromExpression(expression);
    }

    /// <summary>
    /// Запускает фоновую задачу по её <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    /// <param name="jobTypeName">AssemblyQualifiedName типа, реализующего <see cref="IScheduledJob"/>.</param>
    /// <param name="serviceKey">Ключ keyed-сервиса (если задача зарегистрирована через <c>AddKeyedSingleton</c>), иначе <c>null</c>.</param>
    /// <param name="retryOptions"><inheritdoc cref="Shared.Application.Core.Job.Pipeline.RetryOptions" path="/summary"/></param>
    /// <param name="cancellationToken">Токен отмены (пробрасывается через конвейер/pipeline).</param>
    /// <returns>Задача, представляющая асинхронное выполнение фоновой задачи.</returns>
    public Task RunScheduledJobAsync(
        string jobTypeName,
        string? serviceKey,
        RetryOptions? retryOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobTypeName))
        {
            throw new InvalidOperationException($"{nameof(jobTypeName)} must not be empty or whitespace.");
        }

        var jobType = Type.GetType(jobTypeName, throwOnError: false)
            ?? throw new InvalidOperationException($"Failed to resolve type '{jobTypeName}'.");

        var job = serviceKey is null
            ? serviceProvider.GetRequiredService(jobType)
            : serviceProvider.GetRequiredKeyedService(jobType, serviceKey);

        if (job is not IScheduledJob)
        {
            throw new InvalidOperationException(
                $"Type {jobType.FullName} is registered in DI but does not implement {nameof(IScheduledJob)}.");
        }

        var context = new ScheduledJobContext(
            jobKey: jobType.FullName ?? jobTypeName,
            serviceProvider: serviceProvider,
            cancellationToken: cancellationToken)
        {
            JobType = jobType,
            ServiceKey = serviceKey,
            RetryOptions = retryOptions,
        };

        logger.LogDebug(
            "Starting {JobType} (serviceKey: {ServiceKey}).",
            jobType.FullName,
            serviceKey ?? "<none>");

        return executor.ExecuteAsync(context);
    }
}
