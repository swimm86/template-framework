// ----------------------------------------------------------------------------------------------
// <copyright file="IHttpBatchRetryPolicy.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Batch.Http.RetryPolicy.Interfaces;

/// <summary>
/// Политика повторных попыток операции с токеном отмены (например, запроса одной страницы).
/// </summary>
public interface IHttpBatchRetryPolicy
{
    /// <summary>
    /// Выполняет операцию с повторами согласно реализации.
    /// </summary>
    /// <param name="operation">Операция для выполнения.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <typeparam name="TResult">Тип результата операции.</typeparam>
    /// <returns>Результат успешного выполнения.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
