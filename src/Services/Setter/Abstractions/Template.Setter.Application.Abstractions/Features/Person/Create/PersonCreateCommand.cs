// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Setter.Application.Abstractions.Features.Person.Create;

/// <summary>
/// Команда создания сущности "Персона".
/// </summary>
/// <param name="Request">Данные запроса.</param>
public sealed record PersonCreateCommand(
    PersonCreateRequest Request)
    : CreateCommand<PersonCreateRequest, PersonCreateResponse>(Request);
