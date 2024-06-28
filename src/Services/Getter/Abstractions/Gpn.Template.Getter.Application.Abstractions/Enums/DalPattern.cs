// ----------------------------------------------------------------------------------------------
// <copyright file="DalPattern.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Gpn.Template.Getter.Application.Abstractions.Enums;

/// <summary>
/// Dal-патерны.
/// </summary>
public enum DalPattern
{
    /// <summary>
    /// UnitOfWork.
    /// </summary>
    UnitOfWork,

    /// <summary>
    /// Repository.
    /// </summary>
    Repository,

    /// <summary>
    /// Specification.
    /// </summary>
    Specification,
}
