// ----------------------------------------------------------------------------------------------
// <copyright file="GetPersonsRequestDto.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Features.PersonFeature.Cqrs.Queries.Filters;
using Shared.Application.Core.Dto.Requests;

namespace Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;

/// <summary>
/// Request-Dto для получения всех 'Person-ов'.
/// </summary>
/// <param name="DalPattern">Dal-паттерн, который необходимо использовать для получения 'Person-ов' из Dal-слоя.</param>
public sealed record GetPersonsRequestDto(DalPattern DalPattern) : PageableRequest<PersonFilter>
{
}
