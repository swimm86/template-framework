using FluentValidation;
using FluentValidation.Results;
using Shared.Application.Cqrs.Core.Abstractions;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions;

/// <summary>
/// Тесты <see cref="RequestHandler{TRequest,TResponse}"/>.
/// Проверяют <c>GuardAsync</c> (no-op по умолчанию и проброс исключений)
/// и <c>ValidateAsync</c> (успешная валидация, агрегация ошибок, фильтрация null-ошибок).
/// </summary>
public sealed class RequestHandlerTests
{
    #region GuardAsync Tests

    /// <summary>
    /// Реализация <c>GuardAsync</c> по умолчанию — no-op,
    /// не выбрасывает исключений.
    /// </summary>
    [Fact]
    public async Task GuardAsync_DefaultImplementation_IsNoOp()
    {
        // Arrange
        var handler = new TestRequestHandler(new FakeLoggerFactory());

        // Act
        var act = () => handler.GuardAsync(new TestRequest());

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Если переопределённый <c>GuardAsync</c> выбрасывает исключение —
    /// оно пробрасывается наружу без перехвата.
    /// </summary>
    [Fact]
    public async Task GuardAsync_OverrideThrows_PropagatesException()
    {
        // Arrange
        var handler = new TestRequestHandlerThatThrows(new FakeLoggerFactory());

        // Act
        var act = () => handler.CallGuardAsync(new TestRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Guard failed");
    }

    #endregion

    #region ValidateAsync Tests

    /// <summary>
    /// Если все валидаторы проходят проверку —
    /// <c>ValidateAsync</c> не выбрасывает исключений.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_AllValid_DoesNotThrow()
    {
        // Arrange
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = new IValidator<TestRequest>[]
        {
            new FakeValidator<TestRequest>(),
            new FakeValidator<TestRequest>(),
        };

        // Act
        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Если хотя бы один валидатор фейлится —
    /// выбрасывается <see cref="ValidationException"/> с агрегированными ошибками.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_OneFails_ThrowsValidationException_WithAggregatedErrors()
    {
        // Arrange
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = new IValidator<TestRequest>[]
        {
            new FakeValidator<TestRequest>(),
            new FakeValidator<TestRequest>([new ValidationFailure("Name", "Name is required")]),
        };

        // Act
        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 1);
    }

    /// <summary>
    /// Если передан пустой массив валидаторов —
    /// <c>ValidateAsync</c> завершается без ошибок.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_EmptyValidators_DoesNotThrow()
    {
        // Arrange
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = Array.Empty<IValidator<TestRequest>>();

        // Act
        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Если валидатор возвращает <c>null</c> в списке ошибок —
    /// такие элементы фильтруются и не вызывают <c>NullReferenceException</c>.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ValidatorWithNullErrors_FiltersThemOut()
    {
        // Arrange
        var handler = new TestRequestHandler(new FakeLoggerFactory());
        var validators = new IValidator<TestRequest>[]
        {
            new FakeValidator<TestRequest>([null!]),
        };

        // Act
        var act = () => handler.ValidateAsync(new TestRequest(), validators);

        // Assert
        await act.Should().NotThrowAsync<ValidationException>();
    }

    #endregion
}
