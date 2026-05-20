using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Dal.EFCore.Settings;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

public abstract class SqliteUnitOfWorkIntegrationTestBase : IDisposable
{
    private readonly DbConnection _connection;
    private readonly FakeHostEnvironment _environment = new(Environments.Development);

    protected SqliteUnitOfWorkIntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected IntegrationTestUnitOfWorkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationTestUnitOfWorkDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new IntegrationTestUnitOfWorkDbContext(options, _environment);
        context.Database.EnsureCreated();
        return context;
    }

    protected static EfUnitOfWork<IntegrationTestUnitOfWorkDbContext> CreateUnitOfWork(
        IntegrationTestUnitOfWorkDbContext context,
        bool transactionsEnabled = true)
    {
        var settings = new IntegrationTestEfDbSettings(transactionsEnabled);
        return new EfUnitOfWork<IntegrationTestUnitOfWorkDbContext>(
            context,
            new FakeServiceProvider(),
            settings);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class IntegrationTestEfDbSettings : EfDbSettingsBase<IntegrationTestUnitOfWorkDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public IntegrationTestEfDbSettings(bool transactionsEnabled = true)
        {
            ConnectionString = "DataSource=:memory:";
            TransactionsEnabled = transactionsEnabled;
        }
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) => _services[typeof(T)] = instance!;

        public object? GetService(Type serviceType) =>
            _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
