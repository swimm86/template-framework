// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityWithMetadata.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Интерфейс для сущности с дополнительной метадатой.
/// </summary>
public interface IEntityWithMetadata : IEntity, IWithCreated, IWithUpdated, IWithDeleted, IDeletable
{
}

/// /// <summary>
/// Определяет интерфейс для сущности с дополнительной метадатой с идентификатором определенного типа.
/// </summary>
/// <typeparam name="T">Тип идентификатора сущности. Должен быть структурой.</typeparam>
public interface IEntityWithMetadata<out T> : IEntity<T>, IEntityWithMetadata
    where T : struct
{
}
