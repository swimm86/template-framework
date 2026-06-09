// ----------------------------------------------------------------------------------------------
// <copyright file="LifecyclePhase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Enums;

/// <summary>
/// Фаза жизненного цикла сущности.
/// </summary>
public enum LifecyclePhase
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
