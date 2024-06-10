// ----------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Extensions;

/// <summary>
/// Расширение для <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Конвертирует строку в формат kebab-case.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <returns>Преобразованная в формат kebab-case строка.</returns>
    public static string ToKebabCase(this string value)
    {
        return value.ToLowerCaseWithDelimiter("-");
    }

    /// <summary>
    /// Конвертирует строку в формат snake_case.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <returns>Преобразованная в формат snake_case строка.</returns>
    public static string ConvertToSnakeCase(this string value)
    {
        return value.ToLowerCaseWithDelimiter("_");
    }

    /// <summary>
    /// Конвертирует строку в слова с маленькой буквы, разделенные разделителем <paramref name="delimiter"/>.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <param name="delimiter">Разделитель.</param>
    /// <returns>Преобразованная строка.</returns>
    private static string ToLowerCaseWithDelimiter(this string value, string delimiter)
    {
        return string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? delimiter + x : x.ToString())).ToLower();
    }
}
