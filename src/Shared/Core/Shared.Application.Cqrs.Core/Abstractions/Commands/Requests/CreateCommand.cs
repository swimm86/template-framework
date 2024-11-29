// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommand.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Создание.
/// </summary>
/// <param name="Request">ДТО на создание.</param>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Ответ из хендлера.</typeparam>
public abstract record CreateCommand<TRequest, TResponse>(TRequest Request) : ICommand<TResponse>;
