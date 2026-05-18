using Shared.Domain.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Юнит-тесты для <see cref="BusinessLogicExceptionMapper"/> — маппера ошибок бизнес-логики в ProblemDetails.
/// </summary>
public sealed class BusinessLogicExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 422 (Unprocessable Entity).
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode422()
    {
        // Arrange
        var mapper = new BusinessLogicExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(422);
    }

    /// <summary>
    /// Проверяет, что заголовок первой ошибки равен «Ошибка бизнес-логики».
    /// </summary>
    [Fact]
    public void Handle_TitleIsОшибкаБизнесЛогики()
    {
        // Arrange
        var mapper = new BusinessLogicExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Title.Should().Be("Ошибка бизнес-логики");
    }

    /// <summary>
    /// Проверяет проброс <c>AdditionalData</c> из исключения в ответ.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var mapper = new BusinessLogicExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException(
            "msg",
            new Dictionary<string, object> { ["field"] = "value" });

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().NotBeNull();
        response.AdditionalData!["field"].Should().Be("value");
    }

    /// <summary>
    /// Проверяет, что детализация ошибки (Detail) равна сообщению исключения.
    /// </summary>
    [Fact]
    public void Handle_DetailEqualsExceptionMessage()
    {
        // Arrange
        var mapper = new BusinessLogicExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("ошибка валидации");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Detail.Should().Be("ошибка валидации");
    }
}
