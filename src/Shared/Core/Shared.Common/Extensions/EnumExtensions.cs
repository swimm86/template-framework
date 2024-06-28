// ----------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
}
