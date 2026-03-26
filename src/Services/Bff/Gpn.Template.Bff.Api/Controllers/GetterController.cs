// ----------------------------------------------------------------------------------------------
// <copyright file="GetterController.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Bff.Api.Controllers.Base;
using Gpn.Template.Bff.Api.Interfaces;
using Gpn.Template.Bff.Application.Dto.Requests;
using Gpn.Template.Bff.Application.Features.Person.Cqrs.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gpn.Template.Bff.Api.Controllers;

/// <summary>
/// Контроллер Getter
/// </summary>
/// <param name="sender"><see cref="ISender"/>.</param>
/// <param name="logger">Логгер.</param>
public class GetterController(
    ISender sender,
    ILogger<GetterController> logger)
    : BffControllerBase(logger), IGetterController
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("person/list")]
    public Task<IActionResult> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonListQuery(request), cancellationToken),
            cancellationToken);
    }
}
