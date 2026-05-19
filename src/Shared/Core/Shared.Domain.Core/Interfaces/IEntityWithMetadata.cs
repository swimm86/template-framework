// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityWithMetadata.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс для сущности с дополнительной метадатой.
/// </summary>
public interface IEntityWithMetadata : IEntity, IWithCreated, IWithUpdated, IWithDeleted
{
}

/// <summary>
/// Определяет интерфейс для сущности с дополнительной метадатой и идентификатором определенного типа.
/// </summary>
/// <typeparam name="T">Тип идентификатора сущности. Должен быть структурой.</typeparam>
public interface IEntityWithMetadata<out T>
    : IEntity<T>, IEntityWithMetadata
    where T : struct;
