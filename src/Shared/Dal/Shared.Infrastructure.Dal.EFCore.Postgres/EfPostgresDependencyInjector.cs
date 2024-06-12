// ----------------------------------------------------------------------------------------------
// <copyright file="EfPostgresDependencyInjector.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DAL.EFCore;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Postgres;

/// <summary>
/// Класс для внедрения зависимостей EF Postgres.
/// </summary>
/// <param name="logger">Логгер для записи информации.</param>
public class EfPostgresDependencyInjector(
    ILogger<EfPostgresDependencyInjector> logger)
    : EfCoreDependencyInjectorBase(logger)
{
    /// <summary>
    /// Переопределенный метод для обработки сервисов при внедрении зависимостей EF Postgres.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов для обработки.</param>
    /// <returns>Измененная коллекция сервисов после обработки.</returns>
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        serviceCollection
            .AddTransient<IDbContextOptionsBuilderInitializer, DbContextOptionsBuilderInitializer>();
        base.Process(serviceCollection);
        return serviceCollection;
    }
}
