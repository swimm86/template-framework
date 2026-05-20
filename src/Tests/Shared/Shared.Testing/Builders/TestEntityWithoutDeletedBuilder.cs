using Shared.Testing.Entities;

namespace Shared.Testing.Builders;

public static class TestEntityWithoutDeletedBuilder
{
    public static TestEntityWithoutDeleted Valid(
        Guid? id = null,
        string? name = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "test-entity",
            Description = null,
            CreatedByUserId = null,
        };

    public static TestEntityWithoutDeleted WithAudit(
        DateTime? createdDate = null,
        Guid? createdBy = null) =>
        WithAudit(Valid(), createdDate, createdBy);

    public static TestEntityWithoutDeleted WithAudit(
        TestEntityWithoutDeleted entity,
        DateTime? createdDate = null,
        Guid? createdBy = null)
    {
        entity.SetDateCreated(createdDate ?? DateTime.UtcNow);
        entity.CreatedByUserId = createdBy ?? Guid.NewGuid();
        return entity;
    }

    public static TestEntityWithoutDeleted WithName(string name) => Valid(name: name);
}
