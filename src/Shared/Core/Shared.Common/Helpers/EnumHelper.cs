// ----------------------------------------------------------------------------------------------
// <copyright file="EnumHelper.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.ComponentModel;
using Shared.Common.Extensions;

namespace Shared.Common.Helpers;

/// <summary>
/// Предоставляет вспомогательные методы для <see cref="Enum"/>.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Получить значение перечислимого типа <typeparamref name="TEnum"/> по его описанию,
    /// указанному в атрибуте <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <param name="description">Описание значения перечислимого типа.</param>
    /// <param name="emptyValue">Значение по умолчанию. Если указано, то в случае не нахождения нужного значения, будет возвращено.</param>
    /// <typeparam name="TEnum">Тип перечислимого значения.</typeparam>
    /// <returns>Значение перечислимого типа <typeparamref name="TEnum"/>.</returns>
    public static TEnum GetEnumByDescription<TEnum>(
        string? description,
        TEnum? emptyValue = default)
        where TEnum : Enum
    {
        if (string.IsNullOrEmpty(description))
        {
            if (emptyValue is not null)
            {
                return emptyValue;
            }

            throw new ArgumentException($"'{nameof(DescriptionAttribute)}' не должен быть пустым");
        }

        var result = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().FirstOrDefault(x =>
            string.Equals(x.Description(), description, StringComparison.OrdinalIgnoreCase));
        if (result == null)
        {
            if (emptyValue is not null)
            {
                return emptyValue;
            }

            throw new ArgumentException(
                $"Значение перечисления '{typeof(TEnum).Name}', " +
                $"у которого '{nameof(DescriptionAttribute)}'='{description}' не найдено");
        }

        return result;
    }
}
