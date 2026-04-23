// ----------------------------------------------------------------------------------------------
// <copyright file="DalDependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Infrastructure.Dal.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Application.Core.DependencyInjection;

namespace Gpn.Template.Infrastructure.Dal;

/// <summary>
/// Внедрение зависимостей для DAL-слоя.
/// </summary>
public class DalDependencyInjector(
    IConfiguration configuration,
    ILogger<DalDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<DbSettingsBase, DbSettings>(_ =>
            {
                var result = configuration.GetOptions<DbSettings>()!;
                return result;
            });
    }
}
