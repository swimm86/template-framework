// ----------------------------------------------------------------------------------------------
// <copyright file="HashHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Common.Helpers;

/// <summary>
/// Вспомогательный класс для вычисления хэш-значений фиксированной длины на основе строк.
/// </summary>
/// <remarks>
/// Использует алгоритм SHA-256 из <see cref="SHA256"/>.
/// Предназначен для дедупликации и сравнения сущностей; не предназначен для криптографической защиты
/// (например, хранения паролей), для которой следует применять медленные солёные алгоритмы.
/// </remarks>
public static class HashHelper
{
    /// <summary>
    /// Разделитель составных частей строки при формировании хэша.
    /// </summary>
    /// <remarks>
    /// Символ вертикальной черты выбран потому, что он крайне редко встречается в именах
    /// и адресах электронной почты и при этом не может привести к коллизиям вида
    /// <c>"a|b" + ""</c> vs <c>"a" + "|b"</c> при изменении порядка частей.
    /// </remarks>
    private const char PartSeparator = '|';

    /// <summary>
    /// Кодировка, используемая при преобразовании строк в байты.
    /// </summary>
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Вычисляет SHA-256 хэш для набора строковых компонентов, объединённых через разделитель.
    /// </summary>
    /// <param name="parts">
    /// Строковые компоненты, участвующие в формировании хэша.
    /// Каждый компонент нормализуется: <see cref="string.Trim()"/> и
    /// <see cref="string.ToLowerInvariant()"/> для обеспечения регистронезависимости.
    /// </param>
    /// <returns>
    /// Массив байт длиной 32 (256 бит) — результат <see cref="SHA256.HashData(byte[])"/>.
    /// Для одинакового набора нормализованных компонентов всегда возвращается идентичный массив.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="parts"/> равен <c>null</c>.
    /// </exception>
    public static byte[] ComputeSha256(params string?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var combined = CombineParts(parts);
        return SHA256.HashData(combined);
    }

    /// <summary>
    /// Собирает нормализованные строковые компоненты в единый буфер байт,
    /// разделяя их символом <see cref="PartSeparator"/>.
    /// </summary>
    /// <param name="parts">Исходные строковые компоненты.</param>
    /// <returns>Буфер байт в кодировке UTF-8, готовый для передачи в хэш-функцию.</returns>
    private static byte[] CombineParts(params string?[] parts)
    {
        var builder = new StringBuilder(capacity: parts.Length * 16);
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(PartSeparator);
            }

            var normalized = Normalize(parts[i]);
            builder.Append(normalized);
        }

        return Utf8.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Приводит компонент к каноническому виду для стабильного хэширования.
    /// </summary>
    /// <param name="value">Исходное значение (может быть <c>null</c>).</param>
    /// <returns>
    /// Нормализованная строка: обрезаны крайние пробелы, приведение к нижнему регистру
    /// по <see cref="CultureInfo.InvariantCulture"/>.
    /// </returns>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant();
    }
}
