// ----------------------------------------------------------------------------------------------
// <copyright file="IPersonsService.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Getter.Application.Interfaces;

/// <summary>
/// Интерфейс тестового сервиса.
/// </summary>
public interface IPersonsService
{
    /// <summary>
    /// Возвращает список 'Person-ов' с использованием указанного паттерна для доступа к Dal.
    /// </summary>
    /// <param name="request"><inheritdoc cref="PersonListRequest"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Список всех 'Person-ов'.</returns>
    Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken);
}
