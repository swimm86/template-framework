// ----------------------------------------------------------------------------------------------
// <copyright file="ProjectToIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;
using Shared.Infrastructure.Mapper.Tests.Integration;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests.Integration;

/// <summary>
/// Интеграционные тесты <see cref="IMapper.ProjectTo{TResult}"/> для AutoMapper.
/// Содержит только DI-специфичный код; общая логика тестов — в <see cref="ProjectToIntegrationTestBase"/>.
/// </summary>
public sealed class ProjectToIntegrationTests
    : ProjectToIntegrationTestBase
{
    protected override IMapper CreateMapper(MappingProfileBase additionalProfile)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(additionalProfile);

        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        injector.Inject(services);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
