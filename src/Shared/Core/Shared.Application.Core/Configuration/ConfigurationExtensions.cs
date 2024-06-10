// ----------------------------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Shared.Common;
using Shared.Common.Extensions;

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
    /// <returns> Настройки. </returns>
    public static TOptions? GetOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class
    {
        var moduleName = Helpers.GetModuleName();
        var parts = moduleName.Split('.');
        IConfigurationSection? section = default;
        parts.ForEach(part =>
        {
            var currentSection = (section ?? configuration).GetSection(part);
            if (currentSection.GetChildren().Any()) section = currentSection;
        });

        return section?.GetSection($"{typeof(TOptions).Name}").Get<TOptions>();
    }
}
