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
            .FirstOrDefault()?.Description ??
        Enum.GetName(value.GetType(), value)!;

    /// <summary>
    /// Получить элемент enum по его описанию из <see cref="DescriptionAttribute"/>
    /// </summary>
    /// <param name="description">Описание.</param>
    /// <param name="enumType">Тип enum.</param>
    /// <returns>Элемент enum.</returns>
    public static Enum? GetEnumValueByDescription(this string description, Type enumType)
    {
        ArgumentNullException.ThrowIfNull(description);

        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .FirstOrDefault(v => v.Description() == description);
    }

    /// <summary>
    /// Получить элементы enum по их частичному описанию из <see cref="DescriptionAttribute"/>
    /// </summary>
    /// <param name="description">Описание.</param>
    /// <param name="enumType">Тип enum.</param>
    /// <returns>Элемент enum.</returns>
    public static IEnumerable<Enum> GetEnumValueByPartOfDescription(this string description, Type enumType)
    {
        ArgumentNullException.ThrowIfNull(description);

        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .Where(v => v.Description().ToLower().Contains(description.ToLower()));
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
        flags.Combine(flagsToAdd, (value1, value2) => value1 ^ value2);

    private static Enum Combine(this Enum flags, Enum flagsToAdd, Func<long, long, long> combineFunc)
    {
        var type = flags.GetType();
        var addValueType = flagsToAdd.GetType();

        if (!type.IsEnum)
        {
            throw new ArgumentException($"Тип {type.Name} должен наследовать {nameof(Enum)}.");
        }

        if (type != addValueType)
        {
            throw new ArgumentException(
                $"Невозможно объединить значения {nameof(Enum)} разных тпиов " +
                $"(типы '{type.Name}' и '{addValueType.Name}').");
        }

        return (Enum)Enum.ToObject(
            type,
            Convert.ChangeType(
                combineFunc(Convert.ToInt64(flags), Convert.ToInt64(flagsToAdd)),
                Enum.GetUnderlyingType(type)));
    }
}
