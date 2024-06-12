// ----------------------------------------------------------------------------------------------
// <copyright file="NotFoundException.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.Exceptions.Models;

/// <summary>
/// Ошибка типа - Не найден.
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>
    /// Инициализация <see cref="NotFoundException"/>.
    /// </summary>
    public NotFoundException()
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    /// <param name="innerException"> Внутренняя ошибка. </param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
