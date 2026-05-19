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

public sealed class DependencyInjectionExtensionsTests
{
    [Fact]
    public void AddMediatR_ServiceProvider_BuildsSuccessfully()
    {
        using var provider = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
            services.AddSingleton<IUnitOfWork>(_ => new FakeUnitOfWork());
            services.AddSingleton<IMapper>(_ => new FakeMapper());
            services.AddSingleton<IUserProvider>(_ => new FakeUserProvider());
            services.AddMediatR();
        });

        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddMediatR_RegistersPipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR();

        var behaviorDescriptors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(2);
        behaviorDescriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }
}
