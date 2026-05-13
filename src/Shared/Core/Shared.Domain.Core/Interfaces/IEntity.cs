// ----------------------------------------------------------------------------------------------
// <copyright file="IEntity.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Определяет базовый интерфейс для сущности с идентификатором.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Абстрактный идентификатор сущности.
    /// </summary>
    object Id { get; }
}

/// <summary>
/// Определяет интерфейс для сущности с идентификатором определенного типа.
/// </summary>
/// <typeparam name="T">Тип идентификатора сущности. Должен быть структурой.</typeparam>
public interface IEntity<out T> : IEntity
{
    /// <inheritdoc/>>
    object IEntity.Id => Id;

    /// <summary>
    /// Идентификатор сущности с типом <see cref="T"/>.
    /// </summary>
    new T Id { get; }
}
