// ----------------------------------------------------------------------------------------------
// <copyright file="DalPattern.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
