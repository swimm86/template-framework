// ----------------------------------------------------------------------------------------------
// <copyright file="Includable.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Models;

/// <inheritdoc />
public class Includable<TProperty>(List<string> includes)
    : IIncludable<TProperty>
{
    /// <inheritdoc />
    public List<string> Includes { get; private set; } = includes;

    /// <inheritdoc />
    public void AddInclude(string include)
    {
        Includes.Add(include);
    }
}
