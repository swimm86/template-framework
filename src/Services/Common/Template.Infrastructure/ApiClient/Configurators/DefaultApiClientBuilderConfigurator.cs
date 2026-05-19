// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultApiClientBuilderConfigurator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.ApiClient.Configurators.BuilderConfigurator.Interfaces;

namespace Template.Infrastructure.ApiClient.Configurators;

/// <summary>
/// Конфигуратор <see cref="IHttpClientBuilder"/> для API-клиентов по умолчанию.
/// </summary>
public class DefaultApiClientBuilderConfigurator
    : IApiClientBuilderConfigurator
{
    /// <inheritdoc />
    public IReadOnlyCollection<Type> ApiClientTypes => [];

    /// <inheritdoc />
    public IReadOnlyCollection<Type> ExcludedApiClientTypes => [];

    /// <inheritdoc />
    public void Configure(IHttpClientBuilder builder)
    {
        // Реализация post build action-а при создании API-клиентов.
    }
}