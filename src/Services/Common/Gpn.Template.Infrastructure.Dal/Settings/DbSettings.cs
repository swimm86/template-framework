// ----------------------------------------------------------------------------------------------
// <copyright file="DbSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
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
