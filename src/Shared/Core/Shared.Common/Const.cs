// ----------------------------------------------------------------------------------------------
// <copyright file="Const.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Common;

/// <summary>
/// Общие константы, которые не подходят под конкретные группировки
/// </summary>
public static class Const
{
    /// <summary>
    /// Имя политики, которая используется в Cors.
    /// </summary>
    public const string CorsDefaultPolicyName = nameof(CorsDefaultPolicyName);

    /// <summary>
    /// Формат для даты.
    /// </summary>
    public const string DateOnlyFormat = "dd.MM.yyyy";

    /// <summary>
    /// Формат для даты и времени.
    /// </summary>
    public const string DateTimeFormat = "dd.MM.yyyy HH:mm";

    /// <summary>
    /// Размер батча по-умолчанию.
    /// </summary>
    public const int DefaultBatchSize = 100;
}
