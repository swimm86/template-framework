// ----------------------------------------------------------------------------------------------
// <copyright file="IResponseValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Exceptions.Models;

namespace Shared.Application.Core.ApiClient.Validators.Interfaces;

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
    /// <param name="logUri">
    /// URI запроса, передаваемый реализации для целей диагностики. Применяется реализацией
    /// в качестве резервного значения, если <see cref="HttpRequestMessage.RequestUri"/>
    /// недоступен. Если параметр не задан и <see cref="HttpRequestMessage.RequestUri"/>
    /// также недоступен, реализация определяет поведение самостоятельно.
    /// </param>
    /// <param name="logContent">
    /// Содержимое исходящего запроса, передаваемое реализации для целей диагностики.
    /// Решение об использовании значения принимается реализацией; параметр может быть
    /// проигнорирован.
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="ProxiedException">
    /// Выбрасывается при получении любого неуспешного HTTP-ответа от upstream-сервиса,
    /// в том числе при сбое построения деталей ошибки (деградированный
    /// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> с корневой причиной в
    /// <see cref="Exception.InnerException"/>).
    /// </exception>
    Task ValidateAsync(
        HttpResponseMessage httpResponse,
        string clientName,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default);
}
