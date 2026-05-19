using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Cqrs.Core.Extensions;
using Shared.Testing.DependencyInjection;

namespace Shared.Application.Cqrs.Core.Tests.Extensions;

public sealed class DependencyInjectionExtensionsTests
{
    [Fact]
    public void AddMediatR_ServiceProvider_BuildsSuccessfully()
    {
        using var provider = ServiceProviderBuilder.Build(services =>
        {
            services.AddLogging();
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
