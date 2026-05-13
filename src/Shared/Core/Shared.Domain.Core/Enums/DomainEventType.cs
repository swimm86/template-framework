// ----------------------------------------------------------------------------------------------
// <copyright file="DomainEventType.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Enums;

/// <summary>
/// Тип доменного события.
/// </summary>
public enum DomainEventType
{
    /// <summary>
    /// До сохранения.
    /// </summary>
    BeforeSave,

    /// <summary>
    /// После сохранения.
    /// </summary>
    AfterSave,
}
