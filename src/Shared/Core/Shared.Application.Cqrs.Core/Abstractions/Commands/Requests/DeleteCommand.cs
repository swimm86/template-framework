// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommand.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда на удаление
/// </summary>
/// <param name="Key">Ключ.</param>
public abstract record DeleteCommand(object Key) : ICommand<Response>;
