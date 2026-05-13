// ----------------------------------------------------------------------------------------------
// <copyright file="DbMigrator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using DbUp;
using Shared.Common.Helpers;

namespace Shared.Utils.DatabaseUpgrade;

/// <summary>
/// Класс для запуска скриптов миграций.
/// </summary>
internal static class DbMigrator
{
    private const string ScriptsFolder = "Scripts";

    /// <summary>
    /// Запускает скрипты миграций.
    /// </summary>
    /// <param name="connectionString"> Строка подключения к базе данных.</param>
    /// <param name="scriptsPath"> Путь к папке со скриптами как Embedded resource, например "DatabaseUpgrade.Scripts".</param>
    /// <param name="scriptsAssemblyName"> Наименование сборки со скриптами.</param>
    /// <exception cref="InvalidOperationException"> Если произошли ошибки выполнения скриптов.</exception>
    public static void Upgrade(string connectionString, string? scriptsPath = null, string? scriptsAssemblyName = null)
    {
        // Проверка что база существует. Создание в случае отсутствия.
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var assembly = string.IsNullOrEmpty(scriptsAssemblyName)
            ? GetAssembly() ?? Assembly.GetEntryAssembly()
            : AppDomain.CurrentDomain.GetAssemblyByName(scriptsAssemblyName);
        var assemblyName = assembly?.GetName().Name ??
                           throw new InvalidOperationException("Не удалось получить наименование сборки.");

        var executor = DeployChanges
            .To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(assembly, s =>
                s.StartsWith(
                    string.Join('.', assemblyName, scriptsPath ?? ScriptsFolder),
                    StringComparison.InvariantCultureIgnoreCase))
            .LogToConsole()
            .Build();

        var result = executor.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Ошибка применения скриптов миграций.", result.Error);
        }
    }

    private static Assembly? GetAssembly()
        => new StackTrace().GetFrames()
            .Select(x => x.GetMethod()?.ReflectedType?.Assembly).Distinct()
            .LastOrDefault(x =>
                x?.GetReferencedAssemblies().Any(y => y.FullName == Assembly.GetExecutingAssembly().FullName) == true);
}
