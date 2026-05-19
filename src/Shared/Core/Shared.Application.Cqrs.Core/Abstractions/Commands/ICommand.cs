// ----------------------------------------------------------------------------------------------
// <copyright file="ICommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands;

/// <summary>
/// Маркер-интерфейс команды без возвращаемого значения.
/// </summary>
public interface ICommand
    : IRequest<Unit>;

/// <summary>
/// Интерфейс команды с возвращаемым значением.
/// </summary>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
public interface ICommand<out TResponse>
    : IRequest<TResponse>;

/// <summary>
/// Интерфейс команды с отдельным объектом запроса и возвращаемым значением.
/// </summary>
/// <typeparam name="TRequest">Тип объекта запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
public interface ICommand<out TRequest, out TResponse>
    : ICommand<TResponse>
{
    /// <summary>Объект запроса с данными для выполнения команды.</summary>
    TRequest Request { get; }
}
