using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class TestBeforeSaveChangesService
    : IBeforeSaveChangesService
{
    public int ProcessAsyncCallCount { get; private set; }
    public int ProcessCallCount { get; private set; }
    public DbContext? LastDbContext { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }
    public Func<Task>? OnProcessAsync { get; set; }
    public Action? OnProcess { get; set; }

    public Task ProcessAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ProcessAsyncCallCount++;
        LastDbContext = dbContext;
        LastCancellationToken = cancellationToken;
        return OnProcessAsync?.Invoke() ?? Task.CompletedTask;
    }

    public void Process(DbContext dbContext)
    {
        ProcessCallCount++;
        LastDbContext = dbContext;
        OnProcess?.Invoke();
    }
}
