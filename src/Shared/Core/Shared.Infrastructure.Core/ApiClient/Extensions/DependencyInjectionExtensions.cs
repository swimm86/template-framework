// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Attributes;
using Shared.Application.Core.ApiClient.Handlers.Attributes;
using Shared.Application.Core.ApiClient.Handlers.Attributes.Base;
using Shared.Application.Core.DependencyInjection.Extensions;
using Shared.Common.Extensions;
using Shared.Common.Helpers;

namespace Shared.Infrastructure.Core.ApiClient.Extensions;

/// <summary>
/// Методы расширения для регистрации API-клиентов в <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Регистрирует API-клиенты автоматически на основе найденных наследников <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">Конфигурация приложения <see cref="IConfiguration"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddHttpClients(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var apiClientTypes = AssemblyHelper.GetDerivedTypesFromAssemblies<Application.Core.ApiClient.ApiClient>(
                includedAttributesTypes: [typeof(ApiClientRegistrationAttribute)])
            .ToArray();
        if (apiClientTypes.Length == 0)
        {
            return serviceCollection;
        }

        var addClientMethod = ResolveAddClientMethod();
        apiClientTypes
            .ForEach(type =>
            {
                var settings = type.GetCustomAttribute<ApiClientRegistrationAttribute>()!;
                Type[] genericTypes = [settings.SettingsType, settings.ApiClientInterfaceType, type];
                addClientMethod
                    .MakeGenericMethod(genericTypes)
                    .Invoke(null, [serviceCollection, configuration]);
            });

        return serviceCollection;
    }

    /// <summary>
    /// Регистрирует все наследники <see cref="DelegatingHandler"/> помеченные атрибутом
    /// <see cref="ApiClientDelegatingHandleMetadataAttribute"/> в контейнере зависимостей.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddDelegatingHandlers(
        this IServiceCollection serviceCollection)
    {
        var handlersData = ValidateHandlers<DelegatingHandler, ApiClientDelegatingHandleMetadataAttribute>();
        ValidateDelegatingHandlersCollisions(handlersData);
        return serviceCollection.RegisterDerivedTypeDependencies<DelegatingHandler>(
            serviceTypeAsInterface: false,
            lifetime: ServiceLifetime.Transient,
            [typeof(ApiClientDelegatingHandleMetadataAttribute)]);
    }

    /// <summary>
    /// Регистрирует все наследники <see cref="HttpClientHandler"/> помеченные атрибутом
    /// <see cref="ApiClientPrimaryHttpHandlerMetadataAttribute"/> в контейнере зависимостей.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
    /// <returns>Текущая коллекция сервисов <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddPrimaryHttpMessageHandlers(
        this IServiceCollection serviceCollection)
    {
        var handlersData = ValidateHandlers<HttpClientHandler, ApiClientPrimaryHttpHandlerMetadataAttribute>();
        ValidatePrimaryHandlersUniqueness(handlersData);
        return serviceCollection.RegisterDerivedTypeDependencies<HttpClientHandler>(
            serviceTypeAsInterface: false,
            lifetime: ServiceLifetime.Transient,
            [typeof(ApiClientPrimaryHttpHandlerMetadataAttribute)]);
    }

    private static (Type type, TAttribute metadata)[] ValidateHandlers<
        THandler,
        TAttribute>()
        where TAttribute : ApiClientHandlerMetadataAttributeBase
    {
        var handlersData = AssemblyHelper.GetDerivedTypesFromAssemblies<THandler>(
                includedAttributesTypes: [typeof(TAttribute)])
            .Select(type => (type, type.GetCustomAttribute<TAttribute>()))
            .ToArray();
        ValidateHandlersMetadata(handlersData);
        return handlersData!;
    }

    /// <summary>
    /// Гарантирует, что для каждого API-клиента применим не более одного primary-handler-а,
    /// независимо от <see cref="ApiClientDelegatingHandleMetadataAttribute.Order"/>.
    /// </summary>
    private static void ValidatePrimaryHandlersUniqueness(
        (Type type, ApiClientPrimaryHttpHandlerMetadataAttribute metadata)[] data)
    {
        if (data.Length <= 1)
        {
            return;
        }

        var apiClientTypes = AssemblyHelper
            .GetDerivedTypesFromAssemblies<Application.Core.ApiClient.ApiClient>()
            .ToArray();

        var overlaps = apiClientTypes
            .Select(clientType => new
            {
                clientType,
                applicable = data
                    .Where(x => x.metadata.AppliesTo(clientType))
                    .Select(x => x.type.FullName)
                    .OrderBy(name => name)
                    .ToArray(),
            })
            .Where(x => x.applicable.Length > 1)
            .ToArray();

        if (overlaps.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Multiple {nameof(HttpClientHandler)} were found for the same API client. " +
            $"Only one primary handler per client is allowed. " +
            string.Join(
                " | ",
                overlaps.Select(x => $"{x.clientType.Name}: {string.Join(", ", x.applicable)}")));
    }

    private static void ValidateHandlersMetadata<TAttribute>(
        IReadOnlyCollection<(Type type, TAttribute? attribute)> data)
        where TAttribute : ApiClientHandlerMetadataAttributeBase
    {
        var invalidData = data
            .Where(x => x.attribute is null)
            .ToArray();
        if (invalidData.Any())
        {
            throw new InvalidOperationException(
                $"API client handlers missing {typeof(TAttribute).Name}:{Environment.NewLine}" +
                string.Join(", ", invalidData.Select(x => x.type.FullName)));
        }
    }

    private static void ValidateDelegatingHandlersCollisions(
        (Type type, ApiClientDelegatingHandleMetadataAttribute metadata)[] data)
    {
        if (data.Length <= 1)
        {
            return;
        }

        var apiClientTypes = AssemblyHelper
            .GetDerivedTypesFromAssemblies<Application.Core.ApiClient.ApiClient>()
            .ToArray();
        var collisions = new List<string>();
        for (var i = 0; i < data.Length; i++)
        {
            for (var j = i + 1; j < data.Length; j++)
            {
                var first = data[i];
                var second = data[j];
                if (first.metadata.Order != second.metadata.Order)
                {
                    continue;
                }

                var overlappingClients = apiClientTypes
                    .Where(clientType =>
                        first.metadata.AppliesTo(clientType) &&
                        second.metadata.AppliesTo(clientType))
                    .Select(clientType => clientType.Name)
                    .OrderBy(name => name)
                    .ToArray();

                if (overlappingClients.Length == 0)
                {
                    continue;
                }

                collisions.Add(
                    $"[{nameof(DelegatingHandler)}] order={first.metadata.Order}: " +
                    $"{first.type.FullName} and {second.type.FullName}; " +
                    $"API clients: {string.Join(", ", overlappingClients)}");
            }
        }

        if (collisions.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{nameof(ApiClientDelegatingHandleMetadataAttribute)} collisions were detected. " +
            $"For a single handler type, the same {nameof(ApiClientDelegatingHandleMetadataAttribute.Order)} is not allowed " +
            $"when {nameof(ApiClientDelegatingHandleMetadataAttribute.ClientTypes)} overlap. " +
            string.Join(" | ", collisions));
    }

    private static MethodInfo ResolveAddClientMethod()
    {
        const string methodName = nameof(Application.Core.ApiClient.Extensions.DependencyInjectionExtensions.AddClient);
        var method = typeof(Application.Core.ApiClient.Extensions.DependencyInjectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(m =>
            {
                if (m.Name != methodName || !m.IsGenericMethodDefinition)
                {
                    return false;
                }

                var parameters = m.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType == typeof(IServiceCollection)
                       && parameters[1].ParameterType == typeof(IConfiguration);
            });

        return method ?? throw new InvalidOperationException(
            $"Required API client registration method " +
            $"'{methodName}(IServiceCollection, IConfiguration)' was not found.");
    }
}
