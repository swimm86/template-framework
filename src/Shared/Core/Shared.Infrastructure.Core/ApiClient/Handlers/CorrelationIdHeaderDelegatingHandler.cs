// ----------------------------------------------------------------------------------------------
// <copyright file="CorrelationIdHeaderDelegatingHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

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
