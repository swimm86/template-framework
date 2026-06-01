// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Getter.Application.Abstractions.Enums;

namespace Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests;

/// <summary>
/// DTO-запрос для получения списка сущностей "Персона".
/// </summary>
/// <param name="DalPattern">Dal-паттерн для получения сущностей "Персона" из слоя доступа к данным.</param>
/// <param name="UseCqrs">Признак того, что нужно использовать CQRS-реализацию.</param>
public record PersonListRequest(DalPattern DalPattern, bool UseCqrs)
    : Getter.Application.Abstractions.Features.Person.List.Request.PersonListRequest(DalPattern);
