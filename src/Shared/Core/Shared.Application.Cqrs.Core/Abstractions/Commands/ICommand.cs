// ----------------------------------------------------------------------------------------------
// <copyright file="ICommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands;

/// <summary>
/// Интерфейс команды без ответа
/// </summary>
public interface ICommand : IRequest<Unit>
{
}

/// <summary>
/// Интерфейс команды с ответом
/// </summary>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Интерфейс команды с ответом
/// </summary>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public interface ICommand<TRequest, TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// Запрос.
    /// </summary>
    TRequest Request { get; }
}
