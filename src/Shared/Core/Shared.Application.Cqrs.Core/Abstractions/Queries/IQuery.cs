// ----------------------------------------------------------------------------------------------
// <copyright file="IQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries;

/// <summary>
/// Интерфейс запроса на чтение данных.
/// </summary>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
public interface IQuery<out TResponse>
    : IRequest<TResponse>;
