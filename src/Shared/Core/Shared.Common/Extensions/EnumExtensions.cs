// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Reflection;

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="Enum"/>: получение описаний, преобразование по описанию, работа с флагами.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Возвращает значение атрибута <see cref="DescriptionAttribute"/> для элемента перечисления.
    /// </summary>
    /// <param name="value">Значение <see cref="Enum"/>, для которого необходимо получить описание.</param>
    /// <returns>Значение атрибута <see cref="DescriptionAttribute"/>.</returns>
    public static string Description(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttributes<DescriptionAttribute>(false)
            .FirstOrDefault()?.Description ?? string.Empty;
    }

    /// <summary>
    /// Возвращает значение перечисления по его описанию,
    /// указанному в атрибуте <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <param name="description">Описание значения перечисления.</param>
    /// <param name="enumType">Тип перечисления.</param>
    /// <param name="emptyValue">
    /// Значение по умолчанию. Возвращается, если элемент с указанным описанием не найден.
    /// </param>
    /// <returns>Значение перечисления типа <paramref name="enumType"/>.</returns>
    public static Enum GetEnumValueByDescription(
        this string? description,
        Type enumType,
        Enum? emptyValue = null)
    {
        return GetEnumValueByDescription(
            description,
            emptyValue,
            enumType,
            Enum.GetValues(enumType).Cast<Enum?>());
    }

    /// <summary>
    /// Возвращает значение перечисления типа <typeparamref name="TEnum"/> по его описанию,
    /// указанному в атрибуте <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <typeparam name="TEnum">Тип перечисления.</typeparam>
    /// <param name="emptyValue">
    /// Значение по умолчанию. Возвращается, если элемент с указанным описанием не найден.
    /// </param>
    /// <returns>Значение перечисления типа <typeparamref name="TEnum"/>.</returns>
    /// <inheritdoc cref="GetEnumValueByDescription(string?, Type, Enum?)"/>
    /// <param name="description"/>
    public static TEnum? GetEnumValueByDescription<TEnum>(
        this string? description,
        TEnum? emptyValue = null)
        where TEnum : struct, Enum
    {
        return GetEnumValueByDescription(
            description,
            emptyValue,
            typeof(TEnum),
            Enum.GetValues<TEnum>().OfType<Enum?>()) as TEnum?;
    }

    /// <summary>
    /// Возвращает элементы перечисления, описание которых содержит указанную подстроку.
    /// </summary>
    /// <param name="description">Подстрока для поиска в описаниях элементов.</param>
    /// <param name="enumType">Тип перечисления.</param>
    /// <returns>Коллекция элементов перечисления, содержащих подстроку в описании.</returns>
    public static IEnumerable<Enum> GetEnumValueByPartOfDescription(
        this string? description,
        Type enumType)
    {
        ArgumentNullException.ThrowIfNull(description);

        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .Where(v => v.Description().Contains(description, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Объединяет флаги перечисления (побитовое ИЛИ).
    /// </summary>
    /// <param name="flags">Исходные флаги.</param>
    /// <param name="flagsToAdd">Флаги для добавления.</param>
    /// <returns>Результат объединения флагов.</returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если типы не совпадают между собой или не наследуют <see cref="Enum"/>.
    /// </exception>
    public static Enum With(this Enum flags, Enum flagsToAdd) =>
        flags.Combine(flagsToAdd, (value1, value2) => value1 | value2);

    /// <summary>
    /// Исключает указанные флаги из набора (побитовое И НЕ).
    /// </summary>
    /// <param name="flags">Исходные флаги.</param>
    /// <param name="flagsToRemove">Флаги для исключения.</param>
    /// <returns>Результат исключения флагов.</returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если типы не совпадают между собой или не наследуют <see cref="Enum"/>.
    /// </exception>
    public static Enum Without(this Enum flags, Enum flagsToRemove) =>
        flags.Combine(flagsToRemove, (value1, value2) => value1 & ~value2);

    private static Enum Combine(this Enum flags, Enum flagsToAdd, Func<long, long, long> combineFunc)
    {
        var type = flags.GetType();
        var addValueType = flagsToAdd.GetType();

        if (!type.IsEnum)
        {
            throw new ArgumentException($"Type '{type.Name}' must be an enumeration type.");
        }

        if (type != addValueType)
        {
            throw new ArgumentException(
                $"Cannot combine {nameof(Enum)} values of different types " +
                $"(types '{type.Name}' and '{addValueType.Name}').");
        }

        return (Enum)Enum.ToObject(
            type,
            Convert.ChangeType(
                combineFunc(Convert.ToInt64(flags), Convert.ToInt64(flagsToAdd)),
                Enum.GetUnderlyingType(type)));
    }

    private static Enum GetEnumValueByDescription(
        string? description,
        Enum? emptyValue,
        Type enumType,
        IEnumerable<Enum?> enumValues)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return
                 emptyValue ??
                throw new ArgumentException($"'{nameof(description)}' must not be empty or whitespace.");
        }

        var result = enumValues.FirstOrDefault(x =>
            string.Equals(x?.Description(), description, StringComparison.OrdinalIgnoreCase));
        return
            result ??
            emptyValue ??
            throw new ArgumentException(
                $"Enum value of type '{enumType.Name}' with '{nameof(DescriptionAttribute)}'='{description}' was not found.");
    }
}
