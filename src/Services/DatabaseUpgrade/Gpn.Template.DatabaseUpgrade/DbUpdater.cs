// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Infrastructure.Dal.EFCore;

namespace Gpn.Template.DatabaseUpgrade;

/// <see />
public class DbUpdater(
    DbContext dbContext)
    : DbUpdaterBase(dbContext);
