// ----------------------------------------------------------------------------------------------
// <copyright file="AppException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
    /// Инициализация <see cref="AppException"/>.
    /// </summary>
    protected AppException()
    {
    }

    /// <summary>
    /// Инициализация <see cref="AppException"/> с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="additionalData">Дополнительные данные для потребителей API.</param>
    protected AppException(
        string message,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message)
    {
        ValidateAdditionalData(additionalData);
        AdditionalData = additionalData;
    }

    /// <summary>
    /// Инициализация <see cref="AppException"/> с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутренняя ошибка, вызвавшая текущее исключение.</param>
    /// <param name="additionalData">Дополнительные данные для потребителей API.</param>
    protected AppException(
        string message,
        Exception innerException,
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
        if (additionalData?.Any(kvp => string.IsNullOrEmpty(kvp.Key)) is true)
        {
            throw new ArgumentException("Ключи не могут быть null или пустыми", nameof(additionalData));
        }
    }
}
