// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleActionValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using System.Linq.Expressions;
using Shared.Application.Core.LifecycleAction.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Общая логика валидации коллекции <see cref="ILifecycleActionHandler"/>.
/// </summary>
internal static class LifecycleActionValidator
{
    /// <summary>
    /// Выполняет полную валидацию коллекции обработчиков.
    /// </summary>
    /// <param name="handlers">Коллекция обработчиков для проверки.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="handlers"/> равен <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если в коллекции обнаружены дубли по составному ключу
    /// <c>(<see cref="ILifecycleActionHandler.EntityType"/>,
    /// <see cref="ILifecycleActionHandler.Phase"/>, <see cref="ILifecycleActionHandler.Key"/>)</c>
    /// либо конфликты <see cref="ILifecycleActionHandler.Order"/> в пределах одного
    /// <c>(<see cref="ILifecycleActionHandler.EntityType"/>, <see cref="ILifecycleActionHandler.Phase"/>)</c>.
    /// </exception>
    public static void Validate(IEnumerable<ILifecycleActionHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        var materialised = handlers as IReadOnlyList<ILifecycleActionHandler> ?? handlers.ToArray();
        var keyError = "Each (EntityType, Phase, Key) combination must appear in the DI container exactly once; "
            + "otherwise both handlers will be invoked and produce duplicate side effects.";
        var orderError = "Each (EntityType, Phase) combination requires unique Order values; "
            + "otherwise DispatchAsync would produce a non-deterministic execution order.";

        EnsureUnique(materialised, h => h.Key, keyError);
        EnsureUnique(materialised, h => h.Order, orderError);
    }

    private static void EnsureUnique(
        IReadOnlyList<ILifecycleActionHandler> handlers,
        Expression<Func<ILifecycleActionHandler, object>> keySelector,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(errorMessage);

        var keySelectors = new[]
        {
            h => h.EntityType,
            h => h.Phase,
            keySelector,
        };

        var propertyNames = keySelectors
            .Select(ExtractPropertyName)
            .ToArray();

        var compiledSelectors = keySelectors
            .Select(s => s.Compile())
            .ToArray();

        var conflictGroups = handlers
            .GroupBy(
                h => compiledSelectors.Select(sel => sel(h)).ToArray(),
                ArrayEqualityComparer.Instance)
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                Values = g.First().ApplyTo(compiledSelectors),
                Handlers = g.ToArray(),
            })
            .OrderBy(g => FormatValue(g.Values[0]), StringComparer.Ordinal)
            .ToArray();

        if (conflictGroups.Length == 0)
        {
            return;
        }

        var conflictLines = conflictGroups
            .Select(g => FormatConflict(propertyNames, g.Values, g.Handlers));

        throw new InvalidOperationException(
            $"Duplicate lifecycle action handlers detected by the combination " +
            $"({string.Join(", ", propertyNames)}).{Environment.NewLine}" +
            $"{errorMessage}{Environment.NewLine}" +
            $"Conflicts:{Environment.NewLine}  " + string.Join($"{Environment.NewLine}  ", conflictLines));
    }

    /// <summary>
    /// Применяет массив скомпилированных селекторов к обработчику.
    /// </summary>
    private static object?[] ApplyTo(
        this ILifecycleActionHandler handler,
        Func<ILifecycleActionHandler, object>[] compiledSelectors)
    {
        var values = new object?[compiledSelectors.Length];
        for (var i = 0; i < compiledSelectors.Length; i++)
        {
            values[i] = compiledSelectors[i](handler);
        }

        return values;
    }

    /// <summary>
    /// Извлекает имя свойства из селектора. Поддерживает только простой
    /// доступ к свойству (<c>h =&gt; h.Foo</c>); для value-типов
    /// оборачивается в <c>Convert</c>, который тоже разворачивается.
    /// </summary>
    private static string ExtractPropertyName(
        Expression<Func<ILifecycleActionHandler, object>> selector)
    {
        var body = selector.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException(
            $"Selector '{selector}' is not a simple property access.",
            nameof(selector));
    }

    /// <summary>
    /// Форматирует конфликт: сначала значения составного ключа, затем
    /// список handler-ов, попавших в эту группу, с их <c>Key</c> и типом.
    /// </summary>
    /// <param name="propertyNames">Имена свойств составного ключа.</param>
    /// <param name="values">Значения составного ключа для конфликтной группы.</param>
    /// <param name="handlers">handler-ы, попавшие в конфликтную группу.</param>
    /// <returns>Многострочное описание конфликта.</returns>
    private static string FormatConflict(
        string[] propertyNames,
        object?[] values,
        ILifecycleActionHandler[] handlers)
    {
        var keyPart = string.Join(
            ", ",
            Enumerable.Range(0, propertyNames.Length)
                .Select(i => $"{propertyNames[i]}: '{FormatValue(values[i])}'"));

        var handlerList = string.Join(
            $";{Environment.NewLine}",
            handlers
                .OrderBy(h => h.Key, StringComparer.Ordinal)
                .Select(h => $"[Key='{h.Key}', Type={FormatValue(h.GetType())}]"));

        return $"{keyPart}{Environment.NewLine}Conflicting handlers: {handlerList}";
    }

    /// <summary>
    /// Приводит значение свойства к стабильной строке для сообщений об ошибке.
    /// Для <see cref="Type"/> использует <see cref="Type.FullName"/>.
    /// </summary>
    private static string FormatValue(object? value) => value switch
    {
        null => "<null>",
        Type type => type.FullName ?? type.Name,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "<unknown>",
    };

    /// <summary>
    /// Сравнивает массивы объектов по содержимому, поэлементно.
    /// Используется как <see cref="IEqualityComparer{T}"/> для <c>GroupBy</c>
    /// при работе с ключами, представленными массивами.
    /// </summary>
    private sealed class ArrayEqualityComparer
        : IEqualityComparer<object?[]>
    {
        public static readonly ArrayEqualityComparer Instance = new();

        public bool Equals(object?[]? x, object?[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (!Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(object?[] obj)
        {
            var hash = default(HashCode);
            foreach (var value in obj)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }
    }
}
