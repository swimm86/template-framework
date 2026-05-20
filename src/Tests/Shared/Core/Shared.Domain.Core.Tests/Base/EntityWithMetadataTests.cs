using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

/// <summary>
/// Тесты для сущности с метаданными, проверяющие корректность установки аудит-полей при создании, обновлении и удалении.
/// </summary>
public class EntityWithMetadataTests
{
    /// <summary>
    /// Проверяет, что при вызове OnCreate устанавливаются поля CreatedByUserId, CreatedByUserName и DateCreated.
    /// </summary>
    [Fact]
    public void OnCreate_SetsAuditFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "TestUser";

        var entity = new TestEntityWithMetadata();

        // Act
        entity.OnCreate(userId, userName);

        // Assert
        entity.CreatedByUserId.Should().Be(userId);
        entity.CreatedByUserName.Should().Be(userName);
        entity.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Проверяет, что при вызове OnUpdate устанавливаются поля UpdatedByUserId и DateUpdated.
    /// </summary>
    [Fact]
    public void OnUpdate_SetsUpdatedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var entity = new TestEntityWithMetadata();

        // Act
        entity.OnUpdate(userId);

        // Assert
        entity.UpdatedByUserId.Should().Be(userId);
        entity.DateUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Проверяет, что при вызове OnDelete устанавливаются поля DeletedByUserId и DateDeleted.
    /// </summary>
    [Fact]
    public void OnDelete_SetsDeleteFields()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var entity = new TestEntityWithMetadata();

        // Act
        entity.OnDelete(userId);

        // Assert
        entity.DeletedByUserId.Should().Be(userId);
        entity.DateDeleted.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Проверяет, что при повторной установке CreatedByUserId выбрасывается BusinessLogicException.
    /// </summary>
    [Fact]
    public void SetCreatedByUserId_WhenAlreadySet_ThrowsBusinessLogicException()
    {
        // Arrange
        var entity = new TestEntityWithMetadata();
        entity.SetCreatedByUserId(Guid.NewGuid());

        // Act & Assert
        var act = () => entity.SetCreatedByUserId(Guid.NewGuid());

        act.Should().Throw<BusinessLogicException>();
    }

    /// <summary>
    /// Проверяет, что при попытке изменить непустое CreatedByUserName выбрасывается BusinessLogicException.
    /// </summary>
    [Fact]
    public void SetCreatedByUserName_WhenChangingNonEmptyName_ThrowsBusinessLogicException()
    {
        // Arrange
        var entity = new TestEntityWithMetadata();
        entity.SetCreatedByUserName("User1");

        // Act & Assert
        var act = () => entity.SetCreatedByUserName("User2");

        act.Should().Throw<BusinessLogicException>();
    }
}
