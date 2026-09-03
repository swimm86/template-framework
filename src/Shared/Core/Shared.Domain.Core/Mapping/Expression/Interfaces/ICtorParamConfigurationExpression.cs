// ----------------------------------------------------------------------------------------------
// <copyright file="ICtorParamConfigurationExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Mapping.Expression.Interfaces;

/// <summary>
/// Выражение конфигурации параметра конструктора целевого типа.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
public interface ICtorParamConfigurationExpression<TSource>
{
    /// <summary>
    /// Регистрирует источник значения через лямбду.
    /// </summary>
    /// <typeparam name="TSourceMember">Тип элемента источника.</typeparam>
    /// <param name="sourceMember">Лямбда-выражение для получения значения из источника.</param>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
}
