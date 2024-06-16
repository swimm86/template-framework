// ----------------------------------------------------------------------------------------------
// <copyright file="IEntityMapper.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Utils.Mappers.EntityMapper;

/// <summary>
/// Маппер для Entity из DTO.
/// </summary>
/// <typeparam name="TDto">Тип Дто.</typeparam>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface IEntityMapper<in TDto, in TEntity>
{
    /// <summary>
    /// Мапит полученный объект сущности.
    /// </summary>
    /// <param name="dto"> Дто. </param>
    /// <param name="entity"> Сущность. </param>
    void Map(TDto dto, TEntity entity);
}
