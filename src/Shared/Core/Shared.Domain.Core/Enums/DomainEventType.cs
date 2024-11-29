// ----------------------------------------------------------------------------------------------
// <copyright file="DomainEventType.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
