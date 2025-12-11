// ----------------------------------------------------------------------------------------------
// <copyright file="EfDbSettingsBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Settings.Models.Base;

namespace Shared.Infrastructure.Dal.EFCore.Settings;

/// <summary>
/// Базовая конфигурация для бд.
/// </summary>
/// <typeparam name="TDbContext">Тип DbContext-а, для которого задана конфигурация.</typeparam>
public abstract class EfDbSettingsBase<TDbContext>
    : DbSettingsBase
    where TDbContext : DbContextBase;
