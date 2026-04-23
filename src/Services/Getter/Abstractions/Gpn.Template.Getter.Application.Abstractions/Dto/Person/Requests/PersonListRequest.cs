// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Filters;
using Gpn.Template.Getter.Application.Abstractions.Enums;
using Shared.Application.Core.Dto.Requests;

namespace Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;

/// <summary>
/// Request-Dto для получения всех 'Person-ов'.
/// </summary>
/// <param name="DalPattern">Dal-паттерн, который необходимо использовать для получения 'Person-ов' из Dal-слоя.</param>
public record PersonListRequest(DalPattern DalPattern)
    : PageableRequest<PersonListFilter>;
