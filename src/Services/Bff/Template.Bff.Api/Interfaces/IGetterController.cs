// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Template.Bff.Application.Features.Person.Cqrs.Queries.List.Requests;

namespace Template.Bff.Api.Interfaces;

/// <summary>
/// Интерфейс Getter контроллера.
/// </summary>
public interface IGetterController
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Список всех 'Person'-ов.</returns>
    Task<IActionResult> GetPersonsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default);
}
