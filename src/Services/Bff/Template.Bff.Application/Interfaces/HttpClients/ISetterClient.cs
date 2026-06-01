// ----------------------------------------------------------------------------------------------
// <copyright file="ISetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Bff.Application.Interfaces.HttpClients;

/// <summary>
/// Интерфейс API-клиента Setter-а.
/// </summary>
public interface ISetterClient
{
    /// <summary>
    /// Создает сущность "Персона".
    /// </summary>
    /// <param name="request">Данные для создания сущности.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Информация о созданной сущности "Персона".</returns>
    Task<PersonCreateResponse> CreatePersonAsync(
        PersonCreateRequest request,
        CancellationToken cancellationToken = default);
}
