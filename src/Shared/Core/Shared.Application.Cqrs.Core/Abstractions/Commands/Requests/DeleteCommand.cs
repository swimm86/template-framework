// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Базовая команда удаления сущности.
/// </summary>
/// <param name="Key">Ключ удаляемой сущности.</param>
public abstract record DeleteCommand(object Key)
    : ICommand<Response>;
