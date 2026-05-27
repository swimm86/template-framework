using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Cqrs.Core.Extensions;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Testing.DependencyInjection;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.Extensions;

/// <summary>
/// Тесты регистрации MediatR через <see cref="DependencyInjectionExtensions"/>: сборка провайдера и pipeline behaviors.
/// </summary>
public sealed class DependencyInjectionExtensionsTests
{
    /// <summary>
    /// ServiceProvider с MediatR собирается без ошибок.
    /// </summary>
    [Fact]
    public void AddMediatR_ServiceProvider_BuildsSuccessfully()
    {
        // Act
        using var provider = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton<IUnitOfWork>(_ => new FakeUnitOfWork());
            services.AddSingleton<IMapper>(_ => new FakeMapper());
            services.AddSingleton<IUserProvider>(_ => new FakeUserProvider());
            services.AddMediatR();
        });

        // Assert
        provider.Should().NotBeNull();
    }

    /// <summary>
    /// Регистрируются 2 pipeline behavior с <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    [Fact]
    public void AddMediatR_RegistersPipelineBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR();

        // Act
        var behaviorDescriptors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        // Assert
        behaviorDescriptors.Should().HaveCount(2);
        behaviorDescriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }
}
