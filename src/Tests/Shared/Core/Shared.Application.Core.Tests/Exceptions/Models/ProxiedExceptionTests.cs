using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Exceptions.Models;

namespace Shared.Application.Core.Tests;

/// <summary>
/// Модульные тесты для <see cref="ProxiedException"/>.
/// </summary>
public sealed class ProxiedExceptionTests
{
    /// <summary>
    /// Проверяет, что конструктор корректно присваивает <see cref="ProblemDetails"/> и HTTP-статус код.
    /// </summary>
    [Fact]
    public void Constructor_AssignsProblemDetailsAndStatusCode()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Title = "Not Found",
            Detail = "Resource was not found",
            Status = 404,
        };
        const int statusCode = 404;

        // Act
        var ex = new ProxiedException(problemDetails, statusCode);

        // Assert
        ex.ProblemDetails.Should().BeSameAs(problemDetails);
        ex.StatusCode.Should().Be(statusCode);
    }

    /// <summary>
    /// Проверяет, что метод <see cref="ProxiedException.TryGetAdditionalData{T}"/> извлекает значение по ключу, когда тип значения совпадает.
    /// </summary>
    [Fact]
    public void TryGetAdditionalData_WithDirectType_ReturnsValue()
    {
        // Arrange
        const string key = "testKey";
        const int expectedValue = 42;
        var additionalData = new Dictionary<string, object>
        {
            [key] = expectedValue,
        };
        var problemDetails = new ProblemDetails { Title = "Test" };
        const int statusCode = 500;

        var ex = new ProxiedException(problemDetails, statusCode, innerException: null, additionalData);

        // Act
        var found = ex.TryGetAdditionalData<int>(key, out var value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be(expectedValue);
    }
}
