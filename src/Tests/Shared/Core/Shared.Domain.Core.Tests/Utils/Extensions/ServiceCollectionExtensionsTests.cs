using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Core.Utils;
using Shared.Domain.Core.Utils.Extensions;
using Shared.Domain.Core.Utils.Interfaces;

namespace Shared.Domain.Core.Tests.Utils.Extensions;

/// <summary>
/// Тесты для <see cref="ServiceCollectionExtensions.AddPropertyUtil"/> — регистрации <see cref="PropertyUtil"/> и связанных интерфейсов в DI-контейнере.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Проверяет, что <see cref="ServiceCollectionExtensions.AddPropertyUtil"/> регистрирует <see cref="PropertyUtil"/>, <see cref="IPropertyGetter"/> и <see cref="IPropertySetter"/>.
    /// </summary>
    [Fact]
    public void AddPropertyUtil_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPropertyUtil();
        var provider = services.BuildServiceProvider();

        var util = provider.GetService<PropertyUtil>();
        var getter = provider.GetService<IPropertyGetter>();
        var setter = provider.GetService<IPropertySetter>();

        // Assert
        util.Should().NotBeNull();
        getter.Should().NotBeNull();
        setter.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что <see cref="IPropertyGetter"/> и <see cref="IPropertySetter"/> разрешаются в тот же singleton-экземпляр, что и <see cref="PropertyUtil"/>.
    /// </summary>
    [Fact]
    public void AddPropertyUtil_GetterAndSetter_ResolveToSameSingletonInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPropertyUtil();
        var provider = services.BuildServiceProvider();

        var getter = provider.GetRequiredService<IPropertyGetter>();
        var setter = provider.GetRequiredService<IPropertySetter>();
        var util = provider.GetRequiredService<PropertyUtil>();

        // Assert
        getter.Should().BeSameAs(util);
        setter.Should().BeSameAs(util);
    }
}
