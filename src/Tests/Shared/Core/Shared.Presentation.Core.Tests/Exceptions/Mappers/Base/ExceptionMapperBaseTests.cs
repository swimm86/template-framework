using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Tests.Infrastructure;
using Shared.Presentation.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers.Base;

/// <summary>
/// Модульные тесты для <see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.ExceptionMapperBase{TException}"/>.
/// </summary>
public sealed class ExceptionMapperBaseTests
{
    /// <summary>
    /// Проверяет, что <c><see cref="IExceptionMapper.Map"/></c> для совпадающего типа
    /// делегирует вызов в типобезопасный <c><see cref="Shared.Presentation.Core.Exceptions.Interfaces.IExceptionMapper{TException}.Handle"/></c>.
    /// </summary>
    [Fact]
    public void Map_WithMatchingType_DelegatesToHandle()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Map(exception);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(418);
        response.Errors.Should().HaveCount(1);
        response.Errors.Should().ContainSingle(e => e.Title == "Test");
    }

    /// <summary>
    /// Проверяет, что <c><see cref="IExceptionMapper.Map"/></c> для несовпадающего типа
    /// выбрасывает <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Map_WithMismatchedType_ThrowsInvalidOperationException()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new InvalidOperationException("x");

        // Act
        Action act = () => mapper.Map(exception);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Проверяет, что <c><see cref="Shared.Presentation.Core.Exceptions.Interfaces.IExceptionMapper{TException}.Handle"/></c>
    /// всегда устанавливает корректный статус-код ответа.
    /// </summary>
    [Fact]
    public void Handle_AlwaysSetsCorrectStatusCode()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(418);
    }

    /// <summary>
    /// Проверяет, что <c><see cref="Shared.Presentation.Core.Exceptions.Interfaces.IExceptionMapper{TException}.Handle"/></c>
    /// по умолчанию добавляет ровно один <c><see cref="ProblemDetails"/></c> в коллекцию ошибок.
    /// </summary>
    [Fact]
    public void Handle_AlwaysAddsExactlyOneProblemDetailsByDefault()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().HaveCount(1);
    }

    /// <summary>
    /// Проверяет, что заголовок <c><see cref="ProblemDetails.Title"/></c>
    /// совпадает с настроенным значением в маппере.
    /// </summary>
    [Fact]
    public void Handle_ProblemDetailsTitle_EqualsConfiguredTitle()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle(e => e.Title == "Test");
    }

    /// <summary>
    /// Проверяет, что <c><see cref="ProblemDetails.Detail"/></c> равен сообщению исключения.
    /// </summary>
    [Fact]
    public void Handle_ProblemDetailsDetail_EqualsExceptionMessage()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle(e => e.Detail == "x");
    }

    /// <summary>
    /// Проверяет, что <c><see cref="ProblemDetails.Status"/></c> равен статус-коду ответа.
    /// </summary>
    [Fact]
    public void Handle_ProblemDetailsStatus_EqualsResponseStatusCode()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle(e => e.Status == 418);
    }

    /// <summary>
    /// Проверяет, что по умолчанию <see cref="ErrorResponse.AdditionalData"/> равен <see langword="null"/>.
    /// </summary>
    [Fact]
    public void Handle_GetAdditionalData_DefaultIsNull()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что при включённом обогащении (<c><see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.ExceptionMapperBase{TException}.ShouldEnrichWithTrace"/></c> = <see langword="true"/>)
    /// <c><see cref="ErrorResponse.Details"/></c> содержит полное имя типа исключения и его сообщение.
    /// </summary>
    [Fact]
    public void Handle_WithEnrichWithTraceTrue_DetailsIncludesTypeAndMessage()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().Contain(nameof(TestException));
        response.Details.Should().Contain("x");
    }

    /// <summary>
    /// Проверяет, что при включённом обогащении для исключения с реальным стеком вызовов
    /// <c><see cref="ErrorResponse.Details"/></c> содержит фрагменты стека.
    /// </summary>
    [Fact]
    public void Handle_WithEnrichWithTraceTrue_AndStackTrace_DetailsIncludesStackLines()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = ExceptionFactory.Thrown(new TestException("x"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().NotBeNull();
        response.Details.Should().Contain("at ");
    }

    /// <summary>
    /// Проверяет, что при принудительно отключённом обогащении
    /// (<c><see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.ExceptionMapperBase{TException}.ShouldEnrichWithTrace"/></c> = <see langword="false"/>) <c><see cref="ErrorResponse.Details"/></c> равен <see langword="null"/>.
    /// </summary>
    [Fact]
    public void Handle_WithEnrichWithTraceFalseOverride_DetailsIsNull()
    {
        // Arrange
        var mapper = new TestExceptionMapperWithoutTrace(TestConfigurationBuilder.Empty());
        var exception = new TestException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что при наличии внутреннего исключения
    /// <c><see cref="ErrorResponse.Details"/></c> содержит блок внутреннего исключения
    /// с маркерами <c>---&gt;</c> и <c>End of inner exception stack trace</c>.
    /// </summary>
    [Fact]
    public void Handle_WithInnerException_DetailsIncludesInnerBlock()
    {
        // Arrange
        var mapper = new TestExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestException("outer", new TestException("inner"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().Contain(" ---> ");
        response.Details.Should().Contain("--- End of inner exception stack trace ---");
    }

    /// <summary>
    /// Проверяет, что когда глубина вложенности исключений превышает
    /// <c><see cref="Shared.Presentation.Core.Exceptions.Settings.ExceptionMapperSettings.MaxExceptionDepth"/></c>, <c><see cref="ErrorResponse.Details"/></c> содержит маркер
    /// превышения максимальной глубины.
    /// </summary>
    [Fact]
    public void Handle_WhenInnerDepthExceedsMaxExceptionDepth_DetailsContainsDepthMarker()
    {
        // Arrange
        var mapper = new TestExceptionMapper(
            TestConfigurationBuilder.WithSettings(maxExceptionDepth: 1));
        var inner = ExceptionFactory.WithInnerDepth(5);
        var exception = new TestException("level-0", inner);

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().Contain("превышена максимальная глубина исключений");
    }

    /// <summary>
    /// Проверяет, что параметр <c><see cref="Shared.Presentation.Core.Exceptions.Settings.ExceptionMapperSettings.StackTraceDepth"/></c> реально ограничивает
    /// количество строк стека вызовов в <c><see cref="ErrorResponse.Details"/></c>.
    /// </summary>
    [Fact]
    public void Handle_WithStackTraceDepth_LimitsLinesInDetails()
    {
        // Arrange
        var mapper = new TestExceptionMapper(
            TestConfigurationBuilder.WithSettings(stackTraceDepth: 2));
        var exception = ExceptionFactory.Thrown(new TestException("x"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().NotBeNull();
        var stackLines = response.Details!
            .Split(Environment.NewLine)
            .Count(l => l.TrimStart().StartsWith("at "));
        stackLines.Should().BeLessOrEqualTo(2);
    }
}
