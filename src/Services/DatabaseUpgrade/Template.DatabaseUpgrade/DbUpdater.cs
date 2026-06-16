// ----------------------------------------------------------------------------------------------
// <copyright file="DbUpdater.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbUpdater.Interfaces;
using Shared.Infrastructure.Dal.EFCore;

namespace Template.DatabaseUpgrade;

/// <summary>
/// Обновлятор базы данных, делегирующий инициализацию схемы <see cref="IEnsureSchemaStrategy"/>.
/// </summary>
public class DbUpdater(
    DbContext dbContext,
    IEnsureSchemaStrategy ensureSchemaStrategy)
    : DbUpdaterBase(dbContext, ensureSchemaStrategy);
