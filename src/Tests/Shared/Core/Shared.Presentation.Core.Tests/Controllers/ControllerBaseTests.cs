// ----------------------------------------------------------------------------------------------
// <copyright file="ControllerBaseTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Dto.Responses;
using Shared.Testing.Doubles.Infrastructure;
using Shared.Testing.Doubles.Logging;

namespace Shared.Presentation.Core.Tests.Controllers;

/// <summary>
/// Тесты <see cref="Shared.Presentation.Core.Controllers.ControllerBase"/>.
/// Проверяют логику <see cref="Shared.Presentation.Core.Controllers.ControllerBase.Process{TResponse}"/>:
/// маппинг <see cref="Response.StatusCode"/> в HTTP-статус, корректность
/// категории логгера, проброс исключений.
/// </summary>
public sealed class ControllerBaseTests
{
    private sealed class TestController(ILoggerFactory loggerFactory)
        : Shared.Presentation.Core.Controllers.ControllerBase(loggerFactory)
    {
        public Task<IActionResult> SuccessfulEndpoint() =>
            Process(() => Task.FromResult(new TestResponse(StatusCodes.Status200OK)));

        public Task<IActionResult> CreatedEndpoint() =>
            Process(() => Task.FromResult(new TestResponse(StatusCodes.Status201Created)));

        public Task<IActionResult> ThrowingEndpoint() =>
            Process<Response>(() => throw new InvalidOperationException("boom"));

        public Func<Task<IActionResult>> SuccessfulNoArgEndpointFactory() =>
            SuccessfulEndpoint;
    }

    private sealed record TestResponse : Response
    {
        public TestResponse()
        {
        }

        public TestResponse(int statusCode)
            : base(statusCode)
        {
        }
    }

    /// <summary>
    /// <see cref="Process"/> возвращает HTTP-ответ
    /// со статус-кодом из <see cref="Response.StatusCode"/>.
    /// </summary>
    [Fact]
    public async Task Process_WithResponse_ReturnsStatusCodeFromResponse()
    {
        // Arrange
        var sut = new TestController(new FakeLoggerFactory());

        // Act
        var result = await sut.SuccessfulEndpoint();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// <see cref="Process"/> сохраняет
    /// статус-код <c>201 Created</c>.
    /// </summary>
    [Fact]
    public async Task Process_WithCreated_Returns201()
    {
        // Arrange
        var sut = new TestController(new FakeLoggerFactory());

        // Act
        var result = await sut.CreatedEndpoint();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Тело ответа — это исходный <see cref="Response"/>.
    /// </summary>
    [Fact]
    public async Task Process_WithResponse_ReturnsResponseAsBody()
    {
        // Arrange
        var sut = new TestController(new FakeLoggerFactory());

        // Act
        var result = await sut.SuccessfulEndpoint();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<TestResponse>();
    }

    /// <summary>
    /// При исключении внутри <see cref="Process"/>
    /// исключение пробрасывается наружу после логирования.
    /// </summary>
    [Fact]
    public async Task Process_WhenActionThrows_PropagatesException()
    {
        // Arrange
        var sut = new TestController(new FakeLoggerFactory());

        // Act
        var act = () => sut.ThrowingEndpoint();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");
    }

    /// <summary>
    /// Категория логгера соответствует конкретному типу
    /// (<see cref="TestController"/>), а не базовому <see cref="ControllerBase"/>.
    /// </summary>
    [Fact]
    public async Task Process_CreatesLoggerForConcreteType()
    {
        // Arrange
        var factory = new CapturingLoggerFactory();
        var sut = new TestController(factory);

        // Act
        await sut.SuccessfulEndpoint();

        // Assert
        factory.Categories.Should().ContainSingle()
            .Which.Should().EndWith(".TestController");
    }
}
