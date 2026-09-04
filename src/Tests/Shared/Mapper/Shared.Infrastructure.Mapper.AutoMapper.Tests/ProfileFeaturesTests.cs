// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;
using Shared.Infrastructure.Mapper.Tests.Profile;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

/// <summary>
/// Тесты fluent API маппера для AutoMapper. Содержит только DI-специфичный код;
/// общая логика тестов — в <see cref="ProfileFeaturesTestBase"/>.
/// </summary>
public sealed class ProfileFeaturesTests
    : ProfileFeaturesTestBase
{
    protected override IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        injector.Inject(services);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
