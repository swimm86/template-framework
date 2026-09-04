// ----------------------------------------------------------------------------------------------
// <copyright file="InMemoryDbContextOptionsBuilderInitializer.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.Dal.Settings.Models.Base;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

internal sealed class InMemoryDbContextOptionsBuilderInitializer
    : IDbContextOptionsBuilderInitializer
{
    public void Initialize<TSettings>(
        DbContextOptionsBuilder options,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase
    {
        options.UseInMemoryDatabase($"ef-tests-{Guid.NewGuid():N}");
    }
}
