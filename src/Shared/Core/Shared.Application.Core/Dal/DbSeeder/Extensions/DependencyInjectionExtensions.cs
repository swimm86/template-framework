// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Extensions;

/// <summary>
/// Методы расширения для регистрации сервисов заполнения БД в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует реализацию <see cref="IDbSeeder"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDbSeeder(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<IDbSeeder, Implementation.DbSeeder>();
}