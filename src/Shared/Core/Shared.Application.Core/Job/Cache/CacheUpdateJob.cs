// ----------------------------------------------------------------------------------------------
// <copyright file="CacheUpdateJob.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Job.Interfaces;

namespace Shared.Application.Core.Job.Cache;

/// <summary>
/// Базовый класс фоновой задачи для периодического обновления кэша по расписанию.
/// <para>
/// Использует <see cref="Lazy{T}"/> для потокобезопасной инициализации данных:
/// первый вызов <see cref="GetCacheDataAsync"/> инициирует обновление, последующие —
/// возвращают уже кэшированное значение, пока <see cref="ExecuteAsync"/> не сбросит его.
/// </para>
/// </summary>
/// <typeparam name="TData">Тип данных кэша.</typeparam>
public abstract class CacheUpdateJob<TData>
    : IScheduledJob
{
    private readonly object _lazyLock = new();
    private Lazy<Task<TData>>? _lazy;

    /// <summary>
    /// Возвращает кэшированные данные. Если данные отсутствуют, инициирует обновление.
    /// Потокобезопасно: параллельные вызовы не приводят к множественным обновлениям.
    /// </summary>
    /// <returns>Задача с кэшированными данными.</returns>
    public Task<TData> GetCacheDataAsync()
    {
        lock (_lazyLock)
        {
            _lazy ??= new Lazy<Task<TData>>(() => UpdateDataAsync());
        }

        return _lazy.Value;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var data = await UpdateDataAsync(cancellationToken);

        lock (_lazyLock)
        {
            _lazy = new Lazy<Task<TData>>(Task.FromResult(data));
        }
    }

    /// <summary>
    /// Обновляет данные для кэша.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Обновленные данные для кэша.</returns>
    protected abstract Task<TData> UpdateDataAsync(CancellationToken cancellationToken = default);
}