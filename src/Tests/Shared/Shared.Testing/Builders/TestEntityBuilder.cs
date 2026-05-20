using Shared.Testing.Entities;

namespace Shared.Testing.Builders;

public static class TestEntityBuilder
{
    public static TestEntity Valid(Guid? id = null, string? name = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "test-entity",
            Description = null,
            CreatedByUserId = null,
        };

    public static TestEntity WithAudit(DateTime? createdDate = null, Guid? createdBy = null) =>
        WithAudit(Valid(), createdDate, createdBy);

    public static TestEntity WithAudit(TestEntity entity, DateTime? createdDate = null, Guid? createdBy = null)
    {
        entity.SetDateCreated(createdDate ?? DateTime.UtcNow);
        entity.CreatedByUserId = createdBy ?? Guid.NewGuid();
        return entity;
    }

    public static TestEntity Deleted(Guid? deletedBy = null)
    {
        var entity = Valid();
        entity.OnDelete(deletedBy ?? Guid.NewGuid());
        return entity;
    }

    public static TestEntity WithName(string name) => Valid(name: name);

    public static TestEntity WithoutOptionalFields() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
        };
}
