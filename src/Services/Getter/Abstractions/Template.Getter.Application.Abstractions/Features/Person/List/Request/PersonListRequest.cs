// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;
using Template.Getter.Application.Abstractions.Enums;

namespace Template.Getter.Application.Abstractions.Features.Person.List.Request;

/// <summary>
/// DTO-запрос для получения списка сущностей "Персона".
/// </summary>
/// <param name="DalPattern">Dal-паттерн для получения сущностей "Персона" из слоя доступа к данным.</param>
public record PersonListRequest(DalPattern DalPattern)
    : PageableRequest<PersonListFilter>;
