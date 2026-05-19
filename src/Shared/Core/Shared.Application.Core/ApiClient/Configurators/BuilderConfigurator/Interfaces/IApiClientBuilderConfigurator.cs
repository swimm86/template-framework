// ----------------------------------------------------------------------------------------------
// <copyright file="IApiClientBuilderConfigurator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator.Interfaces;

/// <summary>
/// Задает конфигурацию <see cref="IHttpClientBuilder"/> для API-клиентов.
/// </summary>
/// <remarks>
/// <para>
/// Реализация должна быть stateless и не зависеть от runtime-scoped сервисов.
/// </para>
/// <para>
/// Для общего конфигуратора свойство <see cref="ApiClientTypes"/> должно возвращать пустую коллекцию.
/// Такой конфигуратор применяется ко всем API-клиентам, кроме типов из <see cref="ExcludedApiClientTypes"/>,
/// если для клиента не найден специализированный конфигуратор.
/// </para>
/// </remarks>
public interface IApiClientBuilderConfigurator
{
    /// <summary>
    /// Целевые типы API-клиентов.
    /// </summary>
    IReadOnlyCollection<Type> ApiClientTypes { get; }

    /// <summary>
    /// Типы API-клиентов, исключенные из применения конфигуратора.
    /// Используется только для общих конфигураторов.
    /// </summary>
    IReadOnlyCollection<Type> ExcludedApiClientTypes { get; }

    /// <summary>
    /// Применяет конфигурацию к <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <param name="builder">Билдер HTTP-клиента.</param>
    void Configure(IHttpClientBuilder builder);
}
