// ----------------------------------------------------------------------------------------------
// <copyright file="IQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;

/// <summary>
/// Интерфейс обработчика запроса на чтение.
/// </summary>
/// <typeparam name="TQuery">Тип обрабатываемого запроса.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
