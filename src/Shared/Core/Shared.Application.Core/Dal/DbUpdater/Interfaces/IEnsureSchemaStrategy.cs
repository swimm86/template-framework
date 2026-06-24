// ----------------------------------------------------------------------------------------------
// <copyright file="IEnsureSchemaStrategy.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Dal.DbUpdater.Interfaces;

/// <summary>
/// Определяет стратегию инициализации схемы базы данных, не зависящую от конкретного провайдера.
/// </summary>
/// <remarks>
/// Применяется в <see cref="IDbUpdater.CreateDbIfNotExists"/> для сокрытия реляционно-специфичных
/// операций (например, <c>GetPendingMigrations</c>, недоступного в InMemory-провайдере)
/// за абстракцией. Это позволяет тестировать <c>DbUpdaterBase</c> без подключения к реляционной БД.
/// </remarks>
public interface IEnsureSchemaStrategy
{
    /// <summary>
    /// Создаёт схему базы данных, если она ещё не существует.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если схема была создана в рамках вызова;
    /// <see langword="false"/>, если схема уже существовала или создание не требовалось
    /// (например, для провайдеров, не использующих миграции EF Core).
    /// </returns>
    bool EnsureSchemaIfNeeded();
}
