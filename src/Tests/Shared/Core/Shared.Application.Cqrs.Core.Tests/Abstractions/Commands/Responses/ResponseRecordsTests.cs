using Microsoft.AspNetCore.Http;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Responses;

/// <summary>
/// Тесты record-типов ответов команд: <see cref="CreateResponse{TPayload}"/>
/// и <see cref="UpdateResponse{TPayload}"/>.
/// Проверяют инициализацию свойств, иммутабельность через <c>with</c>,
/// значения по умолчанию и параметризованные конструкторы.
/// </summary>
public sealed class ResponseRecordsTests
{
    #region Init-Property Tests

    /// <summary>
    /// При инициализации через init-свойства <see cref="CreateResponse{TPayload}"/>
    /// все значения присваиваются корректно.
    /// </summary>
    [Fact]
    public void CreateResponse_InitProperties_AreAssigned()
    {
        // Arrange
        var id = Guid.NewGuid();
        var payload = new { Name = "test" };

        // Act
        var response = new CreateResponse<object>
        {
            Id = id,
            Payload = payload,
            StatusCode = StatusCodes.Status201Created,
        };

        // Assert
        response.Id.Should().Be(id);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    /// <summary>
    /// При инициализации через init-свойства <see cref="UpdateResponse{TPayload}"/>
    /// все значения присваиваются корректно.
    /// </summary>
    [Fact]
    public void UpdateResponse_InitProperties_AreAssigned()
    {
        // Arrange
        var key = Guid.NewGuid();
        var payload = new { Name = "updated" };

        // Act
        var response = new UpdateResponse<object>
        {
            Key = key,
            Payload = payload,
            StatusCode = StatusCodes.Status200OK,
        };

        // Assert
        response.Key.Should().Be(key);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    #endregion

    #region Immutability Tests

    /// <summary>
    /// Record-ответы поддерживают выражение <c>with</c>:
    /// копирование с изменением выбранных свойств.
    /// </summary>
    [Fact]
    public void Responses_AreRecords_AndSupportWithExpression()
    {
        // Arrange
        var original = new CreateResponse<object>
        {
            Id = Guid.NewGuid(),
            Payload = new { Value = 1 },
            StatusCode = 201,
        };

        // Act
        var modified = original with { StatusCode = 500 };

        // Assert
        modified.Id.Should().Be(original.Id);
        modified.Payload.Should().Be(original.Payload);
        modified.StatusCode.Should().Be(500);
    }

    #endregion

    #region Default Values Tests

    /// <summary>
    /// При создании без явного указания свойств
    /// <c>StatusCode</c> равен <c>0</c> (default).
    /// </summary>
    [Fact]
    public void Response_StatusCodeDefault()
    {
        // Arrange
        var createResponse = new CreateResponse<object>();
        var updateResponse = new UpdateResponse<object>();

        // Assert
        createResponse.StatusCode.Should().Be(0);
        updateResponse.StatusCode.Should().Be(0);
    }

    #endregion

    #region Parameterized Constructor Tests

    /// <summary>
    /// Параметризованный конструктор <see cref="CreateResponse{TPayload}"/>
    /// корректно присваивает <c>Id</c>, <c>Payload</c> и <c>StatusCode</c>.
    /// </summary>
    [Fact]
    public void CreateResponse_ParameterizedConstructor_PropertiesAreAssigned()
    {
        // Arrange
        var id = Guid.NewGuid();
        var payload = "created-data";

        // Act
        var response = new CreateResponse<string>(
            id,
            payload,
            StatusCode: StatusCodes.Status201Created);

        // Assert
        response.Id.Should().Be(id);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Параметризованный конструктор без явного <c>StatusCode</c>
    /// устанавливает <c>StatusCodes.Status201Created</c> по умолчанию.
    /// </summary>
    [Fact]
    public void CreateResponse_ParameterizedConstructor_DefaultStatusCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var payload = new { Value = 42 };

        // Act
        var response = new CreateResponse<object>(id, payload);

        // Assert
        response.Id.Should().Be(id);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    #endregion
}
