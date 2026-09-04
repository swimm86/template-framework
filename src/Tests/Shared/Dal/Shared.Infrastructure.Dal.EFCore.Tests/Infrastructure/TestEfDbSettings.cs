// ----------------------------------------------------------------------------------------------
// <copyright file="TestEfDbSettings.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class TestEfDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public TestEfDbSettings()
    {
        ConnectionString = "Server=localhost;Database=test;";
        TransactionsEnabled = true;
        EnableSensitiveDataLogging = false;
    }

    [SetsRequiredMembers]
    public TestEfDbSettings(bool transactionsEnabled)
    {
        ConnectionString = "Server=localhost;Database=test;";
        TransactionsEnabled = transactionsEnabled;
        EnableSensitiveDataLogging = false;
    }
}
