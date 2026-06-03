// ----------------------------------------------------------------------------------------------
// <copyright file="ScheduledJobContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Job.Pipeline;

/// <summary>
/// Контекст выполнения одной итерации фоновой задачи.
/// Передаётся через все middleware и попадает в terminal-action.
/// </summary>
/// <param name="jobKey">Уникальный ключ задачи.</param>
/// <param name="serviceProvider">Провайдер сервисов для получения зависимостей.</param>
/// <param name="cancellationToken">Токен отмены выполнения.</param>
public sealed class ScheduledJobContext(
    string jobKey,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
{
    /// <summary>
    /// Уникальный ключ задачи.
    /// </summary>
    public string JobKey { get; } = jobKey ?? throw new ArgumentNullException(nameof(jobKey));

    /// <summary>
    /// Тип фоновой задачи (<see cref="Job.Interfaces.IScheduledJob"/>)
    /// или <c>null</c>, если задача задана делегатом.
    /// </summary>
    public Type? JobType { get; init; }

    /// <summary>
    /// Ключ для keyed-сервиса в DI (если <see cref="JobType"/> зарегистрирован как keyed).
    /// </summary>
    public string? ServiceKey { get; init; }

    /// <summary>
    /// Действие задачи, заданное делегатом. Получает <see cref="IServiceProvider"/> и
    /// <see cref="CancellationToken"/>, что позволяет получать зависимости из DI
    /// в момент выполнения. Для задач с указанным типом исполнитель получает задачу по
    /// <see cref="JobType"/>, и это поле остаётся <c>null</c>.
    /// </summary>
    public Func<IServiceProvider, CancellationToken, Task>? Action { get; init; }

    /// <summary>
    /// Провайдер сервисов для получения зависимостей.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <summary>
    /// Токен отмены выполнения.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// Свойства, пробрасываемые между middleware.
    /// </summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
}
