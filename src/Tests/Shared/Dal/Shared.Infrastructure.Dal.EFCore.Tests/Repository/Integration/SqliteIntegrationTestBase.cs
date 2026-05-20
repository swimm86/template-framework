using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

public abstract class SqliteIntegrationTestBase : IDisposable
{
    private readonly DbConnection _connection;

    protected SqliteIntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected IntegrationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new IntegrationTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
