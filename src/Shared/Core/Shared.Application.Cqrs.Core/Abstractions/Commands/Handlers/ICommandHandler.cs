// ----------------------------------------------------------------------------------------------
// <copyright file="ICommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;

/// <summary>
/// Интерфейс обработчика команды без возвращаемого значения.
/// </summary>
/// <typeparam name="TCommand">Тип обрабатываемой команды.</typeparam>
public interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand;

/// <summary>
/// Интерфейс обработчика команды с возвращаемым значением.
/// </summary>
/// <typeparam name="TCommand">Тип обрабатываемой команды.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
