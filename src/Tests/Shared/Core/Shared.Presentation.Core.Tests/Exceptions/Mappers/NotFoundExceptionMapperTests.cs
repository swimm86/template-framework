using Shared.Domain.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Юнит-тесты для <see cref="NotFoundExceptionMapper"/> — маппера исключений «сущность не найдена» в ProblemDetails.
/// </summary>
public sealed class NotFoundExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 404 (Not Found).
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode404()
    {
        // Arrange
        var mapper = new NotFoundExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new NotFoundException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Проверяет, что заголовок первой ошибки равен «Ошибка - не найден».
    /// </summary>
    [Fact]
    public void Handle_TitleIsОшибкаНеНайден()
    {
        // Arrange
        var mapper = new NotFoundExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new NotFoundException("test");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Title.Should().Be("Ошибка - не найден");
    }

    /// <summary>
    /// Проверяет, что при создании исключения через конструктор с сообщением,
    /// детализация ошибки (Detail) равна переданному сообщению.
    /// </summary>
    [Fact]
    public void Handle_FromMessageCtor_DetailEqualsMessage()
    {
        // Arrange
        var mapper = new NotFoundExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new NotFoundException("Entity X not found");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.Errors.Should().ContainSingle().Which.Detail.Should().Be("Entity X not found");
    }

    /// <summary>
    /// Проверяет, что при создании исключения через конструктор с типом сущности и ключом,
    /// детализация ошибки содержит имя типа сущности и значение ключа.
    /// </summary>
    [Fact]
    public void Handle_FromEntityKeyCtor_DetailContainsEntityNameAndKey()
    {
        // Arrange
        var mapper = new NotFoundExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new NotFoundException(typeof(string), 42);

        // Act
        var response = mapper.Handle(exception);

        // Assert
        var detail = response.Errors.Should().ContainSingle().Which.Detail;
        detail.Should().Contain("String");
        detail.Should().Contain("42");
    }

    /// <summary>
    /// Проверяет проброс <c>AdditionalData</c> из исключения в ответ.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var mapper = new NotFoundExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new NotFoundException(
            "test",
            innerException: null,
            new Dictionary<string, object> { ["key"] = "value" });

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().NotBeNull();
        response.AdditionalData!["key"].Should().Be("value");
    }
}
