// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateDeleted.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет контракт для объектов с датой удаления.
/// </summary>
public interface IWithDateDeleted
{
    /// <summary>
    /// Дата удаления.
    /// </summary>
    DateTime? DateDeleted { get; }

    /// <summary>
    /// Устанавливает дату удаления.
    /// </summary>
    /// <param name="dateDeleted">Дата удаления.</param>
    void SetDateDeleted(DateTime? dateDeleted);
}
