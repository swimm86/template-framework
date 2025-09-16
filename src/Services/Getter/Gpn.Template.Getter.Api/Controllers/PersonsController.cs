// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsController.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Api.Controllers.Base;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Gpn.Template.Getter.Api.Controllers;

/// <summary>
/// Person контроллер.
/// </summary>
public sealed class PersonsController(
    IUnitOfWork unitOfWork,
    IPersonsService personsService,
    ILogger<PersonsController> logger
    ) : GetterControllerBase(logger)
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="dto">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    [HttpPost("list")]
    public Task<IActionResult> GetPersonsAsync(
        [FromBody] PersonListRequest dto,
        CancellationToken cancellationToken = default) =>
        Process(() => personsService.GetPersonsAsync(dto), cancellationToken);

    /// <summary>
    /// test
    /// </summary>
    /// <param name="dto">.</param>
    /// <returns>.</returns>
    [HttpPost("test_sequence_number")]
    public async Task<IActionResult> TestSequenceNumber(
        [FromBody] TestSequenceNumberRequest dto)
    {
        var guid = Guid.NewGuid();
        var p = Person.Create($"test{guid}", $"test{guid}", dto.GroupNumber);

        var repo = unitOfWork.GetRepository<Person>();
        await repo.AddAsync(p);
        await unitOfWork.SaveChangesAsync();

        return Ok();
    }
}
