// ----------------------------------------------------------------------------------------------
// <copyright file="QuartzIntegrationCollection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Job.Quartz.Tests;

/// <summary>
/// Коллекция для интеграционных тестов, поднимающих настоящий Quartz-планировщик
/// поверх DI-контейнера: тесты внутри коллекции выполняются последовательно,
/// чтобы не было гонок за общий <c>quartz.properties</c>, общий
/// <c>ISchedulerFactory</c> и глобальный контекст сериализатора Quartz.
/// <para>
/// Симметрична <c>HangfireIntegrationCollection</c>: оба адаптера обязаны
/// проходить идентичный набор интеграционных тестов под одинаковой
/// изоляцией — иначе нельзя гарантировать Zero-Touch Proof
/// (смена Quartz ↔ Hangfire = 0 правок).
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class QuartzIntegrationCollection
{
    /// <summary>
    /// Имя коллекции для атрибута <see cref="Xunit.CollectionAttribute"/>.
    /// </summary>
    public const string Name = "Quartz Integration";
}
