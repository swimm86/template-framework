// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateUpdated.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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
