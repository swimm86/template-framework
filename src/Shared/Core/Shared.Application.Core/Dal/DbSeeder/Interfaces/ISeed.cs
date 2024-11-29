// ----------------------------------------------------------------------------------------------
// <copyright file="ISeed.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

namespace Shared.Application.Core.Dal.DbSeeder.Interfaces;

/// <summary>
/// Интерфейс для seed-ов.
/// </summary>
public interface ISeed
{
    /// <summary>
    /// Реализуеты seed-процесс.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task SeedAsync(IUnitOfWork unitOfWork);
}
