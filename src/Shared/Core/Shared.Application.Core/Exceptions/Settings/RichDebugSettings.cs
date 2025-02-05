// ----------------------------------------------------------------------------------------------
// <copyright file="RichDebugSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Exceptions.Settings;

/// <summary>
/// Настройка отображения расширенного дебага.
/// </summary>
/// <param name="IsEnabled">Признак того, что расширенный дебаг включен.</param>
public record RichDebugSettings(bool IsEnabled);
