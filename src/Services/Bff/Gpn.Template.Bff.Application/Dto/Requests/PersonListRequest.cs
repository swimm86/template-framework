// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequest.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Abstractions.Enums;

namespace Gpn.Template.Bff.Application.Dto.Requests;

/// <summary>
/// Request-Dto для получения всех 'Person-ов'.
/// </summary>
/// <param name="DalPattern">Dal-паттерн, который необходимо использовать для получения 'Person-ов' из Dal-слоя.</param>
/// <param name="UseCqrs">Признак того, что нужно использовать CQRS-реализацию.</param>
public record PersonListRequest(DalPattern DalPattern, bool UseCqrs)
    : Getter.Application.Abstractions.Dto.Person.Requests.PersonListRequest(DalPattern);
