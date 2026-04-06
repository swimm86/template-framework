// ----------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;

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
    public static string ToSnakeCase(this string value)
    {
        return value.ToLowerCaseWithDelimiter("_");
    }

    /// <summary>
    /// Преобразовывает строку в camelCase.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Строка преобразованная в camelCase.</returns>
    public static string ToCamelCase(this string input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? input
            : JsonNamingPolicy.CamelCase.ConvertName(input);
    }

    /// <summary>
    /// Конвертирует строку в строку, которая нчинается с заглавной буквы.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Строка с заглавной буквы.</returns>
    public static string ToUpperFirstChar(this string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            : input.Length > 1
                ? char.ToUpper(input[0]) + input[1..]
                : input.ToUpper();
    }

    /// <summary>
    /// Удаляет все пробельные символы из строки.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Строка без пробельных символов.</returns>
    public static string RemoveWhiteSpaces(this string input)
        => Regex.Replace(input, @"\s+", string.Empty);

    /// <summary>
    /// Конвертирует строку в слова с маленькой буквы, разделенные разделителем <paramref name="delimiter"/>.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <param name="delimiter">Разделитель.</param>
    /// <returns>Преобразованная строка.</returns>
    private static string ToLowerCaseWithDelimiter(
        this string value,
        string delimiter)
    {
        return string.Concat(
                value.Select((x, i) => i > 0 && char.IsUpper(x)
                    ? delimiter + x
                    : x.ToString()))
            .ToLower();
    }
}
