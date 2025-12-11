// ----------------------------------------------------------------------------------------------
// <copyright file="DbUpdaterDependencyInjection.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.DbUpdater.Interfaces;
using Shared.Application.Core.DependencyInjection.Extensions;

namespace Shared.Application.Core.Dal.DbUpdater.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class DbUpdaterDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации <see cref="IDbUpdater"/> в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными <see cref="IDbUpdater"/>.</returns>
    public static IServiceCollection AddDatabaseUpdater(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependencies<IDbUpdater>(ServiceLifetime.Scoped);
}
