// ----------------------------------------------------------------------------------------------
// <copyright file="Includable.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Specification.Interfaces;

namespace Shared.Application.Core.Dal.Specification.Models;

/// <inheritdoc />
public class Includable<TProperty>(List<string> includes) : IIncludable<TProperty>
{
    /// <inheritdoc />
    public List<string> Includes { get; private set; } = includes;

    /// <inheritdoc />
    public void AddInclude(string include)
    {
        Includes.Add(include);
    }
}
