// ----------------------------------------------------------------------------------------------
// <copyright file="LoggingConfigurationExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Shared.Infrastructure.Logging.LayoutRenderers;

namespace Shared.Infrastructure.Logging.Extensions;

/// <summary>
/// Расширения для <see cref="LoggingConfiguration"/>.
/// </summary>
internal static class LoggingConfigurationExtensions
{
    private const string MessagePattern = "msg=${message";
    private const string HttpCorrelationIdLayout = $"${{{Constants.HttpCorrelationIdScopePropertyKey}}}";
    private const string JobCorrelationIdLayout = $"${{{Constants.JobCorrelationIdScopePropertyKey}}}";
    private const string CorrelationIdBlock =
        $"${{when:when='{HttpCorrelationIdLayout}'!=''" +
        $":inner= corId={HttpCorrelationIdLayout}}}" +
        $"${{when:when='{JobCorrelationIdLayout}'!=''" +
        $":inner= corId={JobCorrelationIdLayout}}}";

    private static readonly HashSet<string> ExcludedTargetNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "coloredSystemEventConsole",
        "coloredBusinessEventConsole"
    };

    private static readonly object LockObject = new();
    private static volatile bool _renderersRegistered;

    /// <summary>
    /// Добавляет идентификатор корреляции в layout таргетов конфигурации NLog.
    /// </summary>
    /// <param name="nlogConfig">Конфигурация NLog.</param>
    public static void AddCorrelationIdToTargetLayouts(this LoggingConfiguration nlogConfig)
    {
        RegisterLayoutRenderers();

        foreach (var target in nlogConfig.AllTargets)
        {
            if (ExcludedTargetNames.Contains(target.Name))
            {
                continue;
            }

            if (target is not TargetWithLayout { Layout: SimpleLayout simpleLayout } targetWithLayout)
            {
                continue;
            }

            var currentLayout = simpleLayout.Text;
            if (!currentLayout.Contains(HttpCorrelationIdLayout, StringComparison.OrdinalIgnoreCase) &&
                !currentLayout.Contains(JobCorrelationIdLayout, StringComparison.OrdinalIgnoreCase))
            {
                targetWithLayout.Layout = InsertCorrelationIdIntoLayout(currentLayout);
            }
        }
    }

    /// <summary>
    /// Регистрирует layout renderer-ы для идентификатора корреляции.
    /// Регистрирует два renderer-а с разными ключами:
    /// 1. HttpCorrelationIdLayoutRenderer - для HTTP запросов
    /// 2. JobCorrelationIdLayoutRenderer - для фоновых задач
    /// </summary>
    private static void RegisterLayoutRenderers()
    {
        lock (LockObject)
        {
            if (_renderersRegistered)
            {
                return;
            }

            LogManager.Setup().SetupExtensions(ext =>
            {
                ext.RegisterLayoutRenderer(
                    Constants.HttpCorrelationIdScopePropertyKey,
                    typeof(HttpCorrelationIdLayoutRenderer));
                ext.RegisterLayoutRenderer(
                    Constants.JobCorrelationIdScopePropertyKey,
                    typeof(JobCorrelationIdLayoutRenderer));
            });

            _renderersRegistered = true;
        }
    }

    private static string InsertCorrelationIdIntoLayout(string currentLayout)
    {
        var insertPosition = FindInsertPosition(currentLayout);

        if (insertPosition >= 0)
        {
            return currentLayout.Insert(insertPosition, CorrelationIdBlock);
        }

        return currentLayout + CorrelationIdBlock;
    }

    private static int FindInsertPosition(string layout)
    {
        var patternIndex = layout.IndexOf(MessagePattern, StringComparison.OrdinalIgnoreCase);

        if (patternIndex < 0)
        {
            return -1;
        }

        var searchStart = patternIndex + MessagePattern.Length;
        var closingBraceIndex = layout.IndexOf('}', searchStart);

        return closingBraceIndex >= 0 ? closingBraceIndex + 1 : -1;
    }
}
