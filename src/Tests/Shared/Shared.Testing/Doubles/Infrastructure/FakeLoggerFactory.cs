// ----------------------------------------------------------------------------------------------
// <copyright file="FakeLoggerFactory.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Testing.Doubles.Logging;

namespace Shared.Testing.Doubles.Infrastructure;

/// <summary>
/// Fake-реализация <see cref="ILoggerFactory"/> для тестов.
/// Поддерживает два сценария:
/// <list type="bullet">
/// <item>Без параметров — создаёт новый <see cref="FakeLogger"/> при каждом вызове <c>CreateLogger</c></item>
/// <item>С переданным <see cref="FakeLogger"/> — всегда возвращает один и тот же экземпляр (для верификации логов)</item>
/// </list>
/// </summary>
public sealed class FakeLoggerFactory
    : ILoggerFactory
{
    private readonly FakeLogger? _logger;

    public FakeLoggerFactory()
    {
    }

    public FakeLoggerFactory(FakeLogger logger)
    {
        _logger = logger;
    }

    public ILogger CreateLogger(string categoryName) => _logger ?? new FakeLogger();

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
