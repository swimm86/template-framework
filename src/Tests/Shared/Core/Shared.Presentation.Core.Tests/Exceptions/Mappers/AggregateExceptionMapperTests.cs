using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;
using Shared.Presentation.Core.Tests.Infrastructure.Stubs;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers;

/// <summary>
/// Тесты <see cref="AggregateExceptionMapper"/>.
/// </summary>
public sealed class AggregateExceptionMapperTests
{
    /// <summary>
    /// Проверяет, что статус-код ответа равен 500.
    /// </summary>
    [Fact]
    public void Handle_ReturnsStatusCode500()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(new ErrorResponse { Errors = [] });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException(new Exception("inner"));

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Проверяет fallback на базовый ProblemDetails при отсутствии внутренних исключений.
    /// </summary>
    [Fact]
    public void Handle_WithEmptyInnerExceptions_FallsBackToBaseProblemDetails()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(new ErrorResponse());
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException();

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Errors.Should().ContainSingle()
            .Which.Title.Should().Be("Ошибка сервера");
    }

    /// <summary>
    /// Проверяет делегирование маппинга единственного внутреннего исключения резолверу.
    /// </summary>
    [Fact]
    public void Handle_WithSingleInnerException_DelegatesToResolver()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => new ErrorResponse
        {
            StatusCode = 500,
            Errors = [new ProblemDetails { Status = 500 }],
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException(new Exception("inner"));

        // Act
        mapper.Handle(ex);

        // Assert
        resolver.CallCount.Should().Be(1);
    }

    /// <summary>
    /// Проверяет объединение ProblemDetails от нескольких внутренних исключений.
    /// </summary>
    [Fact]
    public void Handle_WithMultipleInnerExceptions_MergesAllProblemDetails()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => e switch
        {
            InvalidOperationException => new ErrorResponse
            {
                StatusCode = 404,
                Errors = [new ProblemDetails { Status = 404 }],
            },
            ArgumentException => new ErrorResponse
            {
                StatusCode = 422,
                Errors = [new ProblemDetails { Status = 422 }, new ProblemDetails { Status = 418 }],
            },
            _ => new ErrorResponse(),
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException(new InvalidOperationException(), new ArgumentException());

        // Act
        var response = mapper.Handle(ex);

        // Assert
        response.Errors.Should().HaveCount(3);
    }

    /// <summary>
    /// Проверяет, что вложенные AggregateException разворачиваются через Flatten.
    /// </summary>
    [Fact]
    public void Handle_FlattensNestedAggregateExceptions()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => new ErrorResponse
        {
            StatusCode = 500,
            Errors = [new ProblemDetails { Status = 500, Detail = e.Message }],
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var innerA = new Exception("A");
        var innerB = new Exception("B");
        var innerC = new Exception("C");
        var nested = new AggregateException(innerA, innerB);
        var ex = new AggregateException(nested, innerC);

        // Act
        mapper.Handle(ex);

        // Assert
        resolver.CallCount.Should().Be(3);
    }

    /// <summary>
    /// Проверяет, что резолвер лениво разрешается из ServiceProvider при первом вызове с непустым inner.
    /// </summary>
    [Fact]
    public void Handle_LazyResolver_FirstCall_ResolvesFromServiceProvider()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => new ErrorResponse
        {
            StatusCode = 500,
            Errors = [new ProblemDetails { Status = 500 }],
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException(new Exception("inner"));

        // Act
        mapper.Handle(ex);

        // Assert
        resolver.CallCount.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что при последующих вызовах резолвер используется повторно без пересоздания.
    /// </summary>
    [Fact]
    public void Handle_LazyResolver_SubsequentCalls_DoNotReResolve()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => new ErrorResponse
        {
            StatusCode = 500,
            Errors = [new ProblemDetails { Status = 500 }],
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex1 = new AggregateException(new Exception("first"));
        var ex2 = new AggregateException(new Exception("second"));

        // Act
        mapper.Handle(ex1);
        mapper.Handle(ex2);

        // Assert
        resolver.CallCount.Should().Be(2);
    }

    /// <summary>
    /// Проверяет, что при отсутствии внутренних исключений резолвер не запрашивается из ServiceProvider.
    /// </summary>
    [Fact]
    public void Handle_LazyResolver_NotResolvedWhenInnerExceptionsEmpty()
    {
        // Arrange
        var resolver = new StubExceptionMapperResolver(e => new ErrorResponse
        {
            StatusCode = 500,
            Errors = [new ProblemDetails { Status = 500 }],
        });
        var sp = new ServiceCollection().AddSingleton<IExceptionMapperResolver>(resolver).BuildServiceProvider();
        var mapper = new AggregateExceptionMapper(TestConfigurationBuilder.Empty(), sp);
        var ex = new AggregateException();

        // Act
        mapper.Handle(ex);

        // Assert
        resolver.CallCount.Should().Be(0);
    }
}
