// ----------------------------------------------------------------------------------------------
// <copyright file="BusinessLogicException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Exceptions.Models;

/// <summary>
/// Ошибка бизнес логики.
/// </summary>
public class BusinessLogicException
    : AppException
{
    /// <summary>Инициализирует новый экземпляр <see cref="BusinessLogicException"/>.</summary>
    /// <inheritdoc cref="AppException(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    public BusinessLogicException(
        string message,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, innerException, additionalData)
    {
    }
}
