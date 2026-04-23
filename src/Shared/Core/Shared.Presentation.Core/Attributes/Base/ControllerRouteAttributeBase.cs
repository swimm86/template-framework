// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerRouteAttributeBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Conventions;

namespace Shared.Presentation.Core.Attributes.Base;

/// <summary>
/// Базовый атрибут для составления маршрута контроллеров.
/// </summary>
/// <remarks>
/// Используется в конвенции <see cref="ControllerTypeConvention"/> для динамической
/// замены плейсхолдеров в шаблоне маршрута. Наследники должны указывать значение,
/// которое будет подставлено в маршрут вместо имени атрибута.
/// </remarks>
/// <param name="value">Значение для подстановки в маршрут.</param>
/// <example>
/// <code>
/// [ControllerType("bff")]
/// public abstract class BffControllerBase { }
/// // Маршрут: api/{appName}/bff/v1/{controller}
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public abstract class ControllerRouteAttributeBase(string value)
    : Attribute
{
    /// <summary>
    /// Значение.
    /// </summary>
    public string Value { get; } = value;
}