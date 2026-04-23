// ----------------------------------------------------------------------------------------------
// <copyright file="JobCorrelationIdLayoutRenderer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text;
using NLog;
using NLog.LayoutRenderers;
using Shared.Application.Core.CorrelationId;

namespace Shared.Infrastructure.Logging.LayoutRenderers;

/// <summary>
/// Layout renderer для вывода идентификатора корреляции фоновых задач из JobCorrelationContext.
/// </summary>
[LayoutRenderer(Constants.JobCorrelationIdScopePropertyKey)]
public class JobCorrelationIdLayoutRenderer
    : LayoutRenderer
{
    /// <inheritdoc />
    protected override void Append(
        StringBuilder builder,
        LogEventInfo logEvent)
    {
        var correlationId = JobCorrelationContext.GetCorrelationId();

        if (correlationId.HasValue)
        {
            builder.Append(correlationId.Value.ToString());
        }
    }
}
