// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Template.Setter.Api.Controllers.Base;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;

namespace Template.Setter.Api.Controllers;

/// <summary>
/// Контроллер для взаимодействия с сущностями 'Персона'.
/// </summary>
public sealed class PersonsController(
    ISender sender,
    ILogger<PersonsController> logger)
    : SetterControllerBase(logger)
{
    /// <summary>
    /// Создает сущность 'Персона'.
    /// </summary>
    /// <param name="request">Запрос с данными для создания сущности 'Персона'.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Информация о созданной сущности 'Персона'.</returns>
    [HttpPost("create")]
    public Task<IActionResult> PersonsCreateAsync(
        [FromBody] PersonCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Process(
            () => sender.Send(new PersonCreateCommand(request), cancellationToken));
    }
}
