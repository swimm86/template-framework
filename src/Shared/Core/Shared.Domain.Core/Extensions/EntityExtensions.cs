// ----------------------------------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Extensions;

/// <summary>
/// Предоставляет методы расширения для работы с <see cref="IEntity"/>.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Определяет разницу между коллекциями источников и назначения, возвращая списки для добавления, удаления и обновления.
    /// </summary>
    /// <param name="src">Коллекция источников.</param>
    /// <param name="dest">Коллекция назначения.</param>
    /// <returns>
    /// Кортеж, содержащий следующие списки:
    /// <list type="bullet">
    /// <item>
    ///     <description>ItemsToAdd - элементы, которые присутствуют в источниках, но отсутствуют в назначении, и должны быть добавлены.</description>
    /// </item>
    /// <item>
    ///     <description>ItemsToRemove - элементы, которые присутствуют в назначении, но отсутствуют в источниках, и должны быть удалены.</description>
    /// </item>
    /// <item>
    ///     <description>ItemsToUpdate - кортежи, содержащие пары (элемент из источников, элемент из назначения), которые имеют совпадающие ключи, но содержимое отличается и требует обновления.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <typeparam name="TSource">Тип элементов в коллекции источников.</typeparam>
    /// <typeparam name="TDestination">Тип элементов в коллекции назначения.</typeparam>
    public static (
        ICollection<TSource> ItemsToAdd,
        ICollection<TDestination> ItemsToRemove,
        IEnumerable<(TSource src, TDestination dest)> ItemsToUpdate)
        GetDifferenceForMerge<TSource, TDestination>(this IEnumerable<TSource> src, IEnumerable<TDestination> dest)
        where TSource : IEntity
        where TDestination : IEntity
        => GetDifferenceForMerge(src, dest, source => source.Id, destination => destination.Id);

    /// <summary>
    /// Определяет разницу между коллекциями источников и назначения по указанным селекторам.
    /// Возвращает списки для добавления, удаления и обновления элементов.
    /// </summary>
    /// <param name="srcItems">Коллекция источников.</param>
    /// <param name="destItems">Коллекция назначения.</param>
    /// <param name="sourceSelector">Селектор для извлечения ключа из элемента источника. Должен возвращать уникальные значения.</param>
    /// <param name="destinationSelector">Селектор для извлечения ключа из элемента назначения. Должен возвращать уникальные значения.</param>
    /// <returns>
    /// Кортеж, содержащий следующие списки:
    /// <list type="bullet">
    /// <item>
    ///     <description>ItemsToAdd - элементы, которые присутствуют в источниках, но отсутствуют в назначении, и должны быть добавлены.</description>
    /// </item>
    /// <item>
    ///     <description>ItemsToRemove - элементы, которые присутствуют в назначении, но отсутствуют в источниках, и должны быть удалены.</description>
    /// </item>
    /// <item>
    ///     <description>ItemsToUpdate - кортежи, содержащие пары (элемент из источников, элемент из назначения), которые имеют совпадающие ключи, но содержимое отличается и требует обновления.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <typeparam name="TSource">Тип элементов в коллекции источников.</typeparam>
    /// <typeparam name="TDestination">Тип элементов в коллекции назначения.</typeparam>
    public static (
        ICollection<TSource> ItemsToAdd,
        ICollection<TDestination> ItemsToRemove,
        IEnumerable<(TSource src, TDestination dest)> ItemsToUpdate)
        GetDifferenceForMerge<TSource, TDestination>(
            this IEnumerable<TSource> srcItems,
            IEnumerable<TDestination> destItems,
            Func<TSource, object> sourceSelector,
            Func<TDestination, object> destinationSelector) =>
    (
        srcItems.Where(source => !destItems.Select(destinationSelector).Contains(sourceSelector(source))).ToArray(),
        destItems.ExceptBy(srcItems.Select(sourceSelector), destinationSelector).ToArray(),
        srcItems.Join(destItems, sourceSelector, destinationSelector, (src, dest) => (src, dest)).ToArray()
    );

    /// <summary>
    /// Объединяет две коллекции, выполняя операции добавления, удаления и обновления элементов в соответствии с делегатами.
    /// </summary>
    /// <param name="srcItems">Коллекция элементов источника.</param>
    /// <param name="destItems">Коллекция элементов назначения.</param>
    /// <param name="addItem">Делегат для добавления элемента.</param>
    /// <param name="removeItem">Делегат для удаления элемента (может быть <c>null</c>).</param>
    /// <param name="updateItem">Делегат для обновления элемента (может быть <c>null</c>).</param>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TDestination">Тип элемента назначения.</typeparam>
    /// <returns>Задача, представляющая выполнение объединения.</returns>
    public static Task MergeAsync<TSource, TDestination>(
        this IEnumerable<TSource> srcItems,
        IEnumerable<TDestination> destItems,
        Func<TSource, Task>? addItem = null,
        Func<TDestination, Task>? removeItem = null,
        Func<TSource, TDestination, Task>? updateItem = null)
        where TSource : class, IEntity
        where TDestination : class, IEntity
    {
        return MergeAsync(
            destItems,
            srcItems,
            source => source.Id,
            destination => destination.Id,
            addItem,
            removeItem,
            updateItem);
    }

    /// <summary>
    /// Объединяет две коллекции назначения и источника, выполняя операции добавления, удаления и обновления элементов в соответствии с делегатами.
    /// </summary>
    /// <param name="destItems">Коллекция элементов назначения.</param>
    /// <param name="srcItems">Коллекция элементов источника.</param>
    /// <param name="srcKeySelector">Функция для извлечения ключа элемента источника.</param>
    /// <param name="destKeySelector">Функция для извлечения ключа элемента назначения.</param>
    /// <param name="addItem">Делегат для добавления элемента.</param>
    /// <param name="removeItem">Делегат для удаления элемента (может быть <c>null</c>).</param>
    /// <param name="updateItem">Делегат для обновления элемента (может быть <c>null</c>).</param>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TDestination">Тип элемента назначения.</typeparam>
    /// <returns>Результат выполнения задачи по слиянию.</returns>
    public static async Task MergeAsync<TSource, TDestination>(
        this IEnumerable<TDestination> destItems,
        IEnumerable<TSource> srcItems,
        Func<TSource, object> srcKeySelector,
        Func<TDestination, object> destKeySelector,
        Func<TSource, Task>? addItem = null,
        Func<TDestination, Task>? removeItem = null,
        Func<TSource, TDestination, Task>? updateItem = null)
        where TSource : class
        where TDestination : class
    {
        var (itemsToAdd, itemsToRemove, itemsToUpdate) = GetDifferenceForMerge(srcItems, destItems, srcKeySelector, destKeySelector);

        if (addItem != null)
        {
            await itemsToAdd.ForeachAsync(addItem);
        }

        if (removeItem != null)
        {
            await itemsToRemove.ForeachAsync(removeItem);
        }

        if (updateItem != null)
        {
            await itemsToUpdate.ForeachAsync(x => updateItem(x.src, x.dest));
        }
    }
}

