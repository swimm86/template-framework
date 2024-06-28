// ----------------------------------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Extensions;

/// <summary>
/// Расширения для <see cref="IEntity"/>.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Получение списков на добавление, удаление изменение.
    /// </summary>
    /// <param name="dtos"> Dtos. </param>
    /// <param name="entities"> Сущности. </param>
    /// <returns> Списки на добавление, удаление изменение. </returns>
    /// <typeparam name="TSubDto"> Тип dto. </typeparam>
    /// <typeparam name="TSubEntity"> Тип сущности. </typeparam>
    public static
        (IEnumerable<TSubDto> forAddItems, IEnumerable<TSubEntity> forRemoveItems, IEnumerable<TSubDto> forUpdateItems)
        GetDifferenceForMerge<TSubDto, TSubEntity>(this IEnumerable<TSubDto> dtos, IEnumerable<TSubEntity> entities)
        where TSubDto : IEntity
        where TSubEntity : IEntity
    {
        var forAddItems = dtos.ExceptBy(entities.Select(entity => entity.Id), dto => dto.Id);
        var forRemoveItems = entities.ExceptBy(dtos.Select(dto => dto.Id), entity => entity.Id);
        var forUpdateItems = dtos.IntersectBy(entities.Select(entity => entity.Id), dto => dto.Id);
        return (forAddItems, forRemoveItems, forUpdateItems);
    }

    /// <summary>
    /// Слияние списков.
    /// </summary>
    /// <param name="dtos"> Список dto. </param>
    /// <param name="entities"> Список сущностей. </param>
    /// <param name="addEntity"> Делегат добавления. </param>
    /// <param name="removeEntity"> Делегат удаления. </param>
    /// <param name="updateEntity"> Делегат изменения. </param>
    /// <typeparam name="TSubDto"> Тип dto. </typeparam>
    /// <typeparam name="TSubEntity"> Тип сущности. </typeparam>
    public static void Merge<TSubDto, TSubEntity>(
        IEnumerable<TSubDto> dtos,
        IEnumerable<TSubEntity> entities,
        Action<TSubDto> addEntity,
        Action<TSubEntity>? removeEntity = null,
        Action<TSubDto>? updateEntity = null)
        where TSubDto : IEntity
        where TSubEntity : IEntity
    {
        var (forAddItems, forRemoveItems, forUpdateItems) = GetDifferenceForMerge(dtos, entities);

        foreach (var addItem in forAddItems)
        {
            addEntity(addItem);
        }

        if (removeEntity is not null)
        {
            foreach (var removeItem in forRemoveItems)
            {
                removeEntity(removeItem);
            }
        }

        if (updateEntity is not null)
        {
            foreach (var updateItem in forUpdateItems)
            {
                updateEntity(updateItem);
            }
        }
    }
}
