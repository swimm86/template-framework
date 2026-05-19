using FluentValidation;
using FluentValidation.Results;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions;

public sealed class RequestHandlerTests
{
    [Fact]
    public async Task GuardAsync_DefaultImplementation_IsNoOp()
    {
        var handler = new TestRequestHandler(new FakeLoggerFactory());

        var act = () => handler.GuardAsync(new TestRequest());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GuardAsync_OverrideThrows_PropagatesException()
    {
        var handler = new TestRequestHandlerThatThrows(new FakeLoggerFactory());

        var act = () => handler.CallGuardAsync(new TestRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Guard failed");
    }

    [Fact]
    public async Task ValidateAsync_AllValid_DoesNotThrow()
    {
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = new IValidator<TestRequest>[]
        {
            new FakeValidator<TestRequest>(),
            new FakeValidator<TestRequest>()
        };

        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_OneFails_ThrowsValidationException_WithAggregatedErrors()
    {
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = new IValidator<TestRequest>[]
        {
            new FakeValidator<TestRequest>(),
            new FakeValidator<TestRequest>([new ValidationFailure("Name", "Name is required")])
        };

        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 1);
    }

    [Fact]
    public async Task ValidateAsync_EmptyValidators_DoesNotThrow()
    {
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = Array.Empty<IValidator<TestRequest>>();

        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        await act.Should().NotThrowAsync();
    }
}
