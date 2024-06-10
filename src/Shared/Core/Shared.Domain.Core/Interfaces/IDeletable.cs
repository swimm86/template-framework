// ----------------------------------------------------------------------------------------------
// <copyright file="IDeletable.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс для отметки на удаление.
/// </summary>
public interface IDeletable
{
    /// <summary>
    /// Удален.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Установить в состояние - удален.
    /// </summary>
    void SetIsDeleted();
}
