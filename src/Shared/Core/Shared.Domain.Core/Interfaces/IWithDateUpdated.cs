// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateUpdated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет контракт для объектов с датой обновления.
/// </summary>
public interface IWithDateUpdated
{
    /// <summary>
    /// Дата обновления.
    /// </summary>
    DateTime? DateUpdated { get; }

    /// <summary>
    /// Устанавливает дату обновления.
    /// </summary>
    /// <param name="dateUpdated">Дата обновления.</param>
    void SetDateUpdated(DateTime? dateUpdated);
}
