// ----------------------------------------------------------------------------------------------
// <copyright file="IMapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Mapping.Description.Interfaces;

/// <summary>
/// Описание одного преобразования (mapping) между двумя типами. Провайдеро-независимое.
/// </summary>
internal interface IMapDescription
{
    /// <summary>
    /// Исходный тип.
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Целевой тип.
    /// </summary>
    Type DestinationType { get; }

    /// <summary>
    /// Настройки членов.
    /// </summary>
    IReadOnlyCollection<IMemberMapDescription> Members { get; }

    /// <summary>
    /// Настройки параметров конструктора.
    /// </summary>
    IReadOnlyCollection<ICtorParamMapDescription> CtorParams { get; }

    /// <summary>
    /// Действие после преобразования (mapping).
    /// </summary>
    Action<object, object>? AfterMap { get; }

    /// <summary>
    /// Действие перед преобразованием (mapping).
    /// </summary>
    Action<object, object>? BeforeMap { get; }

    /// <summary>
    /// Фабрика целевого объекта с доступом к <see cref="ResolutionContext"/>.
    /// </summary>
    Func<object, ResolutionContext, object>? ConstructUsing { get; }

    /// <summary>
    /// Кастомный конвертер типов (type converter).
    /// </summary>
    object? Converter { get; }

    /// <summary>
    /// Функция-конвертер (type converter).
    /// </summary>
    Func<object, object>? ConvertUsingFunction { get; }

    /// <summary>
    /// Функция-конвертер (type converter) с тремя параметрами (src, dest, ctx) и доступом к <see cref="ResolutionContext"/>.
    /// </summary>
    Func<object, object, ResolutionContext, object>? ConvertUsingFunctionWithDest { get; }

    /// <summary>
    /// Включено ли обратное преобразование (mapping).
    /// </summary>
    bool HasReverseMap { get; }

    /// <summary>
    /// Базовые преобразования (mapping) для <c>IncludeBase</c>.
    /// </summary>
    IReadOnlyList<(Type SourceBase, Type DestBase)> IncludedBases { get; }
}
