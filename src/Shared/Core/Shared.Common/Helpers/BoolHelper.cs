// ----------------------------------------------------------------------------------------------
// <copyright file="BoolHelper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common.Helpers;

/// <summary>
/// Расширение для <see cref="bool"/>.
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
    /// <param name="strong">Если true, то метод ищет подстроку, иначе - точное совпадение.</param>
    /// <returns>Логическое значение, соответствующее заданной строке, или null, если строка не найдена.</returns>
    public static bool? GetBooleanValueByString(string value, bool strong)
    {
        var key = RussianBooleanStrings.Keys.FirstOrDefault(x =>
            strong
                ? x.Contains(value, StringComparison.InvariantCultureIgnoreCase)
                : x.Equals(value, StringComparison.InvariantCultureIgnoreCase));
        return key == null ? null : RussianBooleanStrings[key];
    }
}
