using FluentValidation;
using FluentValidation.Results;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class FakeValidator<T> : AbstractValidator<T>
{
    private readonly List<ValidationFailure> _failures;

    public FakeValidator()
        : this([])
    {
    }

    public FakeValidator(List<ValidationFailure> failures)
    {
        _failures = failures;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = default)
        => Task.FromResult(new ValidationResult(_failures));

    public override ValidationResult Validate(ValidationContext<T> context)
        => new(_failures);
}
