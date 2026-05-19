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
    [Fact]
    public void Started_ContainsExpectedTemplate()
    {
        LogMessages.Started.Should().Be("{process} started.");
    }

    [Fact]
    public void Completed_ContainsExpectedTemplate()
    {
        LogMessages.Completed.Should().Be("{process} completed.");
    }

    [Fact]
    public void Failed_ContainsExpectedTemplate()
    {
        LogMessages.Failed.Should().Be("{process} failed.");
    }

    [Fact]
    public void Elapsed_ContainsTimePlaceholder()
    {
        LogMessages.Elapsed.Should().Contain("{time}");
        LogMessages.Elapsed.Should().Contain("ms");
    }
}
