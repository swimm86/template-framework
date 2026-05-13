// ----------------------------------------------------------------------------------------------
// <copyright file="HttpCorrelationIdLayoutRenderer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using NLog;
using NLog.LayoutRenderers;
using NLog.Web.LayoutRenderers;
using Shared.Application.Core.CorrelationId.Extensions;

namespace Shared.Infrastructure.Logging.LayoutRenderers;

/// <summary>
/// Layout renderer для вывода идентификатора корреляции HTTP запросов из HttpContext.
/// </summary>
[LayoutRenderer(Constants.HttpCorrelationIdScopePropertyKey)]
public class HttpCorrelationIdLayoutRenderer
    : AspNetLayoutRendererBase
{
    /// <inheritdoc />
    protected override void Append(
        StringBuilder builder,
        LogEventInfo logEvent)
    {
        var httpContext = HttpContextAccessor?.HttpContext;
        var correlationId = httpContext?.Request.GetCorrelationId();
        if (correlationId.HasValue)
        {
            builder.Append(correlationId.Value.ToString());
        }
    }
}
