// ----------------------------------------------------------------------------------------------
// <copyright file="CapturingLoggerFactory.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shared.Testing.Doubles.Logging;

/// <summary>
/// Fake-реализация <see cref="ILoggerFactory"/>, фиксирующая имена категорий,
/// переданных в <see cref="CreateLogger"/>.
/// Используется для верификации того, что компонент создаёт логгер
/// для конкретного типа (<c>GetType()</c>), а не для обобщённого базового.
/// </summary>
public sealed class CapturingLoggerFactory
    : ILoggerFactory
{
    private readonly ConcurrentQueue<string> _categories = new();

    /// <summary>Коллекция имён категорий, запрошенных у фабрики.</summary>
    public IReadOnlyCollection<string> Categories => _categories;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        _categories.Enqueue(categoryName);
        return new FakeLogger();
    }

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
