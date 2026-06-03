// ----------------------------------------------------------------------------------------------
// <copyright file="MockKeyedServiceProvider.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Shared.Testing.Doubles.DependencyInjection;

/// <summary>
/// Тестовая in-memory реализация <see cref="IKeyedServiceProvider"/>.
/// Хранит заранее зарегистрированные пары <c>(тип, ключ) → экземпляр</c> и
/// возвращает их при <c>GetKeyedService</c>. Метод <c>GetService</c> всегда
/// возвращает <c>null</c> — это намеренное поведение, отделяющее keyed-резолв
/// от обычного и позволяющее в тестах однозначно проверять, по какому пути
/// пошёл SUT.
/// </summary>
/// <remarks>
/// Используется в тестах адаптеров фоновых задач
/// (<c>HangfireScheduledJobAdapter</c>, <c>QuartzScheduledJobAdapter</c>),
/// где <c>IServiceProvider</c> реально реализует
/// <see cref="IKeyedServiceProvider"/> и SUT выбирает между обычным и
/// keyed-резолвом в зависимости от наличия <c>serviceKey</c>.
/// </remarks>
public sealed class MockKeyedServiceProvider
    : IKeyedServiceProvider
{
    private readonly Dictionary<(Type Type, object? Key), object?> _services = new();

    /// <summary>
    /// Число вызовов <see cref="GetKeyedService(Type, object?)"/>.
    /// Используется в тестах для проверки, что SUT действительно пошёл
    /// по keyed-пути, а не по обычному <see cref="GetService(Type)"/>.
    /// </summary>
    public int GetKeyedCallCount { get; private set; }

    /// <summary>
    /// Регистрирует <paramref name="instance"/> под парой
    /// <c>(<paramref name="type"/>, <paramref name="key"/>)</c>.
    /// Повторная регистрация той же пары перезаписывает предыдущее значение.
    /// </summary>
    /// <param name="type">Тип сервиса.</param>
    /// <param name="key">Ключ keyed-сервиса. <c>null</c> допустим.</param>
    /// <param name="instance">Экземпляр, возвращаемый при резолве.</param>
    public void Register(Type type, object? key, object? instance) =>
        _services[(type, key)] = instance;

    /// <summary>
    /// Всегда возвращает <c>null</c>. Намеренное поведение: тест-кейсы,
    /// требующие обычного (не keyed) резолва, регистрируют сервисы через
    /// реальный <c>ServiceCollection</c> и используют обычный
    /// <c>IServiceProvider</c>. Этот mock существует только для keyed-сценариев.
    /// </summary>
    /// <param name="serviceType">Тип сервиса (игнорируется).</param>
    /// <returns>Всегда <c>null</c>.</returns>
    public object? GetService(Type serviceType) => null;

    /// <summary>
    /// Возвращает экземпляр, зарегистрированный под парой
    /// <c>(<paramref name="serviceType"/>, <paramref name="serviceKey"/>)</c>,
    /// либо <c>null</c>, если регистрация отсутствует.
    /// </summary>
    /// <param name="serviceType">Тип сервиса.</param>
    /// <param name="serviceKey">Ключ keyed-сервиса.</param>
    /// <returns>
    /// Зарегистрированный экземпляр или <c>null</c>, если пара не найдена.
    /// Каждый вызов инкрементирует <see cref="GetKeyedCallCount"/>.
    /// </returns>
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        GetKeyedCallCount++;
        return _services.TryGetValue((serviceType, serviceKey), out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Возвращает экземпляр по паре <c>(тип, ключ)</c> либо бросает
    /// <see cref="InvalidOperationException"/>, если регистрация отсутствует.
    /// Делегирует <see cref="GetKeyedService(Type, object?)"/> и потому
    /// тоже инкрементирует <see cref="GetKeyedCallCount"/>.
    /// </summary>
    /// <param name="serviceType">Тип сервиса.</param>
    /// <param name="serviceKey">Ключ keyed-сервиса.</param>
    /// <returns>Зарегистрированный экземпляр.</returns>
    /// <exception cref="InvalidOperationException">
    /// Бросается, когда для пары <c>(<paramref name="serviceType"/>, <paramref name="serviceKey"/>)</c>
    /// нет регистрации.
    /// </exception>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        GetKeyedService(serviceType, serviceKey)
        ?? throw new InvalidOperationException(
            $"{nameof(MockKeyedServiceProvider)}: service {serviceType.FullName}#{serviceKey ?? "<null>"} not registered.");
}
