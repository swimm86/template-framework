using Microsoft.Extensions.Configuration;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Common.Helpers;
using Shared.Presentation.Core.Exceptions.Settings;

namespace Shared.Presentation.Core.Tests.Infrastructure;

/// <summary>
/// Выдача <see cref="IConfiguration"/> для тестов мапперов исключений.
/// </summary>
/// <remarks>
/// <para>
/// Метод <c>GetOptions&lt;T&gt;</c> ищет секции по имени модуля
/// (<c>AssemblyHelper.GetModuleName()</c>), поэтому ключи конфигурации
/// должны быть с префиксом корневого имени модуля.
/// </para>
/// <para>
/// <c>Empty()</c> возвращает пустую конфигурацию — <c>GetOptions</c> вернёт
/// <c>null</c>, и применятся дефолтные значения <see cref="ExceptionMapperSettings"/>.
/// </para>
/// </remarks>
internal static class TestConfigurationBuilder
{
    /// <summary>
    /// Возвращает пустую конфигурацию. Используется для тестов с дефолтными
    /// значениями <see cref="ExceptionMapperSettings"/>.
    /// </summary>
    public static IConfiguration Empty() => new ConfigurationBuilder().Build();

    /// <summary>
    /// Возвращает конфигурацию с явно заданными настройками
    /// <see cref="ExceptionMapperSettings"/>. Корневая секция совпадает с именем
    /// текущего модуля (assembly name теста), что позволяет
    /// <c>GetOptions&lt;T&gt;</c> найти нужные значения.
    /// </summary>
    public static IConfiguration WithSettings(
        bool? shouldEnrichWithTrace = null,
        int? stackTraceDepth = null,
        int? maxExceptionDepth = null)
    {
        var moduleName = AssemblyHelper.GetModuleName();
        var rootKey = moduleName.Split('.').First();
        var settingsKey = nameof(ExceptionMapperSettings);
        var pairs = new Dictionary<string, string?>();

        if (shouldEnrichWithTrace is { } b)
        {
            pairs[$"{rootKey}:{settingsKey}:{nameof(ExceptionMapperSettings.ShouldEnrichWithTrace)}"]
                = b.ToString();
        }

        if (stackTraceDepth is { } s)
        {
            pairs[$"{rootKey}:{settingsKey}:{nameof(ExceptionMapperSettings.StackTraceDepth)}"]
                = s.ToString();
        }

        if (maxExceptionDepth is { } m)
        {
            pairs[$"{rootKey}:{settingsKey}:{nameof(ExceptionMapperSettings.MaxExceptionDepth)}"]
                = m.ToString();
        }

        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
