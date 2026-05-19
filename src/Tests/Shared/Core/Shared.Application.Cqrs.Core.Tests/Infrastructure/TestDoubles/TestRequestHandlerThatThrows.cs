using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestRequestHandlerThatThrows(ILoggerFactory loggerFactory)
    : RequestHandler<TestRequest, TestResponse>(loggerFactory)
{
    protected override Task GuardAsync(TestRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Guard failed");

    public Task CallGuardAsync(TestRequest request, CancellationToken cancellationToken = default)
        => GuardAsync(request, cancellationToken);

    public override Task<TestResponse> Handle(TestRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestResponse());
}
