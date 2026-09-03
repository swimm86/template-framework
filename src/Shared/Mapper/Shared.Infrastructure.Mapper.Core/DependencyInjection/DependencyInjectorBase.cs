// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection.Base;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Core.DependencyInjection;

/// <summary>
/// Общий базовый класс для регистрации DI-зависимостей слоёв провайдеро-специфичных преобразований (mapping).
/// </summary>
/// <remarks>
/// Выносит общую логику (сканирование профилей преобразования, регистрация <see cref="IMapper"/>)
/// из подклассов <c>DependencyInjector</c>. Подклассы реализуют только провайдеро-специфичные
/// шаги: <see cref="BuildConfig"/> и <see cref="RegisterConfig"/>.
/// <para><inheritdoc cref="DependencyInjectorBase" path="/remarks"/></para>
/// </remarks>
/// <typeparam name="TConfig">Тип провайдеро-специфичной конфигурации маппинга.</typeparam>
/// <typeparam name="TMapper">Тип реализации <see cref="IMapper"/> для регистрации в DI.</typeparam>
/// <param name="loggerFactory"><inheritdoc cref="DependencyInjectorBase(ILoggerFactory)" path="/param[@name='loggerFactory']"/></param>
internal abstract class DependencyInjectorBase<TConfig, TMapper>(
    ILoggerFactory loggerFactory)
    : DependencyInjectorBase(loggerFactory)
    where TConfig : class
    where TMapper : class, IMapper
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        var profiles = GetMappingProfiles();
        var config = BuildConfig(profiles);
        return RegisterConfig(serviceCollection, config)
            .AddSingleton<IMapper, TMapper>();
    }

    /// <summary>
    /// Строит провайдеро-специфичную конфигурацию маппинга из собранных профилей.
    /// </summary>
    /// <param name="profiles">Профили маппинга, найденные в загруженных сборках.</param>
    /// <returns>Конфигурация, готовая к регистрации в DI.</returns>
    protected abstract TConfig BuildConfig(IReadOnlyCollection<IMappingProfile> profiles);

    /// <summary>
    /// Регистрирует построенную конфигурацию и провайдеро-специфичные сервисы в DI.
    /// </summary>
    /// <param name="serviceCollection">Коллекция сервисов.</param>
    /// <param name="config">Конфигурация, построенная <see cref="BuildConfig"/>.</param>
    /// <returns>Та же коллекция для fluent-цепочки.</returns>
    protected abstract IServiceCollection RegisterConfig(
        IServiceCollection serviceCollection,
        TConfig config);

    private static IMappingProfile[] GetMappingProfiles() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(IsMappingProfileCandidate)
            .Select(CreateProfile)
            .Select(profile => profile!)
            .ToArray();

    private static bool IsMappingProfileCandidate(Type type) =>
        typeof(IMappingProfile).IsAssignableFrom(type)
        && type is { IsAbstract: false, IsInterface: false }
        && type.GetConstructor(Type.EmptyTypes) is not null;

    private static IMappingProfile CreateProfile(Type type) =>
        (IMappingProfile)Activator.CreateInstance(type)!;
}
