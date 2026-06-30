// ----------------------------------------------------------------------------------------------
// <copyright file="AppException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Exceptions.Models.Base;

/// <summary>
/// Базовый класс ошибки приложения.
/// </summary>
public abstract class AppException
    : Exception, IWithAdditionalData
{
    /// <summary>
    /// Дополнительная информация.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="AppException"/>.
    /// </summary>
    protected AppException()
    {
    }

    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутренняя ошибка, вызвавшая текущее исключение.</param>
    /// <param name="additionalData">Дополнительные данные для потребителей API.</param>
    /// <inheritdoc cref="AppException"/>
    protected AppException(
        string message,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, innerException)
    {
        ValidateAdditionalData(additionalData);
        AdditionalData = additionalData;
    }

    /// <summary>
    /// Валидирует дополнительные данные.
    /// </summary>
    /// <param name="additionalData">Дополнительные данные для потребителей API.</param>
    private static void ValidateAdditionalData(
        IReadOnlyDictionary<string, object>? additionalData)
    {
        if (additionalData is { Count: 0 })
        {
            throw new ArgumentException(
                "Can't be an empty dictionary. Pass null if there is no data.",
                nameof(additionalData));
        }

        if (additionalData?.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key)) is true)
        {
            throw new ArgumentException(
                "Keys can't be null or empty",
                nameof(additionalData));
        }
    }
}
