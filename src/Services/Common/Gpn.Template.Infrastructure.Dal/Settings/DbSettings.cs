// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Gpn.Template.Infrastructure.Dal.Settings;

/// <summary>
/// Настройки подключения к БД.
/// </summary>
public class DbSettings : EfDbSettingsBase<DbContext>
{
}
