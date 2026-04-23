// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileExtensions.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Shared.Common.Extensions;
using Shared.Domain.Core.Extensions;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Mapper.AutoMapper.Extensions;

/// <summary>
/// Расширения для <see cref="Profile"/>.
/// </summary>
public static class ProfileExtensions
{
    /// <summary>
    /// Конфигурирование коллекции.
    /// </summary>
    /// <typeparam name="TSrc">Тип исходных данных.</typeparam>
    /// <typeparam name="TDest">Тип результирующих данных.</typeparam>
    /// <param name="profile"><see cref="Profile"/>.</param>
    public static void ConfigureCollection<TSrc, TDest>(this Profile profile)
        where TSrc : IEntity
        where TDest : IEntity
    {
        profile
            .CreateMap<ICollection<TSrc>, ICollection<TDest>>()
            .ConvertUsing((src, dest, context) =>
            {
                dest ??= [];
                var (toAdd, toDelete, toUpdate) = src.GetDifferenceForMerge(dest);

                toDelete.ToList().ForEach(x => dest.Remove(x));

                toUpdate.ToList().ForEach(data =>
                {
                    context.Mapper.Map(data.src, data.dest);
                });

                toAdd.ForEach(newItem =>
                    dest.Add(context.Mapper.Map<TSrc, TDest>(newItem)));

                return dest;
            });
    }

    /// <summary>
    /// Конфигурирование коллекции.
    /// </summary>
    /// <typeparam name="TSrc">Тип исходных данных.</typeparam>
    /// <typeparam name="TDest">Тип результирующих данных.</typeparam>
    /// <param name="profile"><see cref="Profile"/>.</param>
    /// <param name="subSelector">Селектор для dto.</param>
    /// <param name="entitySelector">Селектор для сущностей.</param>
    /// <param name="compareFunc">Функция для сравнения.</param>
    public static void ConfigureCollection<TSrc, TDest>(
        this Profile profile,
        Func<TSrc, object> subSelector,
        Func<TDest, object> entitySelector,
        Func<TSrc, TDest, bool> compareFunc)
    {
        profile
            .CreateMap<ICollection<TSrc>, ICollection<TDest>>()
            .ConvertUsing((src, dest, context) =>
            {
                dest ??= [];
                var (toAdd, toDelete, toUpdate) = src.GetDifferenceForMerge(dest, subSelector, entitySelector);

                toDelete.ToList().ForEach(x => dest.Remove(x));

                toUpdate.ToList().ForEach(data =>
                {
                    context.Mapper.Map(data.src, data.dest);
                });

                toAdd.ForEach(newItem =>
                    dest.Add(context.Mapper.Map<TSrc, TDest>(newItem)));

                return dest;
            });
    }
}
