// ----------------------------------------------------------------------------------------------
// <copyright file="LifecycleHookType.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Enums;

/// <summary>
/// Тип перехвата жизненного цикла сущности.
/// </summary>
public enum LifecycleHookType
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
