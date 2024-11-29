// ----------------------------------------------------------------------------------------------
// <copyright file="CloneCommand.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда клонирования.
/// </summary>
/// <param name="Key">Ключ по которому будет производиться поиск.</param>
/// <param name="Request">DTO с дополнительной информацией для клонирование.</param>
/// <typeparam name="TRequest">Тип DTO.</typeparam>
/// <typeparam name="TResponse">Ответ хендлера.</typeparam>
public abstract record CloneCommand<TRequest, TResponse>(object Key, TRequest Request)
    : ICommand<TResponse>;