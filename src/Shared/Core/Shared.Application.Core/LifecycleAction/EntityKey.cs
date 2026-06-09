// ----------------------------------------------------------------------------------------------
// <copyright file="EntityKey.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.LifecycleAction;

/// <summary>
/// Стабильный составной ключ сущности для использования в коллекциях,
/// зависящих от идентичности (<see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> и т.п.).
/// </summary>
/// <remarks>
/// <para>
/// Ссылочная идентичность <see cref="IEntity"/> нестабильна между сессиями,
/// Attach/Detach, и материализацией EF: одна и та же доменная сущность
/// может быть представлена разными экземплярами. Использование
/// <see cref="IEntity"/> как ключа словаря приводит к молчаливой потере
/// конфигурации, привязанной к экземпляру.
/// </para>
/// <para>
/// <see cref="EntityKey"/> решает эту проблему, объединяя тип сущности
/// и значение её идентификатора <see cref="IEntity.Id"/>.
/// </para>
/// </remarks>
/// <param name="Type">Тип сущности.</param>
/// <param name="Id">Значение идентификатора сущности.</param>
public readonly record struct EntityKey(Type Type, object Id)
{
    /// <summary>
    /// Создаёт <see cref="EntityKey"/> для указанной сущности.
    /// </summary>
    /// <param name="entity">Сущность, для которой строится ключ.</param>
    /// <returns>Ключ, основанный на типе и идентификаторе сущности.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="entity"/> равен <c>null</c>.
    /// </exception>
    public static EntityKey Of(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new EntityKey(entity.GetType(), entity.Id);
    }
}
