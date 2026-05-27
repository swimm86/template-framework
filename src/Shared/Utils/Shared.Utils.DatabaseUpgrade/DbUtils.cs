// ----------------------------------------------------------------------------------------------
// <copyright file="DbUtils.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Shared.Utils.DatabaseUpgrade;

/// <summary>
/// Класс для изменения бд.
/// </summary>
public static class DbUtils
{
    private const string ConnectionStringPath = "ConnectionString";
    private const int ScriptPathsArgumentIndex = 0;

    /// <summary>
    /// Обновление бд с аргементам командной строки.
    /// </summary>
    /// <param name="args">Аргументы.</param>
    public static void Upgrade(string[] args)
    {
        var connectionString = GetConnectionStringFromSecrets(Assembly.GetCallingAssembly());
        var scriptPaths = GetScriptPaths(args);

        if (scriptPaths == null)
        {
            throw new ArgumentException("Необходимо указать параметр запуска с путями до скриптов миграций.");
        }

        var scriptPathsAsArray = scriptPaths.Split(",");

        foreach (var scriptPath in scriptPathsAsArray)
        {
            Console.WriteLine($"Применяем скрипты для пути ${scriptPath} ...");
            Upgrade(
                scriptsPath: scriptPath,
                connectionString: connectionString);
        }
    }

    /// <summary>
    /// Обновление бд.
    /// </summary>
    /// <param name="connectionString"> Значение строки соединения.</param>
    /// <param name="connectionStringKey"> Ключ строки соединения в файле настроек.</param>
    /// <param name="scriptsPath"> Путь к папке со скриптами как Embedded resource, например "DatabaseUpgrade.Scripts".</param>
    /// <param name="scriptsAssemblyName"> Наименование сборки со скриптами.</param>
    /// <exception cref="InvalidOperationException"> Если произошли ошибки выполнения скриптов.</exception>
    /// <exception cref="ArgumentException"> Если не удалось получить корректную строку соединения.</exception>
    public static void Upgrade(
        string? connectionString = null,
        string? connectionStringKey = null,
        string? scriptsPath = null,
        string? scriptsAssemblyName = null)
    {
        var currentConnectionString = string.IsNullOrEmpty(connectionString)
            ? GetConnectionString(connectionStringKey)
            : connectionString;

        if (string.IsNullOrEmpty(currentConnectionString))
        {
            throw new ArgumentException("Не удалось получить корректную строку соединения.");
        }

        try
        {
            DbMigrator.Upgrade(currentConnectionString, scriptsPath, scriptsAssemblyName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка выполнения миграции БД.");
            Console.WriteLine(ex.ToString());
            throw;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Миграция БД выполнена успешно.");
        Console.ResetColor();
    }

    /// <summary>
    /// Получение строки из UserSecrets.
    /// </summary>
    /// <param name="key">Ключ ConnectionString.</param>
    /// <typeparam name="T">Тип, в ассембли которого будет идти поиск UserSecrets.</typeparam>
    /// <returns>Строка из UserSecrets.</returns>
    public static string? GetConnectionStringFromSecrets<T>(string? key = null)
        where T : class
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<T>();

        var configuration = builder.Build();

        return configuration.GetSection(string.IsNullOrEmpty(key) ? ConnectionStringPath : key).Value;
    }

    /// <summary>
    /// Получение строки из UserSecrets.
    /// </summary>
    /// <param name="assembly">Сборка.</param>
    /// <param name="key">Ключ ConnectionString.</param>
    /// <returns>Строка из UserSecrets.</returns>
    public static string? GetConnectionStringFromSecrets(Assembly assembly, string? key = null)
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets(assembly);

        var configuration = builder.Build();

        return configuration.GetSection(string.IsNullOrEmpty(key) ? ConnectionStringPath : key).Value;
    }

    private static string? GetConnectionString(string? key = null)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return configuration.GetSection(string.IsNullOrEmpty(key) ? ConnectionStringPath : key).Value;
    }

    private static string? GetScriptPaths(string[] args)
    {
        return args.Length > 0 ? args[ScriptPathsArgumentIndex] : Environment.GetEnvironmentVariable("ScriptPaths");
    }
}
