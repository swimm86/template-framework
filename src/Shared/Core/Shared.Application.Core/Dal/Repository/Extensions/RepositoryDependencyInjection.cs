// ----------------------------------------------------------------------------------------------
// <copyright file="RepositoryDependencyInjection.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.DependencyInjection;

namespace Shared.Application.Core.Dal.Repository.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class RepositoryDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации репозиториев в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными репозиториями.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependenciesTransient(typeof(IRepository<>));
}
