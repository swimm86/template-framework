// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdHeaderDelegatingHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Handlers.Attributes;
using Shared.Application.Core.CorrelationId;
using Shared.Application.Core.CorrelationId.Extensions;

namespace Shared.Infrastructure.Core.ApiClient.Handlers;

/// <summary>
/// Добавляет идентификатор корреляции в исходящие HTTP-запросы, если он не задан.
/// </summary>
[ApiClientDelegatingHandleMetadata(order: 100)]
public sealed class CorrelationIdHeaderDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CorrelationIdHeaderDelegatingHandler> logger)
    : DelegatingHandler
{
    /// <summary>
    /// Устанавливает внутренний обработчик для целей тестирования.
    /// Использует рефлексию для доступа к <see cref="DelegatingHandler.InnerHandler"/>,
    /// который является protected.
    /// Доступен только сборке тестов через <see cref="InternalsVisibleToAttribute"/>.
    /// </summary>
    /// <param name="innerHandler">Обработчик для проксирования запросов.</param>
    internal void SetInnerHandlerForTesting(HttpMessageHandler innerHandler)
    {
        typeof(DelegatingHandler)
            .GetProperty(nameof(InnerHandler), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, innerHandler);
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Contains(Constants.CorrelationIdHeader))
        {
            return base.SendAsync(request, cancellationToken);
        }

        var correlationId =
            httpContextAccessor.HttpContext?.Request.GetCorrelationId() ??
            JobCorrelationContext.GetCorrelationId();
        if (correlationId.HasValue)
        {
            request.Headers.Add(
                Constants.CorrelationIdHeader,
                correlationId.Value.ToString("D"));
        }
        else
        {
            logger.LogError(
                "Correlation id not found for request '{Url}'",
                request.RequestUri?.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
