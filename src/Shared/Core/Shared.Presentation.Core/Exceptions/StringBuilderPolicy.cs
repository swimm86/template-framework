// ----------------------------------------------------------------------------------------------
// <copyright file="StringBuilderPolicy.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Shared.Presentation.Core.Exceptions;

/// <summary>
/// Политика создания объектов StringBuilder для пула.
/// </summary>
internal sealed class StringBuilderPolicy
    : IPooledObjectPolicy<StringBuilder>
{
    /// <summary>
    /// Создаёт новый экземпляр StringBuilder.
    /// </summary>
    /// <returns>Новый экземпляр StringBuilder.</returns>
    public StringBuilder Create() => new(capacity: 1024);

    /// <summary>
    /// Возвращает StringBuilder в пул после очистки.
    /// </summary>
    /// <param name="obj">StringBuilder для возврата в пул.</param>
    /// <returns>Всегда true.</returns>
    public bool Return(StringBuilder obj)
    {
        obj.Clear();
        return true;
    }
}