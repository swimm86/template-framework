// ----------------------------------------------------------------------------------------------
// <copyright file="DefaultObjectToStringConverter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.Converters.Interfaces;

namespace Shared.Domain.Core.Converters;

/// <summary>
/// Конвертер объекта в строку по умолчанию.
/// </summary>
public class DefaultObjectToStringConverter
    : IObjectToStringConverter
{
    /// <inheritdoc />
    public string Convert(object? valueToConvert)
    {
        return valueToConvert switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "Да" : "Нет",
            Guid g => g.ToString("N"),
            Enum e => e.Description(),
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            _ => valueToConvert.ToString() ?? string.Empty,
        };
    }
}