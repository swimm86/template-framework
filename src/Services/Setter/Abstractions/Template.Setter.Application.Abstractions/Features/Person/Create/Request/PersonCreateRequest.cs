// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Application.Dto.Person;

namespace Template.Setter.Application.Abstractions.Features.Person.Create.Request;

/// <summary>
/// Запрос на создание сущности "Персона".
/// </summary>
public sealed record PersonCreateRequest
    : PersonCreateDto
{
    // Reserved for future contract-specific fields
}
