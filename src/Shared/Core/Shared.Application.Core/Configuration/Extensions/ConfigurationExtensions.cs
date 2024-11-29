// ----------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.Configuration.Attributes;
using Shared.Common.Helpers;

namespace Shared.Application.Core.Configuration.Extensions;

/// <summary>
/// Класс, который содержит расширения для <see cref="IConfiguration"/>
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Получение настроек.
    /// </summary>
    /// <typeparam name="TOptions"> Тип настроек. </typeparam>
    /// <param name="configuration"> Конфигурация. </param>
    /// <remarks>
    /// <para>Пример.</para>
    /// <para>Источник: App.Getter.Api</para>
    /// <code>
    ///   app__getter__setting__value="app.get"
    ///   app__setting__value="app"
    /// </code>
    /// <para>Результат: app.get (Если бы источником был App.Setter.Api, то получили бы app)</para>
    /// </remarks>
    /// <returns> Настройки. </returns>
    public static TOptions? GetOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class
    {
        var moduleName = AssemblyHelper.GetModuleName();
        var parts = moduleName.Split('.').ToList();
        var results = new List<TOptions>();
        IConfigurationSection? entrySection = default;

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
    /// Инициализирует конфигурацию.
    /// </summary>
    /// <param name="configuration">Построитель конфигурации.</param>
    /// <param name="hostEnvironment">Среда выполнения приложения.</param>
    public static void InitializeConfiguration(
        this IConfigurationBuilder configuration,
        IHostEnvironment hostEnvironment)
    {
        configuration
            .AddEnvironmentVariables()
            .LoadEnv(hostEnvironment);
    }

    /// <summary>
    /// Загружает файлы конфигурации окружения.
    /// </summary>
    /// <param name="configurationBuilder">Построитель конфигурации.</param>
    /// <param name="hostEnvironment">Среда выполнения приложения.</param>
    /// <returns>Построитель конфигурации.</returns>
    public static IConfigurationBuilder LoadEnv(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment)
    {
        var envPath =
            AssemblyHelper.GetAttributesFromAssemblies<EnvPathAttribute>().FirstOrDefault()?.Path;
        var envDirectory = string.Empty;
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            Directory.SetCurrentDirectory(assemblyPath);

            envPath = Path.GetFullPath(envPath);
            envDirectory = Path.GetDirectoryName(envPath);
        }

        if (!Directory.Exists(envDirectory))
        {
            return configurationBuilder;
        }

        if (File.Exists(envPath))
        {
            configurationBuilder.AddDotNetEnv(envPath);
        }

        var currentEnv =
            Path.Combine(envDirectory, $"{envPath}.{hostEnvironment.EnvironmentName.ToLower()}");
        if (File.Exists(currentEnv))
        {
            configurationBuilder.AddDotNetEnv(currentEnv);
        }

        return configurationBuilder;
    }
}
