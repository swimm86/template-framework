// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateUpdated.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс обновления сущности
/// </summary>
public interface IWithDateUpdated
{
    /// <summary>
    /// Дата обновления.
    /// </summary>
    DateTime? DateUpdated { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateUpdated">Дата создания.</param>
    void SetDateUpdated(DateTime? dateUpdated);
}
