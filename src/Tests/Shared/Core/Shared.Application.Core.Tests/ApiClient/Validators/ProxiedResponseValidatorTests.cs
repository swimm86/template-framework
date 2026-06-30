// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedResponseValidatorTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Validators;
using Shared.Application.Core.Exceptions.Models;
using Shared.Application.Core.Tests.ApiClient.Validators.TestSupport;
using Shared.Testing.Configuration;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Core.Tests.ApiClient.Validators;

/// <summary>
/// Тесты <see cref="ProxiedResponseValidator"/>.
/// </summary>
public sealed class ProxiedResponseValidatorTests
{
    private const string ClientName = "TestClient";
    private const string DefaultAbsolutePath = "/v1/test";

    [Fact]
    public async Task ValidateAsync_NullHttpResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var (validator, _) = CreateValidator();

        // Act
        var act = () => validator.ValidateAsync(null!, ClientName);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.Accepted)]
    public async Task ValidateAsync_2xxStatusCode_ReturnsWithoutExceptionOrLog(HttpStatusCode statusCode)
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder.WithStatusCode(statusCode).Build();

        // Act
        await validator.ValidateAsync(response, ClientName);

        // Assert
        logger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ValidateAsync_ErrorWithValidProblemDetails_ThrowsProxiedExceptionWithMappedFields(HttpStatusCode statusCode)
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = $$"""
        {
            "type": "https://example.com/prob",
            "title": "Upstream error",
            "status": {{(int)statusCode}},
            "detail": "Some detail"
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(statusCode)
            .WithJsonBody(body)
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.StatusCode.Should().Be((int)statusCode);
        assertion.Which.ProblemDetails.Title.Should().Be("Upstream error");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-json")]
    [InlineData("null")]
    [InlineData("\"plain-string\"")]
    [InlineData("[1,2,3]")]
    public async Task ValidateAsync_ErrorWithUnparseableBody_ThrowsProxiedExceptionWithFallback(string body)
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.StatusCode.Should().Be(400);
        assertion.Which.ProblemDetails.Detail.Should().Be(body);
        assertion.Which.ProblemDetails.Status.Should().Be(400);
        assertion.Which.ProblemDetails.Instance.Should().Be(DefaultAbsolutePath);
    }

    [Fact]
    public async Task ValidateAsync_ErrorWithJsonElementObjectAdditionalData_PopulatesAdditionalData()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Error",
            "status": 400,
            "additionalData": { "key1": "value1", "key2": 42 }
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.TryGetAdditionalData<string>("key1", out var v1).Should().BeTrue();
        v1.Should().Be("value1");
    }

    [Fact]
    public async Task ValidateAsync_ErrorWithDictionaryAdditionalData_PopulatesAdditionalData()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Error",
            "status": 400,
            "additionalData": { "k": "v" }
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.AdditionalData.Should().NotBeNull();
        assertion.Which.TryGetAdditionalData<string>("k", out var value).Should().BeTrue();
        value.Should().Be("v");
    }

    [Fact]
    public async Task ValidateAsync_ErrorWithReadOnlyDictionaryAdditionalData_PopulatesAdditionalData()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var additionalData = new Dictionary<string, object> { ["rk"] = "rv" };
        var problemDetails = new ProblemDetails
        {
            Status = 400,
            Title = "Error",
        };
        problemDetails.Extensions["additionalData"] = additionalData;
        var body = JsonSerializer.Serialize(problemDetails);
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.AdditionalData.Should().NotBeNull();
        assertion.Which.TryGetAdditionalData<string>("rk", out var value).Should().BeTrue();
        value.Should().Be("rv");
    }

    [Theory]
    [InlineData("true")]
    [InlineData("123")]
    [InlineData("\"str\"")]
    [InlineData("[1]")]
    public async Task ValidateAsync_ErrorWithNonObjectAdditionalData_DoesNotPopulate(string additionalDataJson)
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = $$"""
        {
            "type": "https://example.com/prob",
            "title": "Error",
            "status": 400,
            "additionalData": {{additionalDataJson}}
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.AdditionalData.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ErrorWithAdditionalData_KeyIsRemovedFromProblemDetailsExtensions()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "status": 400,
            "title": "Error",
            "additionalData": { "k": "v" }
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Extensions.Should().NotContainKey("additionalData");
    }

    [Fact]
    public async Task ValidateAsync_500InErrorsArrayWithDetail_RewritesTitleAndAppendsReason()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": 500,
            "errors": [
                { "status": 500, "detail": "Database connection failed" }
            ]
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.InternalServerError)
            .WithJsonBody(body)
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Title.Should().Be("Ошибка во время взаимодействия с внешним сервисом.");
        assertion.Which.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task ValidateAsync_500InErrorsArrayWithEmptyDetail_RewritesTitleWithoutReason()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": 500,
            "errors": [
                { "status": 500, "detail": "" }
            ]
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.InternalServerError)
            .WithJsonBody(body)
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Title.Should().Be("Ошибка во время взаимодействия с внешним сервисом.");
        assertion.Which.ProblemDetails.Detail.Should().NotContain("Причина");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task ValidateAsync_Non500InErrorsArray_DoesNotRewrite(HttpStatusCode innerStatus)
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = $$"""
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": {{(int)innerStatus}},
            "errors": [
                { "status": {{(int)innerStatus}}, "detail": "Some detail" }
            ]
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(innerStatus)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Title.Should().Be("Original");
    }

    [Fact]
    public async Task ValidateAsync_WithoutErrorsArray_DoesNotRewrite()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": 400
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Title.Should().Be("Original");
    }

    [Fact]
    public async Task ValidateAsync_WithErrorsArrayButNotJsonArray_DoesNotRewrite()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": 400,
            "errors": { "status": 500, "detail": "Should be ignored" }
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Title.Should().Be("Original");
    }

    [Fact]
    public async Task ValidateAsync_WithMultiple500sInErrorsArray_UsesFirstOccurrence()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var body = """
        {
            "type": "https://example.com/prob",
            "title": "Original",
            "status": 500,
            "errors": [
                { "status": 500, "detail": "First reason" },
                { "status": 500, "detail": "Second reason" }
            ]
        }
        """;
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.InternalServerError)
            .WithJsonBody(body)
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Detail.Should().Contain("First reason");
        assertion.Which.ProblemDetails.Detail.Should().NotContain("Second reason");
    }

    [Fact]
    public async Task ValidateAsync_ContentReadThrowsIOException_ThrowsProxiedExceptionWithInnerAndDegradedDetails()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var innerException = new IOException("Connection reset");
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithThrowingContent(innerException)
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.InnerException.Should().BeSameAs(innerException);
        assertion.Which.ProblemDetails.Status.Should().Be(400);
        assertion.Which.ProblemDetails.Instance.Should().Be(DefaultAbsolutePath);
        assertion.Which.ProblemDetails.Title.Should().Be("Ошибка во время взаимодействия с внешним сервисом.");
    }

    [Fact]
    public async Task ValidateAsync_BuildFailurePath_LogErrorIsCalledWithOriginalException()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var innerException = new IOException("Boom");
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithThrowingContent(innerException)
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        logger.Entries.Should().HaveCount(1);
        logger.Entries.First().Exception.Should().BeSameAs(innerException);
        logger.Entries.First().Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task ValidateAsync_PreCancelledToken_PropagatesOperationCanceledException()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400}""")
            .Build();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("https://api.example.com/v1/users", null, "/v1/users")]
    [InlineData(null, "/v1/users", "/v1/users")]
    [InlineData("https://api.example.com", null, "unknown")]
    [InlineData(null, null, "unknown")]
    [InlineData(null, "   ", "unknown")]
    [InlineData("https://api.example.com/v1/users", "/fallback", "/v1/users")]
    public async Task ValidateAsync_UriResolution_SelectsExpectedPath(string? requestUri, string? logUri, string expectedPath)
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var builder = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X"}""");
        if (requestUri != null)
        {
            builder.WithRequestUri(requestUri);
        }
        var response = builder.Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response, ClientName,
                logUri: logUri,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        logger.Entries.Should().HaveCount(1);
        logger.Entries.First().Message.Should().Contain($"Path={expectedPath}");
    }

    [Fact]
    public async Task ValidateAsync_ErrorPath_LogsExactlyOnceAtErrorLevel()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X"}""")
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        logger.Entries.Should().HaveCount(1);
        logger.Entries.First().Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task ValidateAsync_ErrorPath_LogMessageContainsAllContextFields()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"Upstream X","detail":"body-text"}""")
            .WithRequestUri($"https://api.example.com{DefaultAbsolutePath}")
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        var entry = logger.Entries.First();
        entry.Message.Should().Contain(ClientName);
        entry.Message.Should().Contain(DefaultAbsolutePath);
        entry.Message.Should().Contain("400");
        entry.Message.Should().Contain("body-text");
    }

    [Fact]
    public async Task ValidateAsync_ErrorPath_LogContentObject_PropertiesAppearInRenderedLog()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X"}""")
            .Build();
        var logContent = new { Name = "Test", Count = 42 };

        // Act
        try
        {
            await validator.ValidateAsync(response, ClientName, logContent: logContent,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        var entry = logger.Entries.First();
        entry.Message.Should().Contain("name");
        entry.Message.Should().Contain("count");
    }

    [Fact]
    public async Task ValidateAsync_ErrorPath_LogContentNull_LoggedAsNull()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X"}""")
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(response, ClientName, logContent: null,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        var entry = logger.Entries.First();
        entry.Message.Should().Contain("RequestBody=");
    }

    [Fact]
    public async Task ValidateAsync_HappyPath_ProxiedExceptionHasNullInnerException()
    {
        // Arrange
        var (validator, _) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X"}""")
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.InnerException.Should().BeNull();
    }

    [Theory]
    [InlineData("https://api.example.com/v1/users", null, "/v1/users")]
    [InlineData(null, "/v1/users", "/v1/users")]
    [InlineData("https://api.example.com", null, "unknown")]
    [InlineData(null, null, "unknown")]
    [InlineData(null, "   ", "unknown")]
    [InlineData("https://api.example.com/v1/users", "/fallback", "/v1/users")]
    public async Task ValidateAsync_UriResolution_PropagatesToProblemDetailsInstance(
        string? requestUri, string? logUri, string expectedPath)
    {
        // Arrange — unparseable body so the stub ProblemDetails path is taken (Instance = absolutePath)
        var (validator, _) = CreateValidator();
        var builder = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("not-a-json");
        if (requestUri != null)
        {
            builder.WithRequestUri(requestUri);
        }
        var response = builder.Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName, logUri: logUri);

        // Assert
        var assertion = await act.Should().ThrowAsync<ProxiedException>();
        assertion.Which.ProblemDetails.Instance.Should().Be(expectedPath);
    }

    [Fact]
    public async Task ValidateAsync_LargeResponseBody_TruncatesInLog()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var largePayload = new string('x', 8000);
        var body = $$"""{"status":400,"title":"X","detail":"{{largePayload}}"}""";
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        var entry = logger.Entries.First();
        entry.Message.Should().Contain("…[truncated");
        entry.Message.Should().Match(m => m.Contains("chars]") && !m.Contains(new string('x', 8000)));
    }

    [Fact]
    public async Task ValidateAsync_SmallResponseBody_LoggedFully()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X","detail":"short"}""")
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        var entry = logger.Entries.First();
        entry.Message.Should().Contain("short");
        entry.Message.Should().NotContain("truncated");
    }

    [Fact]
    public async Task ValidateAsync_TruncationLength_RespectsConfiguration_DoesNotTruncateBelowLimit()
    {
        // Arrange — MaxLoggedBodyLength=100, body length ~36 chars
        var (validator, logger) = CreateValidator(maxLoggedBodyLength: 100);
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody("""{"status":400,"title":"X","detail":"AAAA"}""")
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        logger.Entries.First().Message.Should().NotContain("truncated");
    }

    [Fact]
    public async Task ValidateAsync_TruncationLength_RespectsConfiguration_TruncatesAboveLimit()
    {
        // Arrange — MaxLoggedBodyLength=20, body length ~57 chars
        var (validator, logger) = CreateValidator(maxLoggedBodyLength: 20);
        var body = """{"status":400,"title":"X","detail":"ABCDEFGHIJKLMNOPQRSTUVWXYZ"}""";
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithJsonBody(body)
            .Build();

        // Act
        try
        {
            await validator.ValidateAsync(
                response,
                ClientName,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch
        {
            // ignored
        }

        // Assert
        logger.Entries.First().Message.Should().Contain("…[truncated");
    }

    [Fact]
    public async Task ValidateAsync_ContentReadCancels_PropagatesOperationCanceledExceptionWithoutLog()
    {
        // Arrange
        var (validator, logger) = CreateValidator();
        var response = HttpResponseBuilder
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithCancellingContent()
            .Build();

        // Act
        var act = () => validator.ValidateAsync(response, ClientName);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Entries.Should().BeEmpty();
    }

    private static (ProxiedResponseValidator Validator, FakeLogger Logger) CreateValidator(
        int? maxLoggedBodyLength = null)
    {
        var logger = new FakeLogger();
        var pairs = new Dictionary<string, string?>();
        if (maxLoggedBodyLength.HasValue)
        {
            pairs["Shared:ProxiedResponseValidatorSettings:MaxLoggedBodyLength"] =
                maxLoggedBodyLength.Value.ToString();
        }

        var config = pairs.Count == 0
            ? TestConfigurationBuilder.Empty()
            : TestConfigurationBuilder.WithSettings(pairs);
        return (
            new ProxiedResponseValidator(new FakeLogger<ProxiedResponseValidator>(logger), config),
            logger);
    }
}
