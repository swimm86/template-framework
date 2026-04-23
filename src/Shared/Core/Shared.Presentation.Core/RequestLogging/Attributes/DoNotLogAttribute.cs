// ----------------------------------------------------------------------------------------------
// <copyright file="DoNotLogAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.RequestLogging.Filters;

namespace Shared.Presentation.Core.RequestLogging.Attributes;

/// <summary>
/// Атрибут для пометки свойств, которые не должны логироваться.
/// </summary>
/// <remarks>
/// Используется для защиты чувствительных данных (пароли, токены, и т.д.).
/// При логировании аргументов контроллера такие свойства заменяются на <see cref="RequestLoggingFilter.RedactedPlaceholder"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotLogAttribute
    : Attribute;
