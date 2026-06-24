// ----------------------------------------------------------------------------------------------
// <copyright file="GroupingPagingGuard.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Dal.Repository;

/// <summary>
/// Проверяет допустимость комбинации параметров пагинации и порядка групп.
/// </summary>
public static class GroupingPagingGuard
{
    /// <summary>
    /// Бросает <see cref="ArgumentException"/>, если задана пагинация (<paramref name="skip"/> / <paramref name="take"/>)
    /// без явного направления сортировки групп (<paramref name="groupKeyOrderDirection"/>).
    /// </summary>
    /// <param name="skip">Количество групп, которые необходимо пропустить.</param>
    /// <param name="take">Количество групп, которые необходимо извлечь.</param>
    /// <param name="groupKeyOrderDirection">Направление сортировки групп по ключу.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="skip"/> или <paramref name="take"/> заданы при <paramref name="groupKeyOrderDirection"/> = <see langword="null"/>.
    /// </exception>
    public static void EnsureGroupOrderingForPaging(
        int? skip,
        int? take,
        OrderDirectionType? groupKeyOrderDirection)
    {
        if ((skip.HasValue || take.HasValue) && groupKeyOrderDirection is null)
        {
            var invalidArgumentName = nameof(groupKeyOrderDirection);
            throw new ArgumentException(
                $"{invalidArgumentName} must be specified when skip or take is used; " +
                $"otherwise the order of groups is non-deterministic.",
                invalidArgumentName);
        }
    }
}
