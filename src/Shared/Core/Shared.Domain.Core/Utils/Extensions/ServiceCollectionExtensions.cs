// ----------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Domain.Core.Utils.Extensions;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Метод расширения для IServiceCollection, который регистрирует зависимости, связанные с <see cref="PropertyUtil"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <returns>Измененная коллекция сервисов с добавленным <see cref="PropertyUtil"/>.</returns>
    public static IServiceCollection AddPropertyUtil(
        this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<PropertyUtil>()
            .AddSingleton<IPropertyGetter>(sp => sp.GetRequiredService<PropertyUtil>())
            .AddSingleton<IPropertySetter>(sp => sp.GetRequiredService<PropertyUtil>());
    }
}