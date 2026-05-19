using FluentValidation;
using FluentValidation.Results;
using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Cqrs.Core.Tests.Behaviours;

/// <summary>
/// Тесты <see cref="ValidationPipelineBehaviour{TRequest,TResponse}"/>.
/// Проверяют валидацию запросов через FluentValidation:
/// вызов <c>Next</c> при отсутствии/успехе валидаторов,
/// агрегацию и дедупликацию ошибок, параллельный запуск валидаторов.
/// </summary>
public sealed class ValidationPipelineBehaviourTests
{
    /// <summary>
    /// Валидатор, который всегда проходит проверку.
    /// </summary>
    private sealed class AlwaysValidValidator
        : AbstractValidator<TestRequest>;

    /// <summary>
    /// Валидатор, который всегда фейлится с заданным сообщением.
    /// </summary>
    private sealed class FailingValidator
        : AbstractValidator<TestRequest>
    {
        /// <summary>
        /// Создаёт валидатор, который всегда возвращает ошибку
        /// для свойства <c>Name</c>.
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке.</param>
        public FailingValidator(string errorMessage)
        {
            RuleFor(x => x.Name).Must(_ => false).WithMessage(errorMessage);
        }
    }

    /// <summary>
    /// Контролируемый валидатор для тестирования параллельного запуска.
    /// Сигнализирует о старте через <paramref name="started"/>,
    /// ожидает сигнала <paramref name="complete"/> и возвращает ошибку.
    /// </summary>
    private sealed class ControlledValidator(
        TaskCompletionSource<bool> started,
        TaskCompletionSource<bool> complete,
        string propertyName = "Name",
        string errorMessage = "error")
        : AbstractValidator<TestRequest>
    {
        /// <inheritdoc />
        public override async Task<ValidationResult> ValidateAsync(
            ValidationContext<TestRequest> context,
            CancellationToken cancellation = default)
        {
            started.TrySetResult(true);
            await complete.Task;
            return new ValidationResult([
                new ValidationFailure(propertyName, errorMessage)
            ]);
        }
    }

    /// <summary>
    /// Создаёт экземпляр pipeline behaviour с заданными валидаторами.
    /// </summary>
    private static ValidationPipelineBehaviour<TestRequest, TestResponse> CreateSut(
        params IValidator<TestRequest>[] validators)
    {
        var logger = new FakeLogger<ValidationPipelineBehaviour<TestRequest, TestResponse>>(new FakeLogger());
        return new ValidationPipelineBehaviour<TestRequest, TestResponse>(logger, validators);
    }

    #region Next Delegate Tests

    /// <summary>
    /// Если валидаторы не зарегистрированы — запрос передаётся
    /// следующему делегату <c>Next</c> без изменений.
    /// </summary>
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }

        // Act
        var result = await sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        called.Should().BeTrue();
        result.Should().Be(response);
    }

    /// <summary>
    /// Если все валидаторы проходят проверку — запрос передаётся
    /// следующему делегату <c>Next</c>.
    /// </summary>
    [Fact]
    public async Task Handle_AllValid_CallsNext()
    {
        // Arrange
        var sut = CreateSut(new AlwaysValidValidator());
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        // Act
        var result = await sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        called.Should().BeTrue();
        result.Should().Be(response);
        return;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }
    }

    #endregion

    #region Validation Failure Tests

    /// <summary>
    /// Если хотя бы один валидатор фейлится — выбрасывается
    /// <see cref="ValidationException"/>, делегат <c>Next</c> не вызывается.
    /// </summary>
    [Fact]
    public async Task Handle_OneFails_ThrowsValidationException()
    {
        // Arrange
        var sut = CreateSut(new FailingValidator("must not be empty"));
        var request = new TestRequest();
        var response = new TestResponse();

        // Act
        var action = () => sut.Handle(request, Next, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
        return;

        Task<TestResponse> Next() => Task.FromResult(response);
    }

    /// <summary>
    /// Если фейлятся несколько валидаторов — ошибки агрегируются
    /// в одном <see cref="ValidationException"/>.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleValidators_AggregatesErrors()
    {
        // Arrange
        var sut = CreateSut(
            new FailingValidator("error A"),
            new FailingValidator("error B"));
        var request = new TestRequest();
        var response = new TestResponse();

        // Act
        var action = () => sut.Handle(request, Next, TestContext.Current.CancellationToken);

        var ex = await action.Should().ThrowAsync<ValidationException>();

        // Assert
        ex.Which.Errors.Should().HaveCount(2);
        ex.Which.Errors.Select(e => e.ErrorMessage).Should().Contain(["error A", "error B"]);
        return;

        Task<TestResponse> Next() => Task.FromResult(response);
    }

    /// <summary>
    /// Дублирующиеся ошибки (одинаковые <c>PropertyName</c> + <c>ErrorMessage</c>)
    /// дедуплицируются — в результате остаётся одна.
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateErrors_AreFiltered()
    {
        // Arrange
        var sut = CreateSut(
            new FailingValidator("same error"),
            new FailingValidator("same error"));
        var request = new TestRequest();
        var response = new TestResponse();

        // Act
        var action = () => sut.Handle(request, Next, TestContext.Current.CancellationToken);

        var ex = await action.Should().ThrowAsync<ValidationException>();

        // Assert
        ex.Which.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Name");
        return;

        Task<TestResponse> Next() => Task.FromResult(response);
    }

    #endregion

    #region Parallel Execution Tests

    /// <summary>
    /// Валидаторы запускаются параллельно:
    /// оба стартуют до завершения любого из них.
    /// </summary>
    [Fact]
    public async Task Handle_ValidatorsAreRunInParallel()
    {
        // Arrange
        var started1 = new TaskCompletionSource<bool>();
        var complete1 = new TaskCompletionSource<bool>();
        var started2 = new TaskCompletionSource<bool>();
        var complete2 = new TaskCompletionSource<bool>();

        var sut = CreateSut(
            new ControlledValidator(started1, complete1, "A", "error A"),
            new ControlledValidator(started2, complete2, "B", "error B"));
        var request = new TestRequest();
        var response = new TestResponse();

        // Act
        var handleTask = sut.Handle(request, Next, TestContext.Current.CancellationToken);

        await Task.WhenAll(started1.Task, started2.Task);

        complete1.TrySetResult(true);
        complete2.TrySetResult(true);

        var action = () => handleTask;

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
        return;

        Task<TestResponse> Next() => Task.FromResult(response);
    }

    #endregion
}
