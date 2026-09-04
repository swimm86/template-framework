// ----------------------------------------------------------------------------------------------
// <copyright file="SqliteIntegrationTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Mapper.Tests.Integration;

/// <summary>
/// Базовый класс интеграционных тестов с реальной SQLite-БД в памяти.
/// Содержит подключение к in-memory SQLite и фабрику <see cref="ProjectToTestDbContext"/>.
/// Общий для всех реализаций мапперов (AutoMapper, Mapster и т.д.).
/// </summary>
public abstract class SqliteIntegrationTestBase
    : IDisposable
{
    private readonly DbConnection _connection;

    protected SqliteIntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Создаёт новый <see cref="ProjectToTestDbContext"/> с уникальной in-memory базой.
    /// </summary>
    protected ProjectToTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProjectToTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ProjectToTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
