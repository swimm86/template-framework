using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;

using IMapper = Shared.Domain.Core.Mapping.Interfaces.IMapper;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests.DependencyInjection;

public sealed class DependencyInjectorTests
{
    [Fact]
    public void Process_RegistersAutoMapper()
    {
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);

        injector.Inject(services);

        services.Should().Contain(sd => sd.ServiceType == typeof(IMapper));
        services.Should().Contain(sd => sd.ServiceType == typeof(IConfigurationProvider));
    }

    [Fact]
    public void Process_RegistersMapperAsSingleton()
    {
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);

        injector.Inject(services);

        var registration = services.SingleOrDefault(sd => sd.ServiceType == typeof(IMapper));
        registration.Should().NotBeNull();
        registration!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
