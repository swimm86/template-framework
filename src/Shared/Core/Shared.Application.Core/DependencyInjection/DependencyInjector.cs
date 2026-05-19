// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjector.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Validators;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Application.Core.Cache;
using Shared.Application.Core.Dal.DbSeeder.Extensions;
using Shared.Application.Core.Dal.Repository.Extensions;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Application.Core.Json;
using Shared.Domain.Core.Cache.Interfaces;
using Shared.Domain.Core.Utils.Extensions;

namespace Shared.Application.Core.DependencyInjection;

/// <summary>
/// Регистрация DI-зависимостей слоя <c>Shared.Application.Core</c>.
/// </summary>
/// <remarks>
/// Регистрирует системные сервисы: аксессор HTTP-контекста, сериализатор JSON,
/// репозитории, средства наполнения БД (DbSeeder), утилиты свойств и кэширование.
/// <para><inheritdoc cref="DependencyInjectorBase" path="/remarks"/></para>
/// </remarks>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
public class DependencyInjector(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(
        IServiceCollection serviceCollection)
    {
        return serviceCollection
            // TODO: вынести в Presentation-layer после миграции туда ApiClient и других Http-зависимостей
            .AddHttpContextAccessor()
            .ConfigureJsonSerializer()
            .AddRepositories()
            .AddDbSeeder()
            .AddPropertyUtil()
            .AddSingleton<IUriValidator, RelativeUriValidator>()
            .AddSingleton<IResponseValidator, ProxiedResponseValidator>()
            .AddScoped<IScopedMemoryCache, ScopedMemoryCache>();
    }
}
