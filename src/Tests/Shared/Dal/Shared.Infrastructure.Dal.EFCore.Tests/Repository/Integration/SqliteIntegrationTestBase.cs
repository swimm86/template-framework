// ----------------------------------------------------------------------------------------------
// <copyright file="SqliteIntegrationTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Repository.Integration;

public abstract class SqliteIntegrationTestBase : IDisposable
{
    private readonly DbConnection _connection;
    private readonly string _databaseFilePath;
    private readonly string _fileConnectionString;
    private readonly object _fileDatabaseInitializationLock = new();
    private bool _fileDatabaseInitialized;

    protected SqliteIntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _databaseFilePath = Path.Combine(
            Path.GetTempPath(),
            $"ef-repository-{Guid.NewGuid():N}.db");
        _fileConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databaseFilePath,
            DefaultTimeout = 30,
            Pooling = false,
        }.ToString();
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

    /// <summary>
    /// Создаёт контекст с отдельным подключением к общей файловой базе данных SQLite текущего теста.
    /// </summary>
    /// <param name="interceptors">Перехватчики операций базы данных.</param>
    /// <returns>Контекст интеграционного теста.</returns>
    protected IntegrationTestDbContext CreateFileContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(_fileConnectionString)
            .AddInterceptors(interceptors)
            .Options;

        var context = new IntegrationTestDbContext(options);
        lock (_fileDatabaseInitializationLock)
        {
            if (!_fileDatabaseInitialized)
            {
                context.Database.EnsureCreated();
                _fileDatabaseInitialized = true;
            }
        }

        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
        File.Delete(_databaseFilePath);
        GC.SuppressFinalize(this);
    }
}
