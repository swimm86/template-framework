// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;
using Shared.Application.Core.DependencyInjection.Extensions;

namespace Shared.Application.Core.Dal.DbUpdater.Extensions;

/// <summary>
/// Методы расширения для регистрации сервисов обновления БД в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует реализацию <see cref="IDbUpdater"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDatabaseUpdater(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependency<IDbUpdater>(
            serviceTypeAsInterface: true,
            ServiceLifetime.Scoped);
}
