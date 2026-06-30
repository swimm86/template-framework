using Shared.Domain.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Exceptions.Models;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Юнит-тесты для <see cref="UnauthorizedExceptionMapper"/> — маппера исключений неаутентифицированного доступа в ProblemDetails.
/// </summary>
public sealed class UnauthorizedExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 401 (Unauthorized).
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode401()
    {
        // Arrange
        var mapper = new UnauthorizedExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new UnauthorizedException(
            new ClientRequestContext("svc-test", "/api/data"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Проверяет, что заголовок первой ошибки равен «Пользователь не аутентифицирован».
    /// </summary>
    [Fact]
    public void Handle_TitleIsПользовательНеАутентифицирован()
    {
        // Arrange
        var mapper = new UnauthorizedExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new UnauthorizedException(
            new ClientRequestContext("svc-test", "/api/data"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Title.Should().Be("Пользователь не аутентифицирован");
    }

    /// <summary>
    /// Проверяет, что детализация ошибки содержит имя клиента и абсолютный путь запроса.
    /// </summary>
    [Fact]
    public void Handle_DetailContainsClientNameAndAbsolutePath()
    {
        // Arrange
        var mapper = new UnauthorizedExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new UnauthorizedException(
            new ClientRequestContext("svc-X", "/api/y"));

        // Act
        var response = mapper.Handle(exception);

        // Assert
        var detail = response.Errors.Should().ContainSingle().Which.Detail;
        detail.Should().Contain("svc-X");
        detail.Should().Contain("/api/y");
    }

    /// <summary>
    /// Проверяет проброс <c>AdditionalData</c> из исключения в ответ.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var mapper = new UnauthorizedExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new UnauthorizedException(
            new ClientRequestContext("svc-test", "/api/data"),
            innerException: null,
            new Dictionary<string, object> { ["reason"] = "token expired" });

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().NotBeNull();
        response.AdditionalData!["reason"].Should().Be("token expired");
    }
}
