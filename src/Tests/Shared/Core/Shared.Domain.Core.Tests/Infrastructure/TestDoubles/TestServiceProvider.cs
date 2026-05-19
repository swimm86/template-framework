namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
