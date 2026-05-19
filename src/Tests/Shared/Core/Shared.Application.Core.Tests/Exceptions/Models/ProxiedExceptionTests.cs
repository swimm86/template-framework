using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Exceptions.Models;

namespace Shared.Application.Core.Tests;

public sealed class ProxiedExceptionTests
{
    [Fact]
    public void Constructor_AssignsProblemDetailsAndStatusCode()
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Not Found",
            Detail = "Resource was not found",
            Status = 404,
        };
        const int statusCode = 404;

        var ex = new ProxiedException(problemDetails, statusCode);

        ex.ProblemDetails.Should().BeSameAs(problemDetails);
        ex.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void TryGetAdditionalData_WithDirectType_ReturnsValue()
    {
        const string key = "testKey";
        const int expectedValue = 42;
        var additionalData = new Dictionary<string, object>
        {
            [key] = expectedValue,
        };
        var problemDetails = new ProblemDetails { Title = "Test" };
        const int statusCode = 500;

        var ex = new ProxiedException(problemDetails, statusCode, additionalData);

        var found = ex.TryGetAdditionalData<int>(key, out var value);
        found.Should().BeTrue();
        value.Should().Be(expectedValue);
    }
}
