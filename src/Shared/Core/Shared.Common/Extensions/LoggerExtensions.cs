// ----------------------------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    public static async Task<T> LogTaskAsync<T>(this ILogger? logger,
        Func<Task<T>> action, 
        CancellationToken token,
        [CallerMemberName] string? methodName = null)
    {
        try
        {
            logger?.LogInformation("'{methodName}' start.", methodName);
            var result = await action().WaitAsync(token).ConfigureAwait(false);
            logger?.LogInformation("'{methodName}' end.", methodName);
            return result;
        }
        catch
        {
            logger?.LogError("'{methodName}' failed", methodName);
            throw;
        }
    }

    public static Task LogTaskAsync(this ILogger? logger, 
        Func<Task> action, 
        CancellationToken token,
        [CallerMemberName] string? methodName = null)
    {
        return LogTaskAsync(logger, async () =>
        {
            await action().WaitAsync(token).ConfigureAwait(false);
            return Task.CompletedTask;
        }, token, methodName);
    }

    public static T LogTask<T>(this ILogger? logger, 
        Func<T> action,
        [CallerMemberName] string? methodName = null)
    {
        return LogTaskAsync(logger, () => Task.FromResult(action()), default, methodName)
            .GetAwaiter()
            .GetResult();
    }

    public static void LogTask(this ILogger? logger, 
        Action action,
        [CallerMemberName] string? methodName = null)
    {
        LogTask<object?>(logger, () =>
        {
            action();
            return default;
        }, methodName);
    }
}
