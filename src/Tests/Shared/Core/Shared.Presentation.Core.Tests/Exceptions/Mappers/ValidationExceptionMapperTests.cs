using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Тесты <see cref="ValidationExceptionMapper"/>.
/// </summary>
public sealed class ValidationExceptionMapperTests
{
    private readonly ValidationExceptionMapper _mapper = new(TestConfigurationBuilder.Empty());

    /// <summary>
    /// Проверяет, что статус-код ответа равен 400 Bad Request.
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode400()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Prop", "Error message") };
        var ex = new ValidationException(failures);

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Проверяет, что заголовок ошибки равен "Ошибка валидации".
    /// </summary>
    [Fact]
    public void Handle_TitleIsОшибкаВалидации()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Prop", "Error message") };
        var ex = new ValidationException(failures);

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Title.Should().Be("Ошибка валидации");
    }

    /// <summary>
    /// Проверяет, что единственная ошибка валидации проставляется в Detail.
    /// </summary>
    [Fact]
    public void Handle_WithSingleError_DetailEqualsThatMessage()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Prop", "msg") };
        var ex = new ValidationException(failures);

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Detail.Should().Be("msg");
    }

    /// <summary>
    /// Проверяет, что несколько различных ошибок объединяются через разделитель.
    /// </summary>
    [Fact]
    public void Handle_WithMultipleErrors_JoinsDistinctMessages()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("A", "first"),
            new("B", "second"),
            new("C", "third"),
        };
        var ex = new ValidationException(failures);

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Detail.Should().Be($"first;{Environment.NewLine}second;{Environment.NewLine}third");
    }

    /// <summary>
    /// Проверяет дедупликацию одинаковых сообщений валидации.
    /// </summary>
    [Fact]
    public void Handle_WithDuplicateErrors_DeduplicatesMessages()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("A", "dup"),
            new("B", "dup"),
            new("C", "unique"),
        };
        var ex = new ValidationException(failures);

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Detail.Should().Be($"dup;{Environment.NewLine}unique");
    }

    /// <summary>
    /// Проверяет, что при отсутствии ошибок Detail равен null.
    /// </summary>
    [Fact]
    public void Handle_WithEmptyErrors_DetailIsNull()
    {
        // Arrange
        var ex = new ValidationException(Array.Empty<ValidationFailure>());

        // Act
        var response = _mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Detail.Should().BeNull();
    }
}
