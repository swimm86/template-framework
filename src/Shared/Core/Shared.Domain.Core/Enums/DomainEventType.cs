// ----------------------------------------------------------------------------------------------
// <copyright file="DomainEventType.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
