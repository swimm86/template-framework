// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterClient.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Gpn.Template.Bff.Application.Interfaces.HttpClients;

/// <summary>
/// Интерфейс клиента Getter
/// </summary>
public interface IGetterClient
{
    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    Task<PersonListResponse> GetPersonsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список всех 'Person'-ов.
    /// </summary>
    /// <param name="request">DTO.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person'-ов</returns>
    Task<PersonListResponse> GetPersonsCqrsAsync(
        [FromBody] PersonListRequest request,
        CancellationToken cancellationToken = default);
}
