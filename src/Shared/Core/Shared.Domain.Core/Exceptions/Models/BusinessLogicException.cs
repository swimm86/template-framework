// ----------------------------------------------------------------------------------------------
// <copyright file="BusinessLogicException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Exceptions.Models;

/// <summary>
/// Ошибка бизнес логики.
/// </summary>
public class BusinessLogicException : AppException
{
    /// <summary>
    /// Инициализация <see cref="BusinessLogicException"/>.
    /// </summary>
    public BusinessLogicException()
    {
    }

    /// <summary>
    /// Инициализация <see cref="BusinessLogicException"/> с сообщением.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    public BusinessLogicException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализация <see cref="BusinessLogicException"/> с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    /// <param name="innerException"> Внутренняя ошибка. </param>
    public BusinessLogicException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
