// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionFormattingPool.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Shared.Presentation.Core.Exceptions;

/// <summary>
/// Статический класс, который содержит пул <see cref="StringBuilder"/>.
/// </summary>
internal static class ExceptionFormattingPool
{
    /// <summary>
    /// Пул <see cref="StringBuilder"/>.
    /// </summary>
    internal static readonly DefaultObjectPool<StringBuilder> StringBuilder = new(
        new StringBuilderPolicy(),
        Math.Min(Environment.ProcessorCount * 2, 16));
}