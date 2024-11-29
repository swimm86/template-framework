// ----------------------------------------------------------------------------------------------
// <copyright file="RepositoryDependencyInjection.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;

namespace Shared.Application.Core.Dal.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class DbSeederDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации репозиториев в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными репозиториями.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependencies(typeof(IRepository<>));
}
