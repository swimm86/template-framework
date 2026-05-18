using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Application.Core.Dto.Responses;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Presentation.Core.Exceptions;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Mappers;
using Shared.Presentation.Core.Tests.Infrastructure;
using Shared.Presentation.Core.Tests.Infrastructure.Stubs;

namespace Shared.Presentation.Core.Tests.Exceptions;

/// <summary>
/// Тесты для <see cref="Shared.Presentation.Core.Exceptions.ExceptionMapperResolver"/> — проверка разрешения мапперов по иерархии типов исключений.
/// </summary>
public sealed class ExceptionMapperResolverTests
{
    /// <summary>
    /// Кастомное исключение для проверки глубокого наследования.
    /// </summary>
    private sealed class MyNotFoundException(string message)
        : NotFoundException(message);

    /// <summary>
    /// Проверяет, что при отсутствии маппера для <see cref="Exception"/> выбрасывается <see cref="InvalidOperationException"/>
    /// с упоминанием <see cref="Shared.Presentation.Core.Exceptions.Mappers.DefaultExceptionMapper"/>.
    /// </summary>
    [Fact]
    public void Constructor_WhenDefaultExceptionMapperMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var mappers = Enumerable.Empty<IExceptionMapper>();

        // Act
        var act = () => new ExceptionMapperResolver(mappers);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultExceptionMapper*");
    }

    /// <summary>
    /// Проверяет, что при дублирующихся мапперах для одного типа исключения выбрасывается <see cref="InvalidOperationException"/>
    /// с указанием имён обоих конфликтующих мапперов.
    /// </summary>
    [Fact]
    public void Constructor_WhenDuplicateMappersForSameType_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var response = new ErrorResponse
        {
            StatusCode = 404,
            Errors = [new ProblemDetails { Status = 404, Title = "Test" }],
        };
        var mappers = new IExceptionMapper[]
        {
            new StubExceptionMapper(typeof(NotFoundException), response),
            new NotFoundExceptionMapper(config),
        };

        // Act
        var act = () => new ExceptionMapperResolver(mappers);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Where(ex => ex.Message.Contains(nameof(NotFoundException), StringComparison.Ordinal)
                         && ex.Message.Contains(nameof(StubExceptionMapper), StringComparison.Ordinal)
                         && ex.Message.Contains(nameof(NotFoundExceptionMapper), StringComparison.Ordinal));
    }

    /// <summary>
    /// Проверяет, что при наличии валидного набора мапперов (включая DefaultExceptionMapper) конструктор успешно завершается.
    /// </summary>
    [Fact]
    public void Constructor_WithValidMappers_BuildsMap()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var mappers = new IExceptionMapper[]
        {
            new DefaultExceptionMapper(config),
            new NotFoundExceptionMapper(config),
        };

        // Act
        var act = () => new ExceptionMapperResolver(mappers);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что при точном совпадении типа исключения с <see cref="IExceptionMapper.HandledType"/>
    /// резолвер вызывает зарегистрированный маппер и возвращает ожидаемый статус 404.
    /// </summary>
    [Fact]
    public void Map_ExactTypeMatch_DelegatesToRegisteredMapper()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new DefaultExceptionMapper(config),
            new NotFoundExceptionMapper(config)
        ]);
        var exception = new NotFoundException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Проверяет, что при отсутствии точного маппера для типа исключения резолвер обходит цепочку базовых типов
    /// и находит маппер для <see cref="AppException"/>.
    /// </summary>
    [Fact]
    public void Map_NoExactMapper_WalksBaseTypeChain()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new AppExceptionMapper(config),
            new DefaultExceptionMapper(config),
        ]);
        var exception = new BusinessLogicException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Проверяет, что при наличии нескольких мапперов в иерархии типов резолвер выбирает самый производный
    /// (наиболее специфичный) зарегистрированный маппер.
    /// </summary>
    [Fact]
    public void Map_PrefersMostDerivedMapper_OverBaseRegistration()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new BusinessLogicExceptionMapper(config),
            new AppExceptionMapper(config),
            new DefaultExceptionMapper(config),
        ]);
        var exception = new BusinessLogicException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(422);
    }

    /// <summary>
    /// Проверяет, что для типа исключения, не имеющего специфичного маппера, резолвер использует
    /// <see cref="DefaultExceptionMapper"/> и возвращает статус 500.
    /// </summary>
    [Fact]
    public void Map_UnknownExceptionType_FallsBackToDefaultExceptionMapper()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new DefaultExceptionMapper(config),
        ]);
        var exception = new NullReferenceException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Проверяет, что порядок регистрации мапперов не влияет на результат —
    /// DefaultExceptionMapper зарегистрирован первым, но NotFoundExceptionMapper всё равно выбирается для <see cref="NotFoundException"/>.
    /// </summary>
    [Fact]
    public void Map_MapperRegistrationOrderDoesNotAffectResult()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new DefaultExceptionMapper(config),
            new NotFoundExceptionMapper(config),
        ]);
        var exception = new NotFoundException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Проверяет, что при глубокой цепочке наследования (<c>MyNotFoundException : NotFoundException : AppException : Exception</c>)
    /// резолвер находит маппер для наиболее производного зарегистрированного типа (<see cref="NotFoundException"/>).
    /// </summary>
    [Fact]
    public void Map_DeepInheritanceChain_PicksMostDerivedRegistered()
    {
        // Arrange
        var config = TestConfigurationBuilder.Empty();
        var resolver = new ExceptionMapperResolver([
            new NotFoundExceptionMapper(config),
            new DefaultExceptionMapper(config),
        ]);
        var exception = new MyNotFoundException("msg");

        // Act
        var response = resolver.Map(exception);

        // Assert
        response.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Проверяет, что экземпляр исключения, переданный в <see cref="Shared.Presentation.Core.Exceptions.ExceptionMapperResolver.Map"/>,
    /// передаётся мапперу ссылочно без изменений.
    /// </summary>
    [Fact]
    public void Map_DelegatesExceptionInstanceUnchanged()
    {
        // Arrange
        Exception? captured = null;
        var stub = new StubExceptionMapper(
            typeof(Exception),
            ex =>
            {
                captured = ex;
                return new ErrorResponse
                {
                    StatusCode = 500,
                    Errors = [new ProblemDetails { Status = 500, Title = "Test" }],
                };
            });
        var resolver = new ExceptionMapperResolver([stub]);
        var original = new InvalidOperationException("msg");

        // Act
        resolver.Map(original);

        // Assert
        captured.Should().BeSameAs(original);
    }
}
