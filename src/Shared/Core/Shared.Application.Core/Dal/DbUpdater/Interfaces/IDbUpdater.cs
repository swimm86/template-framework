// ----------------------------------------------------------------------------------------------
// <copyright file="IDbUpdater.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbUpdater.Interfaces;

/// <summary>
/// Интерфейс для классов инициализации и миграции базы данных.
/// </summary>
public interface IDbUpdater
{
    /// <summary>
    /// Создает базу данных, если она не существует.
    /// </summary>
    void CreateDbIfNotExists();

    /// <summary>
    /// Применяет миграции базы данных до последней версии.
    /// </summary>
    void Migrate();

    /// <summary>
    /// Выполняет начальную инициализацию базы данных.
    /// </summary>
    void Initialize();
}
