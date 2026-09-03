// ----------------------------------------------------------------------------------------------
// <copyright file="IMemberMapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping.Description.Interfaces;

/// <summary>
/// Описание настройки члена преобразования (mapping).
/// </summary>
internal interface IMemberMapDescription
{
    /// <summary>
    /// Имя целевого свойства.
    /// </summary>
    string DestinationMemberName { get; }

    /// <summary>
    /// Тип целевого свойства.
    /// </summary>
    Type DestinationMemberType { get; }

    /// <summary>
    /// Игнорируется ли свойство.
    /// </summary>
    bool IsIgnored { get; }

    /// <summary>
    /// Лямбда-источник значения (если <c>MapFrom</c>).
    /// </summary>
    LambdaExpression? SourceExpression { get; }

    /// <summary>
    /// Лямбда-источник значения с доступом к параметрам <see cref="IMapper.ProjectTo{TResult}(IQueryable, object?)"/>.
    /// </summary>
    LambdaExpression? ParameterizedSourceExpression { get; }
}
