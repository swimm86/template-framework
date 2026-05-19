// ----------------------------------------------------------------------------------------------
// <copyright file="HttpBatchRetryPolicyExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Batch.Http.RetryPolicy.Interfaces;

namespace Shared.Application.Core.Batch.Http.RetryPolicy.Extensions;

/// <summary>
/// Методы расширения для <see cref="IHttpBatchRetryPolicy"/>.
/// </summary>
public static class HttpBatchRetryPolicyExtensions
{
    /// <summary>
    /// Выполняет операцию с повторами согласно реализации.
    /// </summary>
    /// <param name="policy">Политика повторных попыток операции.</param>
    /// <param name="operation">Операция для выполнения.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, завершающаяся без значения после успешного выполнения операции (включая повторы при транзиентных сбоях).</returns>
    public static Task ExecuteAsync(
        this IHttpBatchRetryPolicy policy,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken) =>
        policy.ExecuteAsync<object>(
            async ct =>
            {
                await operation(ct);
                return null!;
            },
            cancellationToken);
}
