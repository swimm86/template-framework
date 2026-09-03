// ----------------------------------------------------------------------------------------------
// <copyright file="IMemberConfigurationExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Expression.Interfaces;

/// <summary>
/// Выражение конфигурации члена преобразования (mapping).
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
/// <typeparam name="TMember">Тип настраиваемого члена целевого типа.</typeparam>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Регистрирует источник значения через лямбду.
    /// </summary>
    /// <typeparam name="TSourceMember">Тип элемента источника.</typeparam>
    /// <param name="sourceMember">Лямбда-выражение для получения значения из источника.</param>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);

    /// <summary>
    /// Регистрирует источник значения через лямбду с доступом к параметрам, переданным в <see cref="IMapper.ProjectTo{TResult}(IQueryable, object?)"/>.
    /// </summary>
    /// <typeparam name="TSourceMember">Тип элемента источника.</typeparam>
    /// <param name="sourceMember">
    /// Лямбда-выражение, принимающее исходный объект и словарь параметров.
    /// Внутри допустимы обращения <c>@params["key"]</c> (индексатор <see cref="IDictionary{TKey, TValue}"/>).
    /// </param>
    /// <remarks>
    /// Поддерживается только в <see cref="IMapper.ProjectTo{TResult}(IQueryable, object?)"/>: для in-memory
    /// <see cref="IMapper.Map{TSource, TResult}(TSource)"/> второй параметр всегда приходит как пустой словарь.
    /// <para>
    /// Используется для runtime-параметризации вычисляемых полей в проекции
    /// (например, локализация, tenant-id, фильтрация через <c>CASE WHEN</c>).
    /// </para>
    /// </remarks>
    void MapFrom<TSourceMember>(Expression<Func<TSource, IDictionary<string, object?>, TSourceMember>> sourceMember);

    /// <summary>
    /// Исключает свойство из преобразования (mapping).
    /// </summary>
    /// <remarks>
    /// Значение в целевом объекте остаётся без изменений.
    /// </remarks>
    void Ignore();
}
