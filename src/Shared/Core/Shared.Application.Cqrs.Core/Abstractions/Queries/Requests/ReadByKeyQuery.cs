// ----------------------------------------------------------------------------------------------
// <copyright file="ReadByKeyQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

/// <summary>
/// Базовый запрос на чтение сущности по ключу.
/// </summary>
/// <typeparam name="TResponse">Тип возвращаемого значения.</typeparam>
/// <param name="key">Ключ для поиска сущности.</param>
public abstract class ReadByKeyQuery<TResponse>(
    object key)
    : IQuery<TResponse>
{
    /// <summary>Ключ для поиска сущности.</summary>
    public object Key { get; } = key;
}
