// ----------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Shared.Common;

namespace Shared.Application.Core.Configuration;

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
        var moduleName = Helpers.GetModuleName();
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
}
