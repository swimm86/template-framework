// ----------------------------------------------------------------------------------------------
// <copyright file="MappingProfileExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Extensions;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping.Expression.Interfaces;

namespace Shared.Domain.Core.Mapping.Extensions;

/// <summary>
/// Расширения для <see cref="IMappingExpression{TSource, TDestination}"/>.
/// </summary>
public static class MappingProfileExtensions
{
    /// <summary>
    /// Конфигурирование преобразования (mapping) коллекции сущностей с операциями добавления/удаления/обновления
    /// по идентификатору сущности.
    /// </summary>
    /// <typeparam name="TSource">Тип элементов исходной коллекции.</typeparam>
    /// <typeparam name="TDestination">Тип элементов целевой коллекции.</typeparam>
    /// <param name="mapping">Выражение преобразования коллекций.</param>
    /// <returns>Выражение преобразования для fluent-цепочки.</returns>
    public static IMappingExpression<ICollection<TSource>, ICollection<TDestination>> ConfigureCollection<TSource, TDestination>(
        this IMappingExpression<ICollection<TSource>, ICollection<TDestination>> mapping)
        where TSource : IEntity
        where TDestination : IEntity
    {
        return mapping.ConfigureCollection(source => source.Id, destination => destination.Id);
    }

    /// <summary>
    /// Конфигурирование преобразования (mapping) коллекции сущностей с операциями добавления/удаления/обновления
    /// по указанным селекторам ключа.
    /// </summary>
    /// <typeparam name="TSource">Тип элементов исходной коллекции.</typeparam>
    /// <typeparam name="TDestination">Тип элементов целевой коллекции.</typeparam>
    /// <param name="mapping">Выражение преобразования коллекций.</param>
    /// <param name="sourceSelector">Селектор ключа элемента источника.</param>
    /// <param name="destinationSelector">Селектор ключа элемента назначения.</param>
    /// <returns>Выражение преобразования для fluent-цепочки.</returns>
    public static IMappingExpression<ICollection<TSource>, ICollection<TDestination>> ConfigureCollection<TSource, TDestination>(
        this IMappingExpression<ICollection<TSource>, ICollection<TDestination>> mapping,
        Func<TSource, object> sourceSelector,
        Func<TDestination, object> destinationSelector)
        where TSource : IEntity
        where TDestination : IEntity
    {
        return mapping.ConvertUsing((src, dest, ctx) =>
        {
            dest ??= [];
            var (toAdd, toDelete, toUpdate) = src.GetDifferenceForMerge(dest, sourceSelector, destinationSelector);

            foreach (var item in toDelete)
            {
                dest.Remove(item);
            }

            foreach (var (srcItem, destItem) in toUpdate)
            {
                ctx.Map(srcItem, destItem);
            }

            foreach (var newItem in toAdd)
            {
                dest.Add(ctx.Map<TSource, TDestination>(newItem));
            }

            return dest;
        });
    }
}
