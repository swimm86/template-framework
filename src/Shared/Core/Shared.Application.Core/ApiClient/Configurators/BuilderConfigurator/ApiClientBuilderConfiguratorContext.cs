// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientBuilderConfiguratorContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator.Interfaces;
using Shared.Common.Helpers;

namespace Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator;

/// <summary>
/// Хранит карту соответствия <see cref="ApiClient"/> -> <see cref="IApiClientBuilderConfigurator"/>.
/// </summary>
/// <remarks>
/// Карта заполняется один раз методом <see cref="InitializeApiClientBuilderConfiguratorsMap"/>
/// в рамках регистрации инфраструктурных зависимостей.
/// Конфигураторы создаются через <see cref="Activator.CreateInstance(Type)"/>, поэтому
/// реализации <see cref="IApiClientBuilderConfigurator"/> должны иметь публичный конструктор без параметров.
/// </remarks>
public static class ApiClientBuilderConfiguratorContext
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, IApiClientBuilderConfigurator?> ApiClientBuilderConfiguratorsMap = new();
    private static volatile bool _isInitialized;

    /// <summary>
    /// Инициализирует карту конфигураторов для всех найденных типов API-клиентов.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если для одного API-клиента найдено более одного применимого конфигуратора
    /// (специализированного или общего).
    /// Также выбрасывается при обращении к контексту до выполнения инициализации.
    /// </exception>
    public static void InitializeApiClientBuilderConfiguratorsMap()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_isInitialized)
            {
                return;
            }

            var allConfigurators = AssemblyHelper.GetDerivedTypesFromAssemblies<IApiClientBuilderConfigurator>()
                .Select(Activator.CreateInstance)
                .OfType<IApiClientBuilderConfigurator>()
                .ToArray();
            if (!allConfigurators.Any())
            {
                _isInitialized = true;
                return;
            }

            var apiClientsTypes = AssemblyHelper.GetDerivedTypesFromAssemblies<ApiClient>().ToArray();
            var configuratorsMap = apiClientsTypes
                .Select(apiClientType =>
                {
                    var kind = ApiClientBuilderConfiguratorKind.Specialized;
                    var configurators = allConfigurators
                        .Where(c => c.ApiClientTypes.Contains(apiClientType))
                        .ToArray();

                    if (configurators.Length == 0)
                    {
                        configurators = allConfigurators
                            .Where(c =>
                                c.ApiClientTypes.Count == 0 &&
                                !c.ExcludedApiClientTypes.Contains(apiClientType))
                            .ToArray();
                        kind = ApiClientBuilderConfiguratorKind.Common;
                    }

                    return new ApiClientConfiguratorMatch(apiClientType, configurators, kind);
                })
                .ToArray();

            var invalidData = configuratorsMap
                .Where(x => x.Configurators.Length > 1)
                .Select(x =>
                    $"{x.ApiClientType.Name} ({x.Kind}):" +
                    $"{string.Join(", ", x.Configurators.Select(configurator => configurator.GetType().FullName))}")
                .ToArray();
            if (invalidData.Any())
            {
                throw new InvalidOperationException(
                    $"Multiple {nameof(IApiClientBuilderConfigurator)} were found.{Environment.NewLine}" +
                    string.Join(Environment.NewLine, invalidData));
            }

            ApiClientBuilderConfiguratorsMap.Clear();
            foreach (var item in configuratorsMap)
            {
                ApiClientBuilderConfiguratorsMap.Add(item.ApiClientType, item.Configurators.SingleOrDefault());
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Возвращает конфигуратор, применимый к указанному типу API-клиента.
    /// </summary>
    /// <param name="apiClientType">Тип API-клиента.</param>
    /// <returns>
    /// Экземпляр <see cref="IApiClientBuilderConfigurator"/>, если конфигуратор найден; иначе <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если контекст не инициализирован методом
    /// <see cref="InitializeApiClientBuilderConfiguratorsMap"/>.
    /// </exception>
    internal static IApiClientBuilderConfigurator? GetBuilderConfigurator(Type apiClientType)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                $"{nameof(ApiClientBuilderConfiguratorContext)} is not initialized. " +
                $"Call {nameof(InitializeApiClientBuilderConfiguratorsMap)} during infrastructure dependency registration.");
        }

        return ApiClientBuilderConfiguratorsMap.GetValueOrDefault(apiClientType);
    }

    private readonly record struct ApiClientConfiguratorMatch(
        Type ApiClientType,
        IApiClientBuilderConfigurator[] Configurators,
        ApiClientBuilderConfiguratorKind Kind);

    /// <summary>
    /// Вид конфигуратора <see cref="Interfaces.IApiClientBuilderConfigurator"/>.
    /// </summary>
    private enum ApiClientBuilderConfiguratorKind
    {
        /// <summary>
        /// Общий конфигуратор, применимый ко всем API-клиентам, кроме исключённых.
        /// </summary>
        Common = 0,

        /// <summary>
        /// Специализированный конфигуратор, применимый только к явно указанным типам API-клиентов.
        /// </summary>
        Specialized = 1,
    }
}
