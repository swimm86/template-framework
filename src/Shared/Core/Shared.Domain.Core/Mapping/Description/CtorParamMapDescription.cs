// ----------------------------------------------------------------------------------------------
// <copyright file="CtorParamMapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Description.Interfaces;

namespace Shared.Domain.Core.Mapping.Description;

/// <summary>
/// Внутренняя реализация <see cref="ICtorParamMapDescription"/>.
/// </summary>
/// <param name="ctorParamName">Имя параметра конструктора.</param>
internal sealed class CtorParamMapDescription(
    string ctorParamName)
    : ICtorParamMapDescription
{
    /// <inheritdoc />
    public string CtorParamName { get; } = ctorParamName;

    /// <inheritdoc />
    public LambdaExpression? SourceExpression { get; set; }
}
