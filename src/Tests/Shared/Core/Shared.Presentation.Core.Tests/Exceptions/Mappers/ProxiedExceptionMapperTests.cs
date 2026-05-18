using Microsoft.AspNetCore.Mvc;
using Shared.Application.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Тесты <see cref="ProxiedExceptionMapper"/>.
/// </summary>
public sealed class ProxiedExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа берётся из исключения.
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCodeFromException()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error" };
        var ex = new ProxiedException(pd, 403);
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Проверяет, что Title берётся из исходного ProblemDetails, а не из свойства маппера.
    /// </summary>
    [Fact]
    public void Handle_TitleFromSourceNotMapper()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error" };
        var ex = new ProxiedException(pd, 403);
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Title.Should().Be("Upstream Error");
    }

    /// <summary>
    /// Проверяет, что Details всегда null, даже при ShouldEnrichWithTrace=true в конфиге.
    /// </summary>
    [Fact]
    public void Handle_ShouldEnrichWithTrace_AlwaysFalse()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error" };
        var ex = new ProxiedException(pd, 403);
        var config = TestConfigurationBuilder.WithSettings(shouldEnrichWithTrace: true);
        var mapper = new ProxiedExceptionMapper(config);

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Details.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что Detail копируется из исходного ProblemDetails.
    /// </summary>
    [Fact]
    public void Handle_CopiesProblemDetailsDetail()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error", Detail = "D" };
        var ex = new ProxiedException(pd, 403);
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Detail.Should().Be("D");
    }

    /// <summary>
    /// Проверяет, что Instance, Type и Extensions копируются из исходного ProblemDetails.
    /// </summary>
    [Fact]
    public void Handle_CopiesProblemDetailsInstanceTypeAndExtensions()
    {
        // Arrange
        var pd = new ProblemDetails
        {
            Title = "Upstream Error",
            Instance = "/api/1",
            Type = "https://example.com/errors/test",
        };
        pd.Extensions["custom"] = "value";
        var ex = new ProxiedException(pd, 403);
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        var singleError = response.Errors.Should().ContainSingle().Subject;
        singleError.Instance.Should().Be("/api/1");
        singleError.Type.Should().Be("https://example.com/errors/test");
        singleError.Extensions["custom"].Should().Be("value");
    }

    /// <summary>
    /// Проверяет, что Status переопределяется статус-кодом исключения, а не исходным ProblemDetails.
    /// </summary>
    [Fact]
    public void Handle_OverridesProblemDetailsStatus_WithExceptionStatusCode()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error", Status = 200 };
        var ex = new ProxiedException(pd, 502);
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Status.Should().Be(502);
    }

    /// <summary>
    /// Проверяет, что AdditionalData из исключения пробрасывается в ответ.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var pd = new ProblemDetails { Title = "Upstream Error" };
        var ex = new ProxiedException(pd, 403, new Dictionary<string, object> { ["key"] = "val" });
        var mapper = new ProxiedExceptionMapper(TestConfigurationBuilder.Empty());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.AdditionalData.Should().NotBeNull()
            .And.ContainKey("key").WhoseValue.Should().Be("val");
    }
}
