// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Getter.Application.Abstractions.Enums;

namespace Template.Bff.Application.Features.Person.Cqrs.Queries.List.Requests;

/// <summary>
/// Request-Dto для получения всех 'Person-ов'.
/// </summary>
/// <param name="DalPattern">Dal-паттерн, который необходимо использовать для получения 'Person-ов' из Dal-слоя.</param>
/// <param name="UseCqrs">Признак того, что нужно использовать CQRS-реализацию.</param>
public record PersonListRequest(DalPattern DalPattern, bool UseCqrs)
    : Getter.Application.Abstractions.Dto.Person.Requests.PersonListRequest(DalPattern);
