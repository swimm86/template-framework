// ----------------------------------------------------------------------------------------------
// <copyright file="SpecificationDependencyInjection.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Application.Core.DependencyInjection;

namespace Shared.Application.Core.Dal.Specification.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class SpecificationDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации спецификационных репозиториев в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными спецификационными репозиториями.</returns>
    public static IServiceCollection AddSpecificationRepositories(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependenciesTransient(typeof(ISpecificationRepository<>));
}
