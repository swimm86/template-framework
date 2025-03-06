// ----------------------------------------------------------------------------------------------
// <copyright file="NotFoundException.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Domain.Core.Attributes;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Exceptions.Models;

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
    /// Инициализация <see cref="NotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="entityType">Тип сущности, которая не была найдена.</param>
    /// <param name="key">Ключ.</param>
    public NotFoundException(MemberInfo entityType, object key)
        : base($"Сущность " +
               $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
               $"не была найдена. Ключ: {key}")
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="entityType">Тип сущности, которая не была найдена.</param>
    /// <param name="keys">Ключи.</param>
    public NotFoundException(MemberInfo entityType, object[] keys)
        : base($"Сущности " +
               $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
               $"не были найдены. Ключи: {string.Join(", ", keys)}")
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
