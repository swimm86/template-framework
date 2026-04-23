// ----------------------------------------------------------------------------------------------
// <copyright file="IWithAdditionalData.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс для объектов, содержащих дополнительные данные для потребителей API.
/// </summary>
/// <remarks>
/// <para>
/// Используется для передачи произвольных данных через HTTP-ответы, которые доступны
/// только бэкенд-потребителям и не передаются фронтенду.
/// </para>
/// </remarks>
public interface IWithAdditionalData
{
    /// <summary>
    /// Дополнительные данные для потребителей API.
    /// </summary>
    IReadOnlyDictionary<string, object>? AdditionalData { get; }
}
