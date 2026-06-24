// ----------------------------------------------------------------------------------------------
// <copyright file="DbSeederJobTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dal.DbSeeder.Interfaces;
using Shared.Application.Core.Job;
using Shared.Testing.Doubles.Job;

namespace Shared.Application.Core.Tests.Job;

/// <summary>
/// Тесты <see cref="DbSeederJob"/>: идемпотентность выполнения, проброс
/// <see cref="CancellationToken"/>, контрактное поведение при исключениях
/// и потокобезопасность (включая cross-scope, имитирующую несколько Quartz-trigger-ов).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DbSeederJob"/> использует <b>статический</b> int-флаг
/// <c>_seeded</c> + <see cref="Interlocked.CompareExchange(ref int, int, int)"/>
/// для обеспечения идемпотентности на уровне всего процесса — это переживает
/// любое количество <c>IServiceScope</c>-ов (которые Quartz создаёт на каждый
/// trigger fire).
/// </para>
/// <para>
/// <b>Следствие для тестов:</b> <c>_seeded</c> — глобальное состояние, поэтому
/// каждый <c>[Fact]</c> вызывает <c>DbSeederJob.ResetSeedFlag()</c> в Arrange —
/// иначе тесты зависят от порядка выполнения (12 принципов тестирования,
/// «Test Independence»).
/// </para>
/// </remarks>
public sealed class DbSeederJobTests
{
    /// <summary>
    /// Первый вызов <see cref="DbSeederJob.ExecuteAsync"/> приводит
    /// к одному вызову <see cref="IDbSeeder.ApplySeedsAsync"/>.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_FirstCall_InvokesSeederOnce()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        var seeder = new FakeIDbSeeder();
        var job = new DbSeederJob(seeder);

        // Act
        await job.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        seeder.ApplySeedsCallCount.Should().Be(1);
    }

    /// <summary>
    /// Повторный вызов <see cref="DbSeederJob.ExecuteAsync"/> на том же экземпляре
    /// НЕ приводит к повторному вызову <see cref="IDbSeeder.ApplySeedsAsync"/> —
    /// задача идемпотентна в пределах процесса.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SecondCallOnSameInstance_DoesNotInvokeSeeder()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        var seeder = new FakeIDbSeeder();
        var job = new DbSeederJob(seeder);

        // Act
        await job.ExecuteAsync(TestContext.Current.CancellationToken);
        await job.ExecuteAsync(TestContext.Current.CancellationToken);
        await job.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        seeder.ApplySeedsCallCount.Should().Be(1, "после первого выполнения задача помечается как completed");
    }

    /// <summary>
    /// <b>Cross-scope идемпотентность:</b> повторный вызов
    /// <see cref="DbSeederJob.ExecuteAsync"/> на <b>новом экземпляре</b>
    /// (имитация нового Quartz-trigger-а в новом <c>IServiceScope</c>) тоже
    /// НЕ приводит к повторному вызову <see cref="IDbSeeder.ApplySeedsAsync"/>.
    /// <para>
    /// Это ключевой инвариант: статический флаг <c>_seeded</c> живёт на уровне
    /// типа и переживает любое количество scope-ов и trigger-ов.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CrossScopeInvocation_DoesNotInvokeSeederAgain()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();

        var seeder1 = new FakeIDbSeeder();
        var job1 = new DbSeederJob(seeder1);

        // Act — первый scope (например, Quartz-trigger #1).
        await job1.ExecuteAsync(TestContext.Current.CancellationToken);

        // Arrange — новый scope (например, Quartz-trigger #2 или retry из middleware).
        var seeder2 = new FakeIDbSeeder();
        var job2 = new DbSeederJob(seeder2);

        // Act
        await job2.ExecuteAsync(TestContext.Current.CancellationToken);
        await job2.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert — только первый экземпляр дёрнул seeder, второй и третий
        // вызовы увидели статический флаг и вышли.
        seeder1.ApplySeedsCallCount.Should().Be(1);
        seeder2.ApplySeedsCallCount.Should().Be(0, "статический флаг блокирует вызовы из нового scope");
    }

    /// <summary>
    /// <see cref="CancellationToken"/>, переданный в <see cref="DbSeederJob.ExecuteAsync"/>,
    /// пробрасывается в <see cref="IDbSeeder.ApplySeedsAsync"/> без изменений.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ForwardsCancellationTokenToSeeder()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        var seeder = new FakeIDbSeeder();
        var job = new DbSeederJob(seeder);

        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        await job.ExecuteAsync(expectedToken);

        // Assert
        seeder.LastCancellationToken.Should().Be(expectedToken);
    }

    /// <summary>
    /// Контракт: если <see cref="IDbSeeder.ApplySeedsAsync"/> бросает исключение,
    /// <see cref="DbSeederJob"/> сбрасывает флаг идемпотентности — повторный вызов
    /// приведёт к новой попытке <see cref="IDbSeeder.ApplySeedsAsync"/>.
    /// <para>
    /// Это соответствует семантике in-process <see cref="Shared.Application.Core.Job.Pipeline.Middlewares.RetryMiddleware"/>:
    /// одна попытка <see cref="DbSeederJob"/> == один вызов <see cref="IDbSeeder.ApplySeedsAsync"/>,
    /// а решение «повторить ли» принимает middleware.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SeederThrows_AllowsRetryOnNextCall()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        var seeder = new FakeIDbSeeder(new InvalidOperationException("seed boom"));
        var job = new DbSeederJob(seeder);

        // Act + Assert — первая попытка пробрасывает исключение.
        await job.Invoking(j => j.ExecuteAsync(TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();

        // Act + Assert — вторая попытка (от лица retry-middleware) также бросает
        // исключение, потому что флаг был сброшен в catch.
        await job.Invoking(j => j.ExecuteAsync(TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();

        // Assert — FakeIDbSeeder.incremented count не изменился, т.к. оба вызова упали
        // до инкремента счётчика успешных вызовов.
        seeder.ApplySeedsCallCount.Should().Be(0);
    }

    /// <summary>
    /// <see cref="DbSeederJob"/> корректно резолвится через DI как scoped-сервис
    /// и получает свой <see cref="IDbSeeder"/> через конструктор.
    /// </summary>
    [Fact]
    public async Task DependencyInjection_RegistersDbSeederJobAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDbSeeder, FakeIDbSeeder>();
        services.AddScoped<DbSeederJob>();

        // Act
        await using var sp = services.BuildServiceProvider();
        using var scope1 = sp.CreateScope();
        using var scope2 = sp.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredService<DbSeederJob>();
        var second = scope2.ServiceProvider.GetRequiredService<DbSeederJob>();

        // Assert
        first.Should().NotBeSameAs(second, "scoped-регистрация даёт разные экземпляры в разных scope");
    }

    /// <summary>
    /// <b>Потокобезопасность:</b> параллельные вызовы <see cref="DbSeederJob.ExecuteAsync"/>
    /// из N потоков на <b>одном экземпляре</b> приводят ровно к одному вызову
    /// <see cref="IDbSeeder.ApplySeedsAsync"/>.
    /// <para>
    /// Реализация использует <see cref="Interlocked.CompareExchange(ref int, int, int)"/>:
    /// только один поток атомарно переводит флаг <c>0 → 1</c> и вызывает seeder,
    /// остальные видят <c>1</c> и сразу выходят.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConcurrentInvocationsOnSameInstance_InvokeSeederExactlyOnce()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        const int concurrency = 32;

        // Используем «долгий» seeder: пока один поток сидит в await ApplySeedsAsync,
        // другие потоки могут войти в guard.
        var seeder = new FakeIDbSeeder(
            exceptionToThrow: null,
            delay: ct => new ValueTask(Task.Delay(100, ct)));
        var job = new DbSeederJob(seeder);

        var barrier = new Barrier(concurrency);

        // Act — запускаем N параллельных вызовов через Barrier,
        // чтобы они стартовали максимально одновременно.
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                await job.ExecuteAsync(TestContext.Current.CancellationToken);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert — ровно один вызов ApplySeedsAsync, несмотря на конкуренцию.
        seeder.ApplySeedsCallCount.Should().Be(1);
    }

    /// <summary>
    /// <b>Потокобезопасность cross-scope:</b> параллельные вызовы
    /// <see cref="DbSeederJob.ExecuteAsync"/> из N потоков, каждый со своим
    /// экземпляром (имитация N Quartz-trigger-ов в разных scope), приводят
    /// ровно к одному вызову <see cref="IDbSeeder.ApplySeedsAsync"/> на одном
    /// из экземпляров.
    /// <para>
    /// Это самый строгий тест: проверяет, что статический <c>_seeded</c>
    /// обеспечивает single-execution-per-process даже при одновременных
    /// вызовах из разных scope-ов (что и происходит в production при
    /// параллельных Quartz-trigger-ах или в Quartz Cluster).
    /// </para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConcurrentInvocationsAcrossScopes_InvokeSeederExactlyOnce()
    {
        // Arrange
        DbSeederJob.ResetSeedFlag();
        const int concurrency = 32;

        var seeder = new FakeIDbSeeder(
            exceptionToThrow: null,
            delay: ct => new ValueTask(Task.Delay(100, ct)));
        var jobs = Enumerable.Range(0, concurrency)
            .Select(_ => new DbSeederJob(seeder))
            .ToArray();

        var barrier = new Barrier(concurrency);

        // Act — каждый поток работает со своим экземпляром (cross-scope).
        var tasks = Enumerable.Range(0, concurrency)
            .Select(i => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                await jobs[i].ExecuteAsync(TestContext.Current.CancellationToken);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert — только один экземпляр (любой из N) дёрнул seeder.
        seeder.ApplySeedsCallCount.Should().Be(1);
    }
}
