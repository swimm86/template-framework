// ----------------------------------------------------------------------------------------------
// <copyright file="InjectorTestDbSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class InjectorTestDbSettings
    : EfDbSettingsBase<InjectorTestDbContext>;
