// ----------------------------------------------------------------------------------------------
// <copyright file="IResponseValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Exceptions.Models;

namespace Shared.Application.Core.ApiClient.Interfaces;

/// <summary>
/// Валидатор HTTP-ответов.
/// </summary>
/// <remarks>
/// Проверяет успешность ответа и преобразует ошибки
/// в доменные исключения (например, <c>ProxiedException</c>).
/// </remarks>
public interface IResponseValidator
{
    /// <summary>
    /// Валидирует HTTP-ответ и выбрасывает исключение в случае ошибки.
    /// </summary>
    /// <param name="httpResponse">HTTP-ответ для проверки.</param>
    /// <param name="clientName">Имя клиента для логирования.</param>
    /// <param name="logUri">URI запроса для логирования (опционально).</param>
    /// <param name="logContent">Содержимое запроса для логирования (опционально).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="ProxiedException">
    /// Выбрасывается при получении ошибки от upstream-сервиса.
    /// </exception>
    /// <exception cref="Exception">
    /// Выбрасывается при непредвиденной ошибке HTTP.
    /// </exception>
    Task ValidateAsync(
        HttpResponseMessage httpResponse,
        string clientName,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default);
}
