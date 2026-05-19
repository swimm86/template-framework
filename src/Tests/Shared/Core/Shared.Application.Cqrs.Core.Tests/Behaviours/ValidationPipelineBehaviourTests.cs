using FluentValidation;
using FluentValidation.Results;
using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Cqrs.Core.Tests.Behaviours;

public sealed class ValidationPipelineBehaviourTests
{
    private sealed class AlwaysValidValidator : AbstractValidator<TestRequest>
    {
    }

    private sealed class FailingValidator : AbstractValidator<TestRequest>
    {
        public FailingValidator(string propertyName, string errorMessage)
        {
            RuleFor(x => x.Name).Must(_ => false).WithMessage(errorMessage);
        }
    }

    private sealed class ControlledValidator : AbstractValidator<TestRequest>
    {
        private readonly TaskCompletionSource<bool> _started;
        private readonly TaskCompletionSource<bool> _complete;
        private readonly string _propertyName;
        private readonly string _errorMessage;

        public ControlledValidator(
            TaskCompletionSource<bool> started,
            TaskCompletionSource<bool> complete,
            string propertyName = "Name",
            string errorMessage = "error")
        {
            _started = started;
            _complete = complete;
            _propertyName = propertyName;
            _errorMessage = errorMessage;
        }

        public override async Task<ValidationResult> ValidateAsync(
            ValidationContext<TestRequest> context,
            CancellationToken cancellation = default)
        {
            _started.TrySetResult(true);
            await _complete.Task;
            return new ValidationResult(new[]
            {
                new ValidationFailure(_propertyName, _errorMessage),
            });
        }
    }

    private static ValidationPipelineBehaviour<TestRequest, TestResponse> CreateSut(
        params IValidator<TestRequest>[] validators)
    {
        var logger = new FakeLogger<ValidationPipelineBehaviour<TestRequest, TestResponse>>(new FakeLogger());
        return new ValidationPipelineBehaviour<TestRequest, TestResponse>(logger, validators);
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }

        var result = await sut.Handle(request, Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be(response);
    }

    [Fact]
    public async Task Handle_AllValid_CallsNext()
    {
        var sut = CreateSut(new AlwaysValidValidator());
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }

        var result = await sut.Handle(request, Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be(response);
    }

    [Fact]
    public async Task Handle_OneFails_ThrowsValidationException()
    {
        var sut = CreateSut(new FailingValidator("Name", "must not be empty"));
        var request = new TestRequest();
        var response = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(response);

        var action = () => sut.Handle(request, Next, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_MultipleValidators_AggregatesErrors()
    {
        var sut = CreateSut(
            new FailingValidator("Name", "error A"),
            new FailingValidator("Name", "error B"));
        var request = new TestRequest();
        var response = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(response);

        var action = () => sut.Handle(request, Next, CancellationToken.None);

        var ex = await action.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
        ex.Which.Errors.Select(e => e.ErrorMessage).Should().Contain(["error A", "error B"]);
    }

    [Fact]
    public async Task Handle_DuplicateErrors_AreFiltered()
    {
        var sut = CreateSut(
            new FailingValidator("Name", "same error"),
            new FailingValidator("Name", "same error"));
        var request = new TestRequest();
        var response = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(response);

        var action = () => sut.Handle(request, Next, CancellationToken.None);

        var ex = await action.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Name");
    }

    [Fact]
    public async Task Handle_ValidatorsAreRunInParallel()
    {
        var started1 = new TaskCompletionSource<bool>();
        var complete1 = new TaskCompletionSource<bool>();
        var started2 = new TaskCompletionSource<bool>();
        var complete2 = new TaskCompletionSource<bool>();

        var sut = CreateSut(
            new ControlledValidator(started1, complete1, "A", "error A"),
            new ControlledValidator(started2, complete2, "B", "error B"));
        var request = new TestRequest();
        var response = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(response);

        var handleTask = sut.Handle(request, Next, CancellationToken.None);

        await Task.WhenAll(started1.Task, started2.Task);

        complete1.TrySetResult(true);
        complete2.TrySetResult(true);

        var action = () => handleTask;
        await action.Should().ThrowAsync<ValidationException>();
    }
}
