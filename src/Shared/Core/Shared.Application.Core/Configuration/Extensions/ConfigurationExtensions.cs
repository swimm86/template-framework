// ----------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Common.Helpers;

namespace Shared.Application.Core.Configuration.Extensions;

/// <summary>
/// Методы расширения для работы с <see cref="IConfiguration"/>.
/// Предоставляет методы для загрузки конфигурации из различных источников,
/// получения настроек по модулю приложения и инициализации конфигурации.
/// </summary>
public static class ConfigurationExtensions
{
    private const string EnvFileName = ".env";

    /// <summary>
    /// Получает настройки указанного типа из конфигурации.
    /// Настройки выбираются на основе имени модуля приложения (извлечённого из сборки)
    /// и иерархической структуры секций конфигурации.
    /// </summary>
    /// <typeparam name="TOptions">
    /// Тип настроек, которые необходимо получить. Должен быть ссылочным типом.
    /// </typeparam>
    /// <param name="configuration">
    /// Экземпляр <see cref="IConfiguration"/>, представляющий текущую конфигурацию приложения.
    /// </param>
    /// <returns>
    /// Экземпляр настроек типа <typeparamref name="TOptions"/> или <see langword="null"/>,
    /// если настройки не найдены.
    /// </returns>
    /// <remarks>
    /// <para>Логика работы:</para>
    /// <list type="number">
    /// <item>Определяется имя модуля приложения (например, "App.Getter.Api").</item>
    /// <item>Имя модуля разбивается на части по точке (например, ["App", "Getter", "Api"]).</item>
    /// <item>Для каждой части ищется соответствующая секция в конфигурации.</item>
    /// <item>Если найдена секция, содержащая подсекцию с именем типа <typeparamref name="TOptions"/>,
    /// её значения десериализуются в экземпляр типа <typeparamref name="TOptions"/>.</item>
    /// <item>Возвращается последняя найденная конфигурация (по принципу наибольшей специфичности).</item>
    /// </list>
    /// <para>Пример:</para>
    /// <code>
    /// app__getter__setting__value="app.get"
    /// app__setting__value="app"
    /// </code>
    /// <para>Результат:</para>
    /// <list type="bullet">
    /// <item>Если источником является "App.Getter.Api", то результат: "app.get".</item>
    /// <item>Если источником является любой другой сервис, то результат: "app".</item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="configuration"/> равен <see langword="null"/>.
    /// </exception>
    public static TOptions? GetOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class
    {
        var moduleName = AssemblyHelper.GetModuleName();
        var parts = moduleName.Split('.').ToList();
        var results = new List<TOptions>();
        IConfigurationSection? entrySection = null;

        foreach (var part in parts)
        {
            var currentSection = (entrySection ?? configuration).GetSection(part);
            var hasChildren = currentSection.GetChildren().Any();
            entrySection = hasChildren ? currentSection : entrySection;

            if (entrySection != null && !hasChildren)
            {
                break;
            }

            if (!hasChildren)
            {
                continue;
            }

            var option = currentSection.GetSection($"{typeof(TOptions).Name}").Get<TOptions>();
            if (option != null)
            {
                results.Add(option);
            }
        }

        return results.LastOrDefault();
    }

    /// <summary>
    /// Инициализирует конфигурацию приложения, добавляя переменные окружения
    /// и загружая файлы конфигурации окружения (.env).
    /// </summary>
    /// <param name="configuration">
    /// Построитель конфигурации (<see cref="IConfigurationBuilder"/>), который будет настроен.
    /// </param>
    /// <param name="hostEnvironment">
    /// Среда выполнения приложения (<see cref="IHostEnvironment"/>),
    /// используемая для определения имени окружения.
    /// </param>
    /// <remarks>
    /// Метод выполняет следующие действия:
    /// <list type="number">
    /// <item>Добавляет переменные окружения в конфигурацию.</item>
    /// <item>Загружает файлы конфигурации окружения (.env) с учётом текущей среды выполнения.</item>
    /// </list>
    /// </remarks>
    public static void InitializeConfiguration(
        this IConfigurationBuilder configuration,
        IHostEnvironment hostEnvironment)
    {
        configuration
            .AddEnvironmentVariables()
            .LoadEnv(hostEnvironment);
    }

    /// <summary>
    /// Загружает файлы конфигурации окружения (.env) в построитель конфигурации.
    /// </summary>
    /// <param name="configurationBuilder">
    /// Построитель конфигурации (<see cref="IConfigurationBuilder"/>), который будет настроен.
    /// </param>
    /// <param name="hostEnvironment">
    /// Среда выполнения приложения (<see cref="IHostEnvironment"/>),
    /// используемая для определения имени окружения.
    /// </param>
    /// <returns>
    /// Построитель конфигурации с добавленными источниками конфигурации.
    /// </returns>
    /// <remarks>
    /// Метод проверяет наличие следующих файлов в директории приложения:
    /// <list type="bullet">
    /// <item>.env</item>
    /// <item>.env.{EnvironmentName}</item>
    /// </list>
    /// Если файл существует, он загружается в конфигурацию.
    /// Файл с указанием окружения имеет приоритет над базовым файлом .env:
    /// ключи, присутствующие только в <c>.env</c>, сохраняются, ключи,
    /// переопределённые в <c>.env.{EnvironmentName}</c>, заменяют значения из базового файла.
    /// </remarks>
    public static IConfigurationBuilder LoadEnv(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment)
    {
        return configurationBuilder.LoadEnvFromPath(
            AppContext.BaseDirectory,
            hostEnvironment.EnvironmentName);
    }

    /// <summary>
    /// Загружает <c>.env</c> и <c>.env.{environmentName}</c> из указанной директории
    /// в построитель конфигурации.
    /// </summary>
    /// <param name="basePath">Директория, в которой ищутся файлы <c>.env</c>.</param>
    /// <param name="environmentName">Имя окружения; используется для поиска <c>.env.{environmentName}</c>.</param>
    /// <inheritdoc cref="LoadEnv"/>
    /// <param name="configurationBuilder"/>
    internal static IConfigurationBuilder LoadEnvFromPath(
        this IConfigurationBuilder configurationBuilder,
        string basePath,
        string environmentName)
    {
        var envPath = Path.Combine(basePath, EnvFileName);
        if (File.Exists(envPath))
        {
            configurationBuilder.AddDotNetEnv(envPath);
        }

        var envSpecificPath = Path.Combine(
            basePath,
            $"{EnvFileName}.{environmentName.ToLowerInvariant()}");
        if (File.Exists(envSpecificPath))
        {
            configurationBuilder.AddDotNetEnv(envSpecificPath);
        }

        return configurationBuilder;
    }
}
