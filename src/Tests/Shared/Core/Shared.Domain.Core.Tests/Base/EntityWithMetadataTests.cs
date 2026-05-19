using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

namespace Shared.Domain.Core.Tests.Base;

public class EntityWithMetadataTests
{
    [Fact]
    public void OnCreate_SetsAuditFields()
    {
        var userId = Guid.NewGuid();
        var userName = "TestUser";

        var entity = new TestEntityWithMetadata();
        entity.OnCreate(userId, userName);

        entity.CreatedByUserId.Should().Be(userId);
        entity.CreatedByUserName.Should().Be(userName);
        entity.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OnUpdate_SetsUpdatedFields()
    {
        var userId = Guid.NewGuid();

        var entity = new TestEntityWithMetadata();
        entity.OnUpdate(userId);

        entity.UpdatedByUserId.Should().Be(userId);
        entity.DateUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OnDelete_SetsDeleteFields()
    {
        var userId = Guid.NewGuid();

        var entity = new TestEntityWithMetadata();
        entity.OnDelete(userId);

        entity.DeletedByUserId.Should().Be(userId);
        entity.DateDeleted.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetCreatedByUserId_WhenAlreadySet_ThrowsBusinessLogicException()
    {
        var entity = new TestEntityWithMetadata();
        entity.SetCreatedByUserId(Guid.NewGuid());

        var act = () => entity.SetCreatedByUserId(Guid.NewGuid());

        act.Should().Throw<BusinessLogicException>();
    }

    [Fact]
    public void SetCreatedByUserName_WhenChangingNonEmptyName_ThrowsBusinessLogicException()
    {
        // TODO BUG (#5): Inverted condition in SetCreatedByUserName.
        // The guard `!string.IsNullOrWhiteSpace(userName)` checks the new value
        // instead of `!string.IsNullOrWhiteSpace(CreatedByUserName)` (the existing value).
        // This causes SetCreatedByUserName("User1") to throw on the first call.
        // When fixed, this test should pass: first call succeeds, second call throws.

        var entity = new TestEntityWithMetadata();
        entity.SetCreatedByUserName("User1");

        var act = () => entity.SetCreatedByUserName("User2");

        act.Should().Throw<BusinessLogicException>();
    }
}
