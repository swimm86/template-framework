// ----------------------------------------------------------------------------------------------
// <copyright file="EfDbSettingsBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Settings;

/// <summary>
/// Базовая конфигурация для бд.
/// </summary>
public abstract class EfDbSettingsBase<TDbContext>
    : DbSettingsBase
    where TDbContext : DbContextBase
{
}
