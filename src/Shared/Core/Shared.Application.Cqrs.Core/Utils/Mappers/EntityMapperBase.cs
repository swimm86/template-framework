// ----------------------------------------------------------------------------------------------
// <copyright file="EntityMapperBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Utils.Mappers.EntityMapper;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Cqrs.Core.Utils.Mappers;

/// <summary>
/// Класс для слияния списка dto и списка сущностей.
/// </summary>
/// <typeparam name="TDto">Тип Дто.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public abstract class EntityMapperBase<TDto, TEntity> : IEntityMapper<TDto, TEntity>
{
    /// <inheritdoc/>
    public abstract void Map(TDto dto, TEntity entity);

    /// <summary>
    /// Получение списков на добавление, удаление изменение.
    /// </summary>
    /// <param name="dtos"> Dtos. </param>
    /// <param name="entities"> Сущности. </param>
    /// <returns> Списки на добавление, удаление изменение. </returns>
    /// <typeparam name="TSubDto"> Тип dto. </typeparam>
    /// <typeparam name="TSubEntity"> Тип сущности. </typeparam>
    protected virtual (IEnumerable<TSubDto> forAddItems, IEnumerable<TSubEntity> forRemoveItems, IEnumerable<TSubDto> forUpdateItems)
        GetDifferenceForMerge<TSubDto, TSubEntity>(IEnumerable<TSubDto> dtos, IEnumerable<TSubEntity> entities)
            where TSubDto : IEntity
            where TSubEntity : IEntity
            => (forAddItems: dtos.Where(dto => !entities.Any(x => x.Id!.Equals(dto.Id))).ToList(),
                forRemoveItems: entities.Where(entitybcEvent => !dtos.Any(x => x.Id!.Equals(entitybcEvent.Id))).ToList(),
                forUpdateItems: dtos.IntersectBy(entities.Select(x => x.Id), x => x.Id).ToList());

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
    protected void Merge<TSubDto, TSubEntity>(
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
