using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Core.Utils;
using Shared.Domain.Core.Utils.Extensions;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Domain.Core.Tests.Utils.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPropertyUtil_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddPropertyUtil();
        var provider = services.BuildServiceProvider();

        var util = provider.GetService<PropertyUtil>();
        var getter = provider.GetService<IPropertyGetter>();
        var setter = provider.GetService<IPropertySetter>();

        util.Should().NotBeNull();
        getter.Should().NotBeNull();
        setter.Should().NotBeNull();
    }

    [Fact]
    public void AddPropertyUtil_GetterAndSetter_ResolveToSameSingletonInstance()
    {
        var services = new ServiceCollection();
        services.AddPropertyUtil();
        var provider = services.BuildServiceProvider();

        var getter = provider.GetRequiredService<IPropertyGetter>();
        var setter = provider.GetRequiredService<IPropertySetter>();
        var util = provider.GetRequiredService<PropertyUtil>();

        getter.Should().BeSameAs(util);
        setter.Should().BeSameAs(util);
    }
}
