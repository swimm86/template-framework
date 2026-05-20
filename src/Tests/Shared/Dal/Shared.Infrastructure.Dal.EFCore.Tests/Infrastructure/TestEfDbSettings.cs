using System.Diagnostics.CodeAnalysis;
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class TestEfDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public TestEfDbSettings(bool transactionsEnabled = true)
    {
        ConnectionString = "Server=localhost;Database=test;";
        TransactionsEnabled = transactionsEnabled;
    }
}
