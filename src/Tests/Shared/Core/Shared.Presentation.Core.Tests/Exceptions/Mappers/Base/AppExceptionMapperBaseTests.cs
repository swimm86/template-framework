using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Tests.Infrastructure;
using Shared.Presentation.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Presentation.Core.Tests.Exceptions.Mappers.Base;

/// <summary>
/// Модульные тесты для <see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.AppExceptionMapperBase{TException}"/>.
/// </summary>
public sealed class AppExceptionMapperBaseTests
{
    /// <summary>
    /// Проверяет, что <see cref="ErrorResponse.AdditionalData"/> содержит
    /// переданные из <see cref="Shared.Presentation.Core.Tests.Infrastructure.TestDoubles.TestAppException"/> дополнительные данные.
    /// </summary>
    [Fact]
    public void Handle_PassesAdditionalDataFromException()
    {
        // Arrange
        var mapper = new TestAppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestAppException(
            "x",
            new Dictionary<string, object> { ["k"] = "v" });

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().NotBeNull();
        response.AdditionalData.Should().ContainKey("k");
        response.AdditionalData!["k"].Should().Be("v");
    }

    /// <summary>
    /// Проверяет, что при отсутствии дополнительных данных в исключении
    /// <see cref="ErrorResponse.AdditionalData"/> равен <see langword="null"/>.
    /// </summary>
    [Fact]
    public void Handle_WithoutAdditionalData_ReturnsNull()
    {
        // Arrange
        var mapper = new TestAppExceptionMapper(TestConfigurationBuilder.Empty());
        var exception = new TestAppException("x");

        // Act
        var response = mapper.Handle(exception);

        // Assert
        response.AdditionalData.Should().BeNull();
    }

    /// <summary>
    /// Проверяет, что передача пустого словаря дополнительных данных
    /// вызывает <see cref="ArgumentException"/> — пустой словарь недопустим.
    /// </summary>
    [Fact]
    public void Handle_WithEmptyAdditionalData_ThrowsArgumentException()
    {
        // Arrange
        var act = () => new TestAppException(
            "x",
            new Dictionary<string, object>());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*empty dictionary*");
    }
}
