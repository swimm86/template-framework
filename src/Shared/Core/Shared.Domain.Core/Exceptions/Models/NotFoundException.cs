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
public class NotFoundException
    : AppException
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
    /// <param name="additionalData">Дополнительная информация для потребителей.</param>
    public NotFoundException(
        string message,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, additionalData)
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="entityType">Тип сущности, которая не была найдена.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="additionalData">Дополнительная информация для потребителей.</param>
    public NotFoundException(
        MemberInfo entityType,
        object key,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(
            $"Сущность " +
            $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
            $"не была найдена. Ключ: {key}",
            additionalData)
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="entityType">Тип сущности, которая не была найдена.</param>
    /// <param name="keys">Ключи.</param>
    /// <param name="additionalData">Дополнительная информация для потребителей.</param>
    public NotFoundException(
        MemberInfo entityType,
        object[] keys,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(
            $"Сущности " +
            $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
            $"не были найдены. Ключи: {string.Join(", ", keys)}",
            additionalData)
    {
    }

    /// <summary>
    /// Инициализация <see cref="NotFoundException"/> с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message"> Сообщение. </param>
    /// <param name="innerException"> Внутренняя ошибка. </param>
    /// <param name="additionalData">Дополнительная информация для потребителей.</param>
    public NotFoundException(
        string message,
        Exception innerException,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, innerException, additionalData)
    {
    }
}
