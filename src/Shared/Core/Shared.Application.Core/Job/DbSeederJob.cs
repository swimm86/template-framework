// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Application.Core.Job.Interfaces;

namespace Shared.Application.Core.Job;

/// <summary>
/// Фоновая задача, выполняющая применение seed-процессов при старте приложения.
/// </summary>
/// <param name="seeder">Сервис применения seed-ов.</param>
public sealed class DbSeederJob(
    IDbSeeder seeder)
    : IScheduledJob
{
    /// <inheritdoc />
    public Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return seeder.ApplySeedsAsync(cancellationToken);
    }
}
