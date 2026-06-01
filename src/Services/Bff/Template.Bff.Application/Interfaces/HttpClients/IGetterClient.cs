// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Bff.Application.HttpClients.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.Interfaces.HttpClients;

/// <summary>
/// Интерфейс API-клиента Getter-а.
/// </summary>
public interface IGetterClient
{
    /// <summary>
    /// Возвращает список сущностей "Персона".
    /// </summary>
    /// <param name="request">Параметры списка (пагинация, фильтры и сортировка).</param>
    /// <param name="pattern">Паттерн: Services или CQRS.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей "Персона".</returns>
    Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        GetPersonsPattern pattern,
        CancellationToken cancellationToken = default);
}
