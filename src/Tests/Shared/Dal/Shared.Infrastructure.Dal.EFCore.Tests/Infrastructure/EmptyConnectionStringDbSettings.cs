using System.Diagnostics.CodeAnalysis;
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Тестовая конфигурация БД с пустой строкой подключения.
/// Используется для проверки валидации <c>DbContextOptionsBuilderInitializer</c>.
/// </summary>
public sealed class EmptyConnectionStringDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public EmptyConnectionStringDbSettings()
    {
        ConnectionString = string.Empty;
        TransactionsEnabled = true;
    }
}

/// <summary>
/// Тестовая конфигурация БД с whitespace-строкой подключения.
/// </summary>
public sealed class WhitespaceConnectionStringDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public WhitespaceConnectionStringDbSettings()
    {
        ConnectionString = "   ";
        TransactionsEnabled = true;
    }
}

/// <summary>
/// Тестовая конфигурация БД с включённым <c>EnableSensitiveDataLogging</c>.
/// </summary>
public sealed class SensitiveLoggingEnabledDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public SensitiveLoggingEnabledDbSettings()
    {
        ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";
        TransactionsEnabled = true;
        EnableSensitiveDataLogging = true;
    }
}

/// <summary>
/// Тестовая конфигурация БД с выключенным <c>EnableSensitiveDataLogging</c>.
/// </summary>
public sealed class SensitiveLoggingDisabledDbSettings
    : EfDbSettingsBase<TestDbContext>
{
    [SetsRequiredMembers]
    public SensitiveLoggingDisabledDbSettings()
    {
        ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";
        TransactionsEnabled = true;
        EnableSensitiveDataLogging = false;
    }
}
