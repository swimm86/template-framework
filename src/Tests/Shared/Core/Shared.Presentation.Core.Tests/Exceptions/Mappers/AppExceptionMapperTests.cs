using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Юнит-тесты для <c><see cref="AppExceptionMapper"/></c> — маппера доменных исключений <c><see cref="AppException"/></c> в ProblemDetails.
/// Тестируется через конкретный наследник <c><see cref="BusinessLogicException"/></c>.
/// </summary>
public sealed class AppExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 500 (Internal Server Error).
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode500()
    {
        // Arrange
        var mapper = new AppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Проверяет, что заголовок первой ошибки равен «Ошибка приложения».
    /// </summary>
    [Fact]
    public void Handle_TitleIsОшибкаПриложения()
    {
        // Arrange
        var mapper = new AppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Title.Should().Be("Ошибка приложения");
    }

    /// <summary>
    /// Проверяет проброс <c>AdditionalData</c> из исключения в ответ.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var mapper = new AppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("msg", new Dictionary<string, object> { ["k"] = "v" });

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().NotBeNull();
        response.AdditionalData!["k"].Should().Be("v");
    }

    /// <summary>
    /// Проверяет, что детализация ошибки (Detail) равна сообщению исключения.
    /// </summary>
    [Fact]
    public void Handle_DetailEqualsExceptionMessage()
    {
        // Arrange
        var mapper = new AppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new BusinessLogicException("test message");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Detail.Should().Be("test message");
    }
}
