// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerTypeAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes.Base;
using Shared.Presentation.Core.Conventions;

namespace Shared.Presentation.Core.Attributes;

/// <summary>
/// Указывает тип контроллера для использования в маршрутизации.
/// </summary>
/// <remarks>
/// Применяется к базовому классу контроллера. Значение подставляется в маршрут
/// вместо плейсхолдера <c>[controllerType]</c>. Используется в конвенции
/// <see cref="ControllerTypeConvention"/>.
/// </remarks>
/// <param name="value">Тип контроллера (например, "bff", "getter", "setter").</param>
/// <example>
/// <code>
/// [ControllerType("getter")]
/// public abstract class GetterControllerBase { }
/// // Маршрут: api/{appName}/getter/v1/{controller}
/// </code>
/// </example>
public class ControllerTypeAttribute(string value)
    : ControllerRouteAttributeBase(value);
