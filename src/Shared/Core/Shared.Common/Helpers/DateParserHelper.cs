// ----------------------------------------------------------------------------------------------
// <copyright file="DateParserHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Globalization;

namespace Shared.Common.Helpers;

/// <summary>
/// Вспомогательный класс для парсинга дат из строк в различных форматах.
/// </summary>
public static class DateParserHelper
{
    /// <summary>
    /// Поддерживаемые форматы дат для парсинга из пользовательского ввода.
    /// </summary>
    private static readonly string[] DateTimeFormats =
    [
        "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt", "MM/dd/yyyy hh:mm:ss",
        "M/d/yyyy h:mm:ss", "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
        "M/d/yyyy h:mm", "M/d/yyyy h:mm", "MM/dd/yyyy hh:mm",
        "M/dd/yyyy hh:mm", "dd.MM.yyyy", "dd.MM.yyyy hh:mm:ss", "MM/dd/yyyy", "yyyy-MM-dd HH:mm:ss.fff",
        "dd.MM.yyyy HH:mm:ss.fff", "dd.MM.yyyy H:mm:ss", "M/d/yyyy", "M/dd/yyyy", "MM/d/yyyy", "yyyy-MM-dd"
    ];

    /// <summary>
    /// Парсит строку с датой, игнорируя время, если оно присутствует.
    /// </summary>
    /// <param name="dateString">Строка с датой.</param>
    /// <returns>Объект DateOnly, если парсинг успешен; иначе null.</returns>
    public static DateOnly? TryParseDateOnlyIgnoringTime(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        if (DateOnly.TryParseExact(
                dateString, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        var dateTime = TryParseDateTime(dateString);

        if (dateTime is not null)
        {
            return DateOnly.FromDateTime(dateTime.Value);
        }

        return null;
    }

    /// <summary>
    /// Выполняет парсинг даты из строки с поддержкой множества форматов.
    /// </summary>
    /// <param name="inValue">Входящая строка с датой.</param>
    /// <returns>Значение <see cref="DateTime"/> при успешном парсинге; иначе <c>null</c>.</returns>
    public static DateTime? TryParseDateTime(string? inValue)
    {
        if (string.IsNullOrWhiteSpace(inValue))
        {
            return null;
        }

        if (inValue.StartsWith('\''))
        {
            inValue = inValue[1..];
        }

        var success = DateTime.TryParseExact(
            inValue, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var outValue);

        if (!success)
        {
            success = DateTime.TryParse(inValue, CultureInfo.InvariantCulture, out outValue);
        }

        if (!success)
        {
            success = TryParseDateFromDouble(inValue, out outValue);
        }

        if (!success)
        {
            return null;
        }

        return outValue;
    }

    private static bool TryParseDateFromDouble(string inValue, out DateTime dateTime)
    {
        dateTime = DateTime.MinValue;

        if (double.TryParse(inValue, out var doubleValue))
        {
            try
            {
                dateTime = DateTime.FromOADate(doubleValue);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return false;
    }
}
