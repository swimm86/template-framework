// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Application.Dto.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Gpn.Template.Bff.Api.Interfaces;

/// <summary>
/// Интерфейс Getter контроллера
/// </summary>
public interface IGetterController
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    Task<IActionResult> GetPersonsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default);
}
