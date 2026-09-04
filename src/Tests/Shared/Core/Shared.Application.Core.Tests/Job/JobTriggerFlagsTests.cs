// ----------------------------------------------------------------------------------------------
// <copyright file="JobTriggerFlagsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Enums;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты для флагов <see cref="JobTriggerFlags"/>.
/// </summary>
public sealed class JobTriggerFlagsTests
{
    /// <summary>
    /// Комбинация флагов содержит оба значения.
    /// </summary>
    [Fact]
    public void CombinedFlags_HasBothFlags()
    {
        // Act
        var combined = JobTriggerFlags.Daily | JobTriggerFlags.EveryMinute;

        // Assert
        combined.Should().HaveFlag(JobTriggerFlags.Daily);
        combined.Should().HaveFlag(JobTriggerFlags.EveryMinute);
        combined.HasFlag(JobTriggerFlags.Weekly).Should().BeFalse();
    }
}
