// ----------------------------------------------------------------------------------------------
// <copyright file="FakeIDbSeeder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.DbSeeder.Interfaces;

namespace Shared.Testing.Doubles.Job;

/// <summary>
/// Тестовая реализация <see cref="IDbSeeder"/> для unit-тестов: подсчитывает
/// число вызовов <see cref="IDbSeeder.ApplySeedsAsync"/> и позволяет инжектировать
/// исключение или токен отмены.
/// </summary>
/// <remarks>
/// <para>
/// Используется в <c>DbSeederJobTests</c> для проверки идемпотентности выполнения
/// фоновой задачи и корректности проброса <see cref="CancellationToken"/>.
/// </para>
/// <para>
/// Не путать с реальной <c>DbSeeder</c> (из <c>Shared.Application.Core</c>):
/// этот тип не делает никаких обращений к БД и нужен исключительно для изоляции
/// тестируемого <see cref="IDbSeeder"/>-потребителя от внешних зависимостей.
/// </para>
/// </remarks>
public sealed class FakeIDbSeeder
    : IDbSeeder
{
    private readonly Exception? _exceptionToThrow;
    private readonly Func<CancellationToken, ValueTask>? _delay;

    /// <summary>
    /// Счётчик вызовов <see cref="ApplySeedsAsync"/>.
    /// </summary>
    public int ApplySeedsCallCount { get; private set; }

    /// <summary>
    /// Последний <see cref="CancellationToken"/>, переданный в <see cref="ApplySeedsAsync"/>.
    /// Используется для проверки корректности проброса токена.
    /// </summary>
    public CancellationToken? LastCancellationToken { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="FakeIDbSeeder"/>.
    /// </summary>
    /// <param name="exceptionToThrow">
    /// Исключение, которое <see cref="ApplySeedsAsync"/> выбросит при первом вызове.
    /// <c>null</c> — метод завершается успешно.
    /// </param>
    /// <param name="delay">
    /// Делегат задержки, вызываемый перед инкрементом счётчика. Используется для
    /// сценариев, где нужно дождаться параллельного вызова. <c>null</c> — без задержки.
    /// </param>
    public FakeIDbSeeder(
        Exception? exceptionToThrow = null,
        Func<CancellationToken, ValueTask>? delay = null)
    {
        _exceptionToThrow = exceptionToThrow;
        _delay = delay;
    }

    /// <inheritdoc />
    public async Task ApplySeedsAsync(CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;

        if (_delay is not null)
        {
            await _delay(cancellationToken);
        }

        if (_exceptionToThrow is not null)
        {
            throw _exceptionToThrow;
        }

        ApplySeedsCallCount++;
    }
}
