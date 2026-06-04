// ----------------------------------------------------------------------------------------------
// <copyright file="HangfireIntegrationCollection.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Infrastructure.Job.Hangfire.Tests;

/// <summary>
/// Коллекция для интеграционных тестов, трогающих глобальное состояние Hangfire
/// (<c>JobStorage.Current</c>, <c>AspNetCoreLogProvider</c>): тесты внутри
/// коллекции выполняются последовательно, чтобы не было гонок за
/// глобальный <see cref="Microsoft.Extensions.Logging.LoggerFactory"/> и
/// <c>JobStorage.Current</c> (Hangfire dispose-ит пойманный
/// <see cref="Microsoft.Extensions.Logging.LoggerFactory"/> при остановке сервера).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HangfireIntegrationCollection
{
    /// <summary>
    /// Имя коллекции для атрибута <see cref="Xunit.CollectionAttribute"/>.
    /// </summary>
    public const string Name = "Hangfire Integration";
}
