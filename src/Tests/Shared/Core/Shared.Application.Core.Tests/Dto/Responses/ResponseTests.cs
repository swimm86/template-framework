using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Batch;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Тесты для классов ответов: <see cref="Response"/>, <see cref="PageableResponse{T}"/>, <see cref="ErrorResponse"/>, <see cref="ResponseWithMessage"/>.
/// </summary>
public sealed class ResponseTests
{
    private sealed record TestPageableRequest : PageableRequest;

    /// <summary>
    /// Значения по умолчанию <see cref="PageableRequest"/>: номер страницы = 1, размер = <see cref="Constants.DefaultBatchSize"/>.
    /// </summary>
    [Fact]
    public void PageableRequest_Defaults()
    {
        // Act
        var request = new TestPageableRequest();

        // Assert
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(Constants.DefaultBatchSize);
    }

    /// <summary>
    /// <see cref="PageableResponse{T}"/> заполняет все поля через конструктор.
    /// </summary>
    [Fact]
    public void PageableResponse_HasAllFields()
    {
        // Arrange
        const int totalPages = 5;
        const int pageNumber = 3;
        const int statusCode = StatusCodes.Status200OK;
        var payload = new[] { 1, 2, 3 };

        // Act
        var response = new PageableResponse<IReadOnlyCollection<int>>(totalPages, pageNumber, payload, statusCode);

        // Assert
        response.TotalPages.Should().Be(totalPages);
        response.PageNumber.Should().Be(pageNumber);
        response.StatusCode.Should().Be(statusCode);
        response.Payload.Should().BeEquivalentTo(payload);
    }

    /// <summary>
    /// <see cref="Response.StatusCode"/> по умолчанию равен <c>default</c> (0).
    /// </summary>
    [Fact]
    public void Response_StatusCodeDefault()
    {
        // Act
        var response = new Response();

        // Assert
        response.StatusCode.Should().Be(default);
    }

    /// <summary>
    /// <see cref="ErrorResponse.Errors"/> заполняется переданной коллекцией.
    /// </summary>
    [Fact]
    public void ErrorResponse_ErrorsCollection_Initialized()
    {
        // Arrange
        var errors = new List<ProblemDetails>
        {
            new() { Title = "Error 1" },
            new() { Title = "Error 2" },
        };

        // Act
        var errorResponse = new ErrorResponse { Errors = errors, Details = "Multiple errors occurred" };

        // Assert
        errorResponse.Errors.Should().HaveCount(2);
        errorResponse.Errors.Should().BeEquivalentTo(errors);
        errorResponse.Details.Should().Be("Multiple errors occurred");
    }

    /// <summary>
    /// <c>with</c>-выражение создаёт копию с изменённым свойством, не мутируя оригинал.
    /// </summary>
    [Fact]
    public void Response_WithExpression_ModifiesProperties()
    {
        // Arrange
        var original = new Response(200);

        // Act
        var modified = original with { StatusCode = 404 };

        // Assert
        modified.StatusCode.Should().Be(404);
        original.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// <see cref="ResponseWithMessage"/> сохраняет переданные message и status code.
    /// </summary>
    [Fact]
    public void ResponseWithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Operation completed";
        const int statusCode = StatusCodes.Status201Created;

        // Act
        var response = new ResponseWithMessage(message, statusCode);

        // Assert
        response.Message.Should().Be(message);
        response.StatusCode.Should().Be(statusCode);
    }
}
