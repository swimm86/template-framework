using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestRequestHandler(ILoggerFactory loggerFactory)
    : RequestHandler<TestRequest, TestResponse>(loggerFactory)
{
    public new Task GuardAsync(TestRequest request, CancellationToken cancellationToken = default)
        => base.GuardAsync(request, cancellationToken);

    public new Task ValidateAsync<TEntity>(
        TEntity entity,
        IEnumerable<IValidator<TEntity>> validators,
        CancellationToken cancellationToken = default)
        => base.ValidateAsync(entity, validators, cancellationToken);

    public override Task<TestResponse> Handle(TestRequest query, CancellationToken cancellationToken)
        => Task.FromResult(new TestResponse());
}
