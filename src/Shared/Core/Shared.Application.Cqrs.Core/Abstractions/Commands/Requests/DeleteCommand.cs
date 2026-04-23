// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда на удаление
/// </summary>
/// <param name="Key">Ключ.</param>
public abstract record DeleteCommand(object Key) : ICommand<Response>;
