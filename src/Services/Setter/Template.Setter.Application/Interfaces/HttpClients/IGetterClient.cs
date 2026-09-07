// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Template.Setter.Application.Interfaces.HttpClients;

/// <summary>
/// Интерфейс API-клиента Getter-а.
/// </summary>
public interface IGetterClient
{
    /// <summary>
    /// Выполняет тестовый вызов Getter для проверки передачи ошибки через HTTP-границу сервисов.
    /// </summary>
    /// <remarks>
    /// Getter намеренно завершает вызов исключением. HTTP-клиент преобразует ошибочный ответ
    /// удалённого сервиса в <see cref="Shared.Application.Core.Exceptions.Models.ProxiedException"/>
    /// с сохранением исходных сведений об исключении и передаёт его вызывающему коду.
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>
    /// Ответ Getter, если удалённый сервис неожиданно завершает вызов успешно.
    /// В штатном диагностическом сценарии задача завершается исключением и ответ не возвращается.
    /// </returns>
    /// <exception cref="Shared.Application.Core.Exceptions.Models.ProxiedException">
    /// Выбрасывается при получении от Getter ошибочного HTTP-ответа с данными исходного исключения.
    /// </exception>
    Task<Response> TestExceptionChainAsync(CancellationToken cancellationToken = default);
}
