// ----------------------------------------------------------------------------------------------
// <copyright file="ICtorParamMapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;

namespace Shared.Domain.Core.Mapping.Description.Interfaces;

/// <summary>
/// Описание настройки параметра конструктора.
/// </summary>
internal interface ICtorParamMapDescription
{
    /// <summary>
    /// Имя параметра конструктора.
    /// </summary>
    string CtorParamName { get; }

    /// <summary>
    /// Лямбда-источник значения.
    /// </summary>
    LambdaExpression? SourceExpression { get; }
}
