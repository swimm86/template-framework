// ----------------------------------------------------------------------------------------------
// <copyright file="HelloWorldJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Core.Job.Interfaces;

namespace Template.Bff.Application.Jobs;

/// <summary>
/// Тестовая фоновая задача, используемая как proof of zero-touch migration на Hangfire.
/// <para>
/// Реализует только абстракции <see cref="IScheduledJob"/> и использует DI через конструктор
/// — никаких прямых зависимостей на Quartz или Hangfire.
/// </para>
/// </summary>
/// <param name="logger">Логгер.</param>
public sealed class HelloWorldJob(
    ILogger<HelloWorldJob> logger)
    : IScheduledJob
{
    private readonly ILogger<HelloWorldJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Hello from Hangfire at {Time}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
