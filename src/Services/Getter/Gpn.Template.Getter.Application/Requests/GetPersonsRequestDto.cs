// ----------------------------------------------------------------------------------------------
// <copyright file="GetPersonsRequestDto.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Gpn.Template.Getter.Application.Requests;

/// <summary>
/// Request-Dto для получения всех 'Person-ов'.
/// </summary>
/// <param name="DalPattern">Dal-паттерн, который необходимо использовать для получения 'Person-ов' из Dal-слоя.</param>
public sealed record GetPersonsRequestDto(DalPattern DalPattern)
{
}
