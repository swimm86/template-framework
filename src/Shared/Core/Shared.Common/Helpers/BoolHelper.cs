// ----------------------------------------------------------------------------------------------
// <copyright file="BoolHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Helpers;

/// <summary>
/// Вспомогательный класс для преобразования строковых представлений логических значений.
/// </summary>
public static class BoolHelper
{
    private static readonly Dictionary<string, bool> RussianBooleanStrings = new()
    {
        { "да", true }, { "нет", false }
    };

    /// <summary>
    /// Возвращает логическое значение, соответствующее заданной строке.
    /// </summary>
    /// <param name="value">Строка, которую нужно преобразовать в логическое значение.</param>
    /// <param name="strong">
    /// Если <c>true</c>, выполняется поиск по подстроке; если <c>false</c> — точное совпадение.
    /// </param>
    /// <returns>Логическое значение, соответствующее заданной строке, или <c>null</c>, если соответствие не найдено.</returns>
    public static bool? GetBooleanValueByString(string value, bool strong)
    {
        var key = RussianBooleanStrings.Keys.FirstOrDefault(x =>
            strong
                ? x.Contains(value, StringComparison.InvariantCultureIgnoreCase)
                : x.Equals(value, StringComparison.InvariantCultureIgnoreCase));
        return key == null ? null : RussianBooleanStrings[key];
    }
}
