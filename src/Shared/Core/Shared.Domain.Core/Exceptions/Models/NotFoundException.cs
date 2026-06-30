// ----------------------------------------------------------------------------------------------
// <copyright file="NotFoundException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
    /// <summary>Инициализирует новый экземпляр <see cref="NotFoundException"/>.</summary>
    /// <inheritdoc cref="AppException(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    public NotFoundException(
        string message,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, innerException, additionalData)
    {
    }

    /// <param name="entityType">Тип сущности, которая не была найдена.</param>
    /// <param name="key">Ключ.</param>
    /// <inheritdoc cref="NotFoundException(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    /// <param name="additionalData"/><param name="innerException"/>
    public NotFoundException(
        MemberInfo entityType,
        object key,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(
            $"Сущность " +
            $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
            $"не была найдена. Ключ: {key}",
            innerException,
            additionalData)
    {
    }

    /// <param name="keys">Ключи.</param>
    /// <inheritdoc cref="NotFoundException(MemberInfo, object, Exception?, IReadOnlyDictionary{string, object}?)"/>
    /// <param name="entityType"/><param name="additionalData"/><param name="innerException"/>
    public NotFoundException(
        MemberInfo entityType,
        object[] keys,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(
            $"Сущности " +
            $"\"{entityType.GetCustomAttribute<EntityNameAttribute>()?.Name ?? entityType.Name}\" " +
            $"не были найдены. Ключи: {string.Join(", ", keys)}",
            innerException,
            additionalData)
    {
    }
}
