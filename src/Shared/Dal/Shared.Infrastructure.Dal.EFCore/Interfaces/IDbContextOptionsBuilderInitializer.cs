// ----------------------------------------------------------------------------------------------
// <copyright file="IDbContextOptionsBuilderInitializer.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Interfaces;

/// <summary>
/// Интерфейс инициализатора построителя опций контекста данных.
/// </summary>
public interface IDbContextOptionsBuilderInitializer
{
    /// <summary>
    /// Инициализирует построитель опций контекста данных.
    /// </summary>
    /// <typeparam name="TSettings">Тип настроек для базы данных.</typeparam>
    /// <param name="options">Построитель опций контекста данных.</param>
    /// <param name="migrationAssemblyName">Название сборки, в которой хранятся миграции.</param>
    void Initialize<TSettings>(
        DbContextOptionsBuilder options,
        string migrationAssemblyName)
        where TSettings : DbSettingsBase;
}
