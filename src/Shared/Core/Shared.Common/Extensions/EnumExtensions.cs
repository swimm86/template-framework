// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Reflection;

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="Enum"/>.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Получить значения атрибута <see cref="DescriptionAttribute"/> из значения <see cref="Enum"/>.
    /// </summary>
    /// <param name="value">Значение <see cref="Enum"/>, для которого необходимо получить описание.</param>
    /// <returns>Значение атрибута <see cref="DescriptionAttribute"/>.</returns>
    public static string Description(this Enum value) =>
        value.GetType().GetField(value.ToString())!.GetCustomAttributes<DescriptionAttribute>(false)
            .FirstOrDefault()?.Description ?? string.Empty;

    /// <summary>
    /// Возвращает значение enum типа <see cref="enumType"/> по его описанию,
    /// указанному в атрибуте <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <param name="description">Описание значения enum.</param>
    /// <param name="enumType">Тип enum.</param>
    /// <param name="emptyValue">Значение по умолчанию. Если указано, то в случае не нахождения нужного значения, будет возвращено.</param>
    /// <param name="contextSearch">Признак того, что необходимо осуществить поиск по частичному совпадению.</param>
    /// <returns>Значение enum типа <see cref="enumType"/>.</returns>
    public static Enum GetEnumValueByDescription(
        this string? description,
        Type enumType,
        Enum? emptyValue = null,
        bool contextSearch = false)
    {
        return GetEnumValueByDescription(
            description,
            emptyValue,
            enumType,
            Enum.GetValues(enumType).Cast<Enum?>(),
            contextSearch);
    }

    /// <summary>
    /// Возвращает значение перечисления типа <typeparamref name="TEnum"/> по его описанию,
    /// указанному в атрибуте <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <typeparam name="TEnum">Тип перечисления.</typeparam>
    /// <param name="emptyValue">Значение по умолчанию. Если указано, то в случае не нахождения нужного значения, будет возвращено.</param>
    /// <returns>Значение enum типа <typeparamref name="TEnum"/>.</returns>
    /// <inheritdoc cref="GetEnumValueByDescription(string?, Type, Enum?, bool)"/>
    /// <param name="description"/><param name="contextSearch"/>
    public static TEnum? GetEnumValueByDescription<TEnum>(
        this string? description,
        TEnum? emptyValue = null,
        bool contextSearch = false)
        where TEnum : struct, Enum
    {
        return GetEnumValueByDescription(
            description,
            emptyValue,
            typeof(TEnum),
            Enum.GetValues<TEnum>().OfType<Enum?>(),
            contextSearch) as TEnum?;
    }

    /// <summary>
    /// Объединяет флаги между собой.
    /// </summary>
    /// <param name="flags">Флаги.</param>
    /// <param name="flagsToAdd">Флаги для добавления.</param>
    /// <returns>Объединенные флаги.</returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если типы не совпадают между собой или не наследуют <see cref="Enum"/>.
    /// </exception>
    public static Enum With(this Enum flags, Enum flagsToAdd) =>
        flags.Combine(flagsToAdd, (value1, value2) => value1 | value2);

    /// <summary>
    /// Исключает флаги из других флагов.
    /// </summary>
    /// <param name="flags">Флаги.</param>
    /// <param name="flagsToAdd">Флаги для исключения.</param>
    /// <returns>Флаги после исключения.</returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если типы не совпадают между собой или не наследуют <see cref="Enum"/>.
    /// </exception>
    public static Enum Without(this Enum flags, Enum flagsToAdd) =>
        flags.Combine(flagsToAdd, (value1, value2) => value1 & ~value2);

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
        IEnumerable<Enum?> enumValues,
        bool contextSearch = false)
    {
        if (string.IsNullOrWhiteSpace(description?.Trim()))
        {
            return
                 emptyValue ??
                throw new ArgumentException($"'{nameof(description)}' must not be empty or whitespace.");
        }

        var result = enumValues.FirstOrDefault(x =>
            contextSearch
                ? x?.Description().Contains(description, StringComparison.OrdinalIgnoreCase) == true
                : string.Equals(x?.Description(), description, StringComparison.OrdinalIgnoreCase));
        return
            result ??
            emptyValue ??
            throw new ArgumentException(
                $"Enum value of type '{enumType.Name}' with '{nameof(DescriptionAttribute)}'='{description}' was not found.");
    }
}
