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
        // Act
        var result = LogMessages.Started;

        // Assert
        result.Should().Be("{process} started.");
    }

    /// <summary>
    /// Шаблон для completed содержит ожидаемый текст.
    /// </summary>
    [Fact]
    public void Completed_ContainsExpectedTemplate()
    {
        // Act
        var result = LogMessages.Completed;

        // Assert
        result.Should().Be("{process} completed.");
    }

    /// <summary>
    /// Шаблон для failed содержит ожидаемый текст.
    /// </summary>
    [Fact]
    public void Failed_ContainsExpectedTemplate()
    {
        // Act
        var result = LogMessages.Failed;

        // Assert
        result.Should().Be("{process} failed.");
    }

    /// <summary>
    /// Шаблон Elapsed точно соответствует документированному формату.
    /// </summary>
    [Fact]
    public void Elapsed_EqualsDocumentedTemplate()
    {
        LogMessages.Elapsed.Should().Be("{process} processed time: {time}ms.");
    }
}
