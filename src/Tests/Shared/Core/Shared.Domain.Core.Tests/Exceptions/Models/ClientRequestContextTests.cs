using Shared.Domain.Core.Exceptions.Models;

namespace Shared.Domain.Core.Tests.Exceptions.Models;

/// <summary>
/// Модульные тесты для <see cref="ClientRequestContext"/>.
/// </summary>
public sealed class ClientRequestContextTests
{
    /// <summary>
    /// Проверяет, что конструктор <see cref="ClientRequestContext"/>
    /// корректно присваивает свойства <see cref="ClientRequestContext.ClientName"/>
    /// и <see cref="ClientRequestContext.AbsolutePath"/>.
    /// </summary>
    [Fact]
    public void Constructor_AssignsProperties()
    {
        // Arrange
        const string clientName = "test-client";
        const string absolutePath = "/api/v1/orders";

        // Act
        var context = new ClientRequestContext(clientName, absolutePath);

        // Assert
        context.ClientName.Should().Be(clientName);
        context.AbsolutePath.Should().Be(absolutePath);
    }

    /// <summary>
    /// Проверяет, что свойства <see cref="ClientRequestContext.ClientName"/>
    /// и <see cref="ClientRequestContext.AbsolutePath"/> доступны только для чтения
    /// (отсутствуют set-акцессоры).
    /// </summary>
    [Fact]
    public void Properties_AreGetOnly()
    {
        // Arrange
        var type = typeof(ClientRequestContext);

        // Act
        var clientNameProperty = type.GetProperty(nameof(ClientRequestContext.ClientName));
        var absolutePathProperty = type.GetProperty(nameof(ClientRequestContext.AbsolutePath));

        // Assert
        clientNameProperty.Should().NotBeNull();
        clientNameProperty!.CanWrite.Should().BeFalse();
        absolutePathProperty.Should().NotBeNull();
        absolutePathProperty!.CanWrite.Should().BeFalse();
    }
}
