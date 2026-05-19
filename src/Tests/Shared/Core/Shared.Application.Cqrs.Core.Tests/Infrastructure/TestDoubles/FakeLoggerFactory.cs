using Microsoft.Extensions.Logging;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class FakeLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName) => new FakeLogger();

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
