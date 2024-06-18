// ----------------------------------------------------------------------------------------------
// <copyright file="IPersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Responses;
using Shared.Application.Core.Dto.Responses;

namespace Gpn.Template.Getter.Application.Interfaces;

/// <summary>
/// Интерфейс тестового сервиса.
/// </summary>
public interface IPersonsService
{
    /// <summary>
    /// Возвращает список 'Person-ов' с использованием указанного паттерна для доступа к Dal.
    /// </summary>
    /// <param name="dto"><inheritdoc cref="GetPersonsRequestDto"/></param>
    /// <returns>Список всех 'Person-ов'.</returns>
    Task<PageableResponse<ICollection<PersonDto>>> GetPersonsAsync(GetPersonsRequestDto dto);
}
