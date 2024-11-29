// ----------------------------------------------------------------------------------------------
// <copyright file="AppException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Exceptions.Models.Base;

/// <summary>
/// Базовый класс ошибки приложения.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Инициализация <see cref="AppException"/>.
    /// </summary>
    protected AppException()
    {
    }

    /// <summary>
    /// Инициализация <see cref="AppException"/> с сообщением.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    protected AppException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализация <see cref="AppException"/> с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    /// <param name="innerException"> Внутренняя ошибка. </param>
    protected AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
