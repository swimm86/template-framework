// ----------------------------------------------------------------------------------------------
// <copyright file="IPersonsService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;

namespace Gpn.Template.Getter.Application.Interfaces;

/// <summary>
/// Интерфейс тестового сервиса.
/// </summary>
public interface IPersonsService
{
    /// <summary>
    /// Возвращает список 'Person-ов' с использованием указанного паттерна для доступа к Dal.
    /// </summary>
    /// <param name="request"><inheritdoc cref="PersonListRequest"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Список всех 'Person-ов'.</returns>
    Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request,
        CancellationToken cancellationToken);
}
