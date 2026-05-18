using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Юнит-тесты для <see cref="DefaultExceptionMapper"/> — маппера необработанных исключений <see cref="Exception"/> в ProblemDetails (RFC 7807).
/// </summary>
public sealed class DefaultExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 500 (Internal Server Error).
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode500()
    {
        // Arrange
        var mapper = new DefaultExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new Exception("boom");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Проверяет, что заголовок первой ошибки равен «Ошибка сервера».
    /// </summary>
    [Fact]
    public void Handle_TitleIsОшибкаСервера()
    {
        // Arrange
        var mapper = new DefaultExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new Exception("boom");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Title.Should().Be("Ошибка сервера");
    }

    /// <summary>
    /// Проверяет, что детализация ошибки (Detail) равна сообщению исключения.
    /// </summary>
    [Fact]
    public void Handle_DetailEqualsExceptionMessage()
    {
        // Arrange
        var mapper = new DefaultExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new Exception("boom");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Detail.Should().Be("boom");
    }

    /// <summary>
    /// Проверяет, что при отключённом обогащении трейсом свойство <c>Details</c> ответа равно <see langword="null"/>.
    /// </summary>
    [Fact]
    public void Handle_WithoutEnrichWithTrace_DetailsIsNull()
    {
        // Arrange
        var configuration = TestConfigurationBuilder.WithSettings(shouldEnrichWithTrace: false);
        var mapper = new DefaultExceptionMapper(configuration);
        var exception = new Exception("boom");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Details.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что <c>AdditionalData</c> равно <see langword="null"/> для <see cref="Exception"/>,
    /// не реализующего <see cref="Shared.Domain.Core.Interfaces.IWithAdditionalData"/>.
    /// </summary>
    [Fact]
    public void Handle_AdditionalDataIsNull()
    {
        // Arrange
        var mapper = new DefaultExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new Exception("boom");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().BeNull();
    }
}
