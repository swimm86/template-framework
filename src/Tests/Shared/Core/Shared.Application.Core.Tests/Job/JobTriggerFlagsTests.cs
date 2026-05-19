using Shared.Application.Core.Job;

namespace Shared.Application.Core.Tests;

public sealed class JobTriggerFlagsTests
{
    [Fact]
    public void Daily_HasCorrectFlag()
    {
        var flag = JobTriggerFlags.Daily;

        flag.Should().HaveFlag(JobTriggerFlags.Daily);
        ((int)flag).Should().Be(1);
    }

    [Fact]
    public void CombinedFlags_HasBothFlags()
    {
        var combined = JobTriggerFlags.Daily | JobTriggerFlags.EveryMinute;

        combined.Should().HaveFlag(JobTriggerFlags.Daily);
        combined.Should().HaveFlag(JobTriggerFlags.EveryMinute);
        combined.HasFlag(JobTriggerFlags.Weekly).Should().BeFalse();
    }
}
