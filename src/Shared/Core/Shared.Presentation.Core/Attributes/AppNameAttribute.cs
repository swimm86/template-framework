// ----------------------------------------------------------------------------------------------
// <copyright file="AppNameAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Attributes.Base;
using Shared.Presentation.Core.Conventions;

namespace Shared.Presentation.Core.Attributes;

/// <summary>
/// Указывает имя приложения для использования в маршрутизации контроллеров.
/// </summary>
/// <remarks>
/// Применяется к базовому классу контроллера. Значение подставляется в маршрут
/// вместо плейсхолдера <c>[appName]</c>. Используется в конвенции
/// <see cref="ControllerTypeConvention"/>.
/// </remarks>
/// <param name="value">Имя приложения (например, "Template", "Bff", "Getter").</param>
/// <example>
/// <code>
/// [AppName("Template")]
/// [ControllerType("bff")]
/// public abstract class BffControllerBase { }
/// // Маршрут: api/template/bff/v1/{controller}
/// </code>
/// </example>
public class AppNameAttribute(string value)
    : ControllerRouteAttributeBase(value);