// ----------------------------------------------------------------------------------------------
// <copyright file="MemberMapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Description.Interfaces;

namespace Shared.Domain.Core.Mapping.Description;

/// <summary>
/// Внутренняя реализация <see cref="IMemberMapDescription"/>.
/// </summary>
internal sealed class MemberMapDescription(
    string destinationMemberName,
    Type destinationMemberType)
    : IMemberMapDescription
{
    /// <inheritdoc />
    public string DestinationMemberName { get; } = destinationMemberName;

    /// <inheritdoc />
    public Type DestinationMemberType { get; } = destinationMemberType;

    /// <inheritdoc />
    public bool IsIgnored { get; set; }

    /// <inheritdoc />
    public LambdaExpression? SourceExpression { get; set; }

    /// <inheritdoc />
    public LambdaExpression? ParameterizedSourceExpression { get; set; }
}
