// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederDependencyInjection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class DbSeederDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации <see cref="IDbSeeder"/> в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленными <see cref="IDbSeeder"/>.</returns>
    public static IServiceCollection AddDbSeeder(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<IDbSeeder, Implementation.DbSeeder>();
}