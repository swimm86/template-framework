using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class TestEntityWithCreatedDeleted
    : IEntity<Guid>, IWithCreated, IWithDeleted, IWithUpdated
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime DateCreated { get; private set; }

    public DateTime? DateUpdated { get; private set; }

    public DateTime? DateDeleted { get; private set; }

    public bool IsDeleted { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public string? CreatedByUserName { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public string? UpdatedByUserName { get; private set; }

    public string[] RequiredToSaveNavigationPropertiesNames => [];

    public void SetDateCreated(DateTime dateCreated) => DateCreated = dateCreated;

    public void SetDateUpdated(DateTime? dateUpdated) => DateUpdated = dateUpdated;

    public void SetDateDeleted(DateTime? dateDeleted) => DateDeleted = dateDeleted;

    public void SetIsDeleted() => IsDeleted = true;

    public void SetCreatedByUserId(Guid? createdByUserId) => CreatedByUserId = createdByUserId;

    public void SetCreatedByUserName(string userName) => CreatedByUserName = userName;

    public void SetDeletedByUserId(Guid? deletedByUserId) => DeletedByUserId = deletedByUserId;

    public void SetUpdatedByUserId(Guid? updatedByUserId) => UpdatedByUserId = updatedByUserId;

    public void SetUpdatedByUserName(string userName) => UpdatedByUserName = userName;

    public void OnCreate(Guid? userId, string? userName)
    {
        DateCreated = DateTime.UtcNow;
        CreatedByUserId = userId;
        CreatedByUserName = userName;
    }

    public void OnDelete(Guid? userId)
    {
        IsDeleted = true;
        DateDeleted = DateTime.UtcNow;
        DeletedByUserId = userId;
    }

    public void OnUpdate(Guid? userId)
    {
        DateUpdated = DateTime.UtcNow;
        UpdatedByUserId = userId;
    }
}
