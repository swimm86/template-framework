using Shared.Application.Core.Job.Enums;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты для флагов <see cref="JobTriggerFlags"/>.
/// </summary>
public sealed class JobTriggerFlagsTests
{
    /// <summary>
    /// Флаг <see cref="JobTriggerFlags.Daily"/> имеет корректное числовое значение (1).
    /// </summary>
    [Fact]
    public void Daily_HasCorrectFlag()
    {
        // Act
        var flag = JobTriggerFlags.Daily;

        // Assert
        flag.Should().HaveFlag(JobTriggerFlags.Daily);
        ((int)flag).Should().Be(1);
    }

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
