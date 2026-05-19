using Shared.Domain.Core.Base;
using Shared.Domain.Core.Interfaces;

namespace Shared.Testing.Entities;

public class TestEntityWithoutDeleted : BaseEntity<Guid>, IWithDateCreated, IWithUpdated
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime DateCreated { get; private set; }

    public DateTime? DateUpdated { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public void SetDateCreated(DateTime dateCreated) => DateCreated = dateCreated;

    public void SetDateUpdated(DateTime? dateUpdated) => DateUpdated = dateUpdated;

    public void SetUpdatedByUserId(Guid? updatedByUserId) => UpdatedByUserId = updatedByUserId;

    public void OnUpdate(Guid? userId) => UpdatedByUserId = userId;
}
