// ----------------------------------------------------------------------------------------------
// <copyright file="IMappingExpression.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Expression.Interfaces;

/// <summary>
/// Выражение преобразования (mapping) между <typeparamref name="TSource"/> и <typeparamref name="TDestination"/>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>
    /// Регистрирует правило для конкретного свойства целевого типа.
    /// </summary>
    /// <typeparam name="TMember">Тип настраиваемого свойства целевого типа.</typeparam>
    /// <param name="destinationMember">Лямбда-выражение, выбирающее свойство целевого типа.</param>
    /// <param name="memberOptions">Действие конфигурации свойства.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    /// <summary>
    /// Регистрирует правило для параметра конструктора целевого типа.
    /// </summary>
    /// <param name="ctorParamName">Имя параметра конструктора.</param>
    /// <param name="paramOptions">Действие конфигурации параметра конструктора.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ForCtorParam(
        string ctorParamName,
        Action<ICtorParamConfigurationExpression<TSource>> paramOptions);

    /// <summary>
    /// Регистрирует действие, вызываемое после преобразования (mapping).
    /// </summary>
    /// <param name="afterMapAction">Действие, вызываемое после преобразования.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> AfterMap(
        Action<TSource, TDestination> afterMapAction);

    /// <summary>
    /// Регистрирует действие, вызываемое перед преобразованием (mapping).
    /// </summary>
    /// <param name="beforeMapAction">Действие, вызываемое перед преобразованием.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> BeforeMap(
        Action<TSource, TDestination> beforeMapAction);

    /// <summary>
    /// Регистрирует фабрику для создания целевого объекта из источника.
    /// </summary>
    /// <param name="ctor">Фабрика, создающая целевой объект из источника.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ConstructUsing(
        Func<TSource, TDestination> ctor);

    /// <summary>
    /// Регистрирует фабрику для создания целевого объекта с доступом к <see cref="ResolutionContext"/>.
    /// </summary>
    /// <param name="ctor">Фабрика, создающая целевой объект из источника и контекста.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ConstructUsing(
        Func<TSource, ResolutionContext, TDestination> ctor);

    /// <summary>
    /// Подключает пользовательский <see cref="ITypeConverter{TSource, TDestination}"/>.
    /// </summary>
    /// <param name="converter">Провайдеро-независимый конвертер типов (type converter).</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        ITypeConverter<TSource, TDestination> converter);

    /// <summary>
    /// Регистрирует преобразование через лямбду.
    /// </summary>
    /// <param name="mappingFunction">Функция-конвертер (type converter).</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination> mappingFunction);

    /// <summary>
    /// Регистрирует преобразование через лямбду с доступом к существующему экземпляру и <see cref="ResolutionContext"/>.
    /// </summary>
    /// <param name="mappingFunction">Функция-конвертер (type converter), принимающая источник, существующий целевой экземпляр и контекст.</param>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination, ResolutionContext, TDestination> mappingFunction);

    /// <summary>
    /// Регистрирует обратное преобразование (mapping <typeparamref name="TDestination"/> → <typeparamref name="TSource"/>).
    /// </summary>
    /// <returns>Выражение преобразования обратного направления.</returns>
    IMappingExpression<TDestination, TSource> ReverseMap();

    /// <summary>
    /// Подключает правила базового преобразования (mapping) из указанной пары типов.
    /// </summary>
    /// <typeparam name="TSourceBase">Базовый исходный тип.</typeparam>
    /// <typeparam name="TDestBase">Базовый целевой тип.</typeparam>
    /// <returns>Текущее выражение преобразования для fluent-цепочки.</returns>
    IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestBase>();
}
