// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientDependencyInjection.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Settings.Base;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Application.Core.ApiClient;

/// <summary>
/// Методы расширения для <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiClientDependencyInjection
{
    /// <summary>
    /// Метод расширения для регистрации всех производных HttpClient в IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для регистрации.</param>
    /// <param name="configuration"><see cref="IConfiguration"/>.</param>
    /// <returns>Измененная коллекция сервисов с добавленными контекстами данных.</returns>
    public static IServiceCollection AddHttpClients(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        AssemblyHelper.GetDerivedTypesFromAssemblies<ApiClient>(
                excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
            .ForEach(type =>
            {
                var settings = AssemblyHelper
                    .GetDerivedTypesFromAssemblies(
                        typeof(ApiClientSettingsBase<>).MakeGenericType(type),
                        excludedAttributesTypes: [typeof(ManualConfigurationAttribute)])
                    .FirstOrDefault();
                if (settings is null)
                {
                    throw new InvalidOperationException($"Не удалось найти настройки для типа {type.FullName}");
                }

                var method = typeof(ApiClientExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m =>
                        m.Name == nameof(ApiClientExtensions.AddClient) &&
                        m.GetParameters().Length == 2 &&
                        m.GetGenericArguments().Length == 3);
                var @interface = type
                    .GetInterfaces()
                    .FirstOrDefault(@interface => @interface.Name == $"I{type.Name}");
                if (@interface is null)
                {
                    throw new InvalidOperationException($"Не удалось найти интефейс для типа {type.FullName}");
                }

                method
                    .MakeGenericMethod(settings, @interface, type)
                    .Invoke(null, [serviceCollection, configuration]);
            });

        return serviceCollection;
    }
}
