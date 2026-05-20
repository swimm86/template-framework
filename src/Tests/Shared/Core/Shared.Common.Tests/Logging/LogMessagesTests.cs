// ----------------------------------------------------------------------------------------------
// <copyright file="LogMessagesTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Logging;

namespace Shared.Common.Tests.Logging;

/// <summary>
/// Тесты для шаблонов сообщений <see cref="LogMessages"/>.
/// </summary>
public sealed class LogMessagesTests
{
    /// <summary>
    /// Шаблон для started содержит ожидаемый текст.
    /// </summary>
    [Fact]
    public void Started_ContainsExpectedTemplate()
    {
        // Act & Assert
        LogMessages.Started.Should().Be("{process} started.");
    }

    /// <summary>
    /// Шаблон для completed содержит ожидаемый текст.
    /// </summary>
    [Fact]
    public void Completed_ContainsExpectedTemplate()
    {
        // Act & Assert
        LogMessages.Completed.Should().Be("{process} completed.");
    }

    /// <summary>
    /// Шаблон для failed содержит ожидаемый текст.
    /// </summary>
    [Fact]
    public void Failed_ContainsExpectedTemplate()
    {
        // Act & Assert
        LogMessages.Failed.Should().Be("{process} failed.");
    }

    /// <summary>
    /// Шаблон Elapsed содержит плейсхолдер времени и "ms".
    /// </summary>
    [Fact]
    public void Elapsed_ContainsTimePlaceholder()
    {
        // Act & Assert
        LogMessages.Elapsed.Should().Contain("{time}");
        LogMessages.Elapsed.Should().Contain("ms");
    }
}
