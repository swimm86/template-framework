using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Dto.Requests;
using Shared.Application.Core.Dto.Responses;
using Shared.Common.Batch;

namespace Shared.Application.Core.Tests;

public sealed class ResponseTests
{
    private sealed record TestPageableRequest : PageableRequest;

    [Fact]
    public void PageableRequest_Defaults()
    {
        var request = new TestPageableRequest();

        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(Constants.DefaultBatchSize);
    }

    [Fact]
    public void PageableResponse_HasAllFields()
    {
        const int totalPages = 5;
        const int pageNumber = 3;
        const int statusCode = StatusCodes.Status200OK;
        var payload = new[] { 1, 2, 3 };

        var response = new PageableResponse<IReadOnlyCollection<int>>(totalPages, pageNumber, payload, statusCode);

        response.TotalPages.Should().Be(totalPages);
        response.PageNumber.Should().Be(pageNumber);
        response.StatusCode.Should().Be(statusCode);
        response.Payload.Should().BeEquivalentTo(payload);
    }

    [Fact]
    public void Response_StatusCodeDefault()
    {
        var response = new Response();

        response.StatusCode.Should().Be(default);
    }

    [Fact]
    public void ErrorResponse_ErrorsCollection_Initialized()
    {
        var errors = new List<ProblemDetails>
        {
            new() { Title = "Error 1" },
            new() { Title = "Error 2" },
        };

        var errorResponse = new ErrorResponse { Errors = errors, Details = "Multiple errors occurred" };

        errorResponse.Errors.Should().HaveCount(2);
        errorResponse.Errors.Should().BeEquivalentTo(errors);
        errorResponse.Details.Should().Be("Multiple errors occurred");
    }

    [Fact]
    public void Response_WithExpression_ModifiesProperties()
    {
        var original = new Response(200);

        var modified = original with { StatusCode = 404 };

        modified.StatusCode.Should().Be(404);
        original.StatusCode.Should().Be(200);
    }

    [Fact]
    public void ResponseWithMessage_SetsMessage()
    {
        const string message = "Operation completed";
        const int statusCode = StatusCodes.Status201Created;

        var response = new ResponseWithMessage(message, statusCode);

        response.Message.Should().Be(message);
        response.StatusCode.Should().Be(statusCode);
    }
}
