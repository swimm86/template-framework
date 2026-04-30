// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.HttpClients.Enums;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;

namespace Gpn.Template.Bff.Application.Interfaces.HttpClients;

/// <summary>
/// Интерфейс API-клиента Getter-а.
/// </summary>
public interface IGetterClient
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">Параметры списка (пагинация, фильтры и сортировка).</param>
    /// <param name="pattern">Режим маршрута Getter: сервисный слой или CQRS.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Коллекция сущностей 'Person'.</returns>
    Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        GetPersonsPattern pattern,
        CancellationToken cancellationToken = default);
}
