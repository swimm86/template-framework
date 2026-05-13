// ----------------------------------------------------------------------------------------------
// <copyright file="ReadByKeyQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

/// <summary>
/// Базовый класс для чтения по Id
/// </summary>
/// <param name="key">Ключ по которому будет проиходить чтение.</param>
/// <typeparam name="TResponse">Возвращаемый тип из хендлера.</typeparam>
public abstract class ReadByKeyQuery<TResponse>(object key) : IQuery<TResponse>
{
    /// <summary>
    /// Ключ
    /// </summary>
    public object Key { get; } = key;
}
