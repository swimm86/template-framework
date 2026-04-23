// ----------------------------------------------------------------------------------------------
// <copyright file="IWithDateCreated.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс даты создания.
/// </summary>
public interface IWithDateCreated
{
    /// <summary>
    /// Дата создания.
    /// </summary>
    DateTime DateCreated { get; }

    /// <summary>
    /// Метод установки.
    /// </summary>
    /// <param name="dateCreated">Дата создания.</param>
    void SetDateCreated(DateTime dateCreated);
}
