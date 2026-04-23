// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeeder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Infrastructure.Dal.EFCore;

namespace Gpn.Template.DatabaseUpgrade;

/// <see />
public class DbUpdater(
    DbContext dbContext)
    : DbUpdaterBase(dbContext);
