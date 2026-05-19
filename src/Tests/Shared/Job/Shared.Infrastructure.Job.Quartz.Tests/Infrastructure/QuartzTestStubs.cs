using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;

namespace Shared.Infrastructure.Job.Quartz.Tests.Infrastructure;

internal sealed class StubJobDetail : IJobDetail
{
    public JobKey Key { get; set; } = new("stub");
    public string? Description { get; set; }
    public Type JobType { get; set; } = typeof(QuartzJobWrapper);
    public JobDataMap JobDataMap { get; set; } = new();
    public bool Durable { get; set; }
    public bool PersistJobDataAfterExecution { get; set; }
    public bool ConcurrentExecutionDisallowed { get; set; }
    public bool RequestsRecovery { get; set; }

    public JobBuilder GetJobBuilder() => throw new NotSupportedException();
    public IJobDetail Clone() => throw new NotSupportedException();
}

internal sealed class StubScheduler : IScheduler
{
    public bool ScheduleJobCalled { get; private set; }
    public ITrigger? LastScheduledTrigger { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    string IScheduler.SchedulerName => throw new NotSupportedException();
    string IScheduler.SchedulerInstanceId => throw new NotSupportedException();
    SchedulerContext IScheduler.Context => throw new NotSupportedException();
    bool IScheduler.InStandbyMode => throw new NotSupportedException();
    bool IScheduler.IsShutdown => throw new NotSupportedException();
    bool IScheduler.IsStarted => throw new NotSupportedException();
    IJobFactory IScheduler.JobFactory { set => throw new NotSupportedException(); }
    IListenerManager IScheduler.ListenerManager => throw new NotSupportedException();

    Task<bool> IScheduler.IsJobGroupPaused(string groupName, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.IsTriggerGroupPaused(string groupName, CancellationToken ct) => throw new NotSupportedException();
    Task<SchedulerMetaData> IScheduler.GetMetaData(CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<IJobExecutionContext>> IScheduler.GetCurrentlyExecutingJobs(CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<string>> IScheduler.GetJobGroupNames(CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<string>> IScheduler.GetTriggerGroupNames(CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<string>> IScheduler.GetPausedTriggerGroups(CancellationToken ct) => throw new NotSupportedException();

    Task<DateTimeOffset> IScheduler.ScheduleJob(ITrigger trigger, CancellationToken ct)
    {
        ScheduleJobCalled = true;
        LastScheduledTrigger = trigger;
        LastCancellationToken = ct;
        return Task.FromResult(DateTimeOffset.UtcNow);
    }

    Task<DateTimeOffset> IScheduler.ScheduleJob(IJobDetail jobDetail, ITrigger trigger, CancellationToken ct) =>
        throw new NotSupportedException();

    Task IScheduler.ScheduleJobs(IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs, bool replace, CancellationToken ct) =>
        throw new NotSupportedException();

    Task IScheduler.ScheduleJob(IJobDetail jobDetail, IReadOnlyCollection<ITrigger> triggersForJob, bool replace, CancellationToken ct) =>
        throw new NotSupportedException();

    Task<bool> IScheduler.UnscheduleJob(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.UnscheduleJobs(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken ct) => throw new NotSupportedException();
    Task<DateTimeOffset?> IScheduler.RescheduleJob(TriggerKey triggerKey, ITrigger newTrigger, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.AddJob(IJobDetail jobDetail, bool replace, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.AddJob(IJobDetail jobDetail, bool replace, bool storeNonDurableWhileAwaitingScheduler, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.DeleteJob(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.DeleteJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.TriggerJob(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.TriggerJob(JobKey jobKey, JobDataMap data, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.PauseJob(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.PauseTrigger(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResumeJob(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResumeTrigger(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.PauseAll(CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResumeAll(CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<JobKey>> IScheduler.GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<ITrigger>> IScheduler.GetTriggersOfJob(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<TriggerKey>> IScheduler.GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken ct) => throw new NotSupportedException();
    Task<IJobDetail?> IScheduler.GetJobDetail(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task<ITrigger?> IScheduler.GetTrigger(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task<TriggerState> IScheduler.GetTriggerState(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.ResetTriggerFromErrorState(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.AddCalendar(string calName, ICalendar calendar, bool replace, bool updateTriggers, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.DeleteCalendar(string calName, CancellationToken ct) => throw new NotSupportedException();
    Task<ICalendar?> IScheduler.GetCalendar(string calName, CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyCollection<string>> IScheduler.GetCalendarNames(CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.Interrupt(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.Interrupt(string fireInstanceId, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.CheckExists(JobKey jobKey, CancellationToken ct) => throw new NotSupportedException();
    Task<bool> IScheduler.CheckExists(TriggerKey triggerKey, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.Clear(CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.Start(CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.StartDelayed(TimeSpan delay, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.Shutdown(CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.Shutdown(bool waitForJobsToComplete, CancellationToken ct) => throw new NotSupportedException();
    Task IScheduler.Standby(CancellationToken ct) => throw new NotSupportedException();
}

internal sealed class StubTrigger : ITrigger
{
    public TriggerKey Key => throw new NotSupportedException();
    public JobKey JobKey { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public string Description { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public string CalendarName { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public JobDataMap JobDataMap { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public int Priority { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public DateTimeOffset StartTimeUtc { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public DateTimeOffset? EndTimeUtc { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public DateTimeOffset? FinalFireTimeUtc { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public int MisfireInstruction { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public bool HasMillisecondPrecision => throw new NotSupportedException();

    public DateTimeOffset? GetNextFireTimeUtc() => throw new NotSupportedException();
    public DateTimeOffset? GetPreviousFireTimeUtc() => throw new NotSupportedException();
    public DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTimeUtc) => throw new NotSupportedException();
    public bool GetMayFireAgain() => throw new NotSupportedException();
    public TriggerBuilder GetTriggerBuilder() => throw new NotSupportedException();
    public IScheduleBuilder GetScheduleBuilder() => throw new NotSupportedException();
    public ITrigger Clone() => throw new NotSupportedException();
    public int CompareTo(ITrigger? other) => throw new NotSupportedException();
}

internal sealed class StubJobExecutionContext : IJobExecutionContext
{
    public IScheduler Scheduler { get; set; } = null!;
    public ITrigger Trigger { get; set; } = null!;
    public ICalendar? Calendar { get; set; }
    public IJobDetail JobDetail { get; set; } = null!;
    public JobDataMap MergedJobDataMap { get; set; } = new();
    public DateTimeOffset FireTimeUtc { get; set; }
    public DateTimeOffset? ScheduledFireTimeUtc { get; set; }
    public DateTimeOffset? PreviousFireTimeUtc { get; set; }
    public DateTimeOffset? NextFireTimeUtc { get; set; }
    public TimeSpan JobRunTime { get; set; }
    public object? Result { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public int RefireCount { get; set; }
    public bool Recovering { get; set; }
    public TriggerKey RecoveringTriggerKey { get; set; } = null!;
    public IJob JobInstance { get; set; } = null!;
    public string FireInstanceId { get; set; } = null!;

    public object? Get(object key) => MergedJobDataMap[(string)key];
    public void Put(object key, object value) => MergedJobDataMap[(string)key] = value;
}
