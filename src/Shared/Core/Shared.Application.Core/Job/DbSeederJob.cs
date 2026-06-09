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
/// <remarks>
/// Гарантирует <b>не более одного выполнения</b> <see cref="IDbSeeder.ApplySeedsAsync"/>
/// за время жизни процесса — даже при конкурентных вызовах.
/// </remarks>
/// <param name="seeder">Сервис применения seed-ов.</param>
public sealed class DbSeederJob(
    IDbSeeder seeder)
    : IScheduledJob
{
    private static int _seeded;

    /// <inheritdoc />
    public async Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _seeded, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await seeder.ApplySeedsAsync(cancellationToken);
        }
        catch
        {
            Interlocked.Exchange(ref _seeded, 0);
            throw;
        }
    }

    /// <summary>
    /// Сбрасывает статический флаг идемпотентности.
    /// Используется только в unit-тестах (<c>Shared.Application.Core.Tests</c>),
    /// где каждый <c>[Fact]</c> должен стартовать из «чистого» состояния.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В production-коде вызывать не нужно: реальная задача выполняется
    /// ровно один раз за время жизни процесса.
    /// </para>
    /// <para>
    /// Помечен <c>internal</c>, чтобы случайное использование из бизнес-кода
    /// приводило к ошибке компиляции.
    /// </para>
    /// </remarks>
    internal static void ResetSeedFlag() => Interlocked.Exchange(ref _seeded, 0);
}
