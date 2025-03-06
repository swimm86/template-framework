// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateDeleted.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс даты удаления.
/// </summary>
public interface IWithDateDeleted
{
    /// <summary>
    /// Дата удаления.
    /// </summary>
    DateTime? DateDeleted { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateDeleted">Дата удаления.</param>
    void SetDateDeleted(DateTime? dateDeleted);
}
