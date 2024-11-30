// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Infrastructure.Dal.EFCore;

namespace Gpn.Template.DatabaseUpgrade;

/// <summary>
/// Реализация <see cref="IDbSeeder"/>.
/// </summary>
/// <param name="dbContextFactory">Фабрика DbContext-ов.</param>
public class DbSeeder(
    IDbContextFactory<DbContext> dbContextFactory,
    IUnitOfWork unitOfWork)
    : DbSeederBase<DbContext>(dbContextFactory, unitOfWork);
