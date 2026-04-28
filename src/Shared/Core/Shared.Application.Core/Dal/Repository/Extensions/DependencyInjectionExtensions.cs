// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.DependencyInjection.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Extensions;

/// <summary>
/// Методы расширения для регистрации DAL-репозиториев в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует реализации репозиториев.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection serviceCollection) =>
        serviceCollection.RegisterDerivedTypeDependency(
            typeof(IRepository<>),
            serviceTypeAsInterface: true,
            ServiceLifetime.Transient);
}
