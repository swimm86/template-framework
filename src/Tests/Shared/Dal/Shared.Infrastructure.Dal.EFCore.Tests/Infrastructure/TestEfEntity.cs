using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public class TestEfEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
}

public class TestParentEntity
    : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ChildId { get; set; }
    public TestChildEntity? Child { get; set; }
    public ICollection<TestChildEntity> Children { get; set; } = new List<TestChildEntity>();

    object IEntity.Id => Id;
}

public class TestChildEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public TestParentEntity? Parent { get; set; }
    public Guid? GrandChildId { get; set; }
    public TestGrandChildEntity? GrandChild { get; set; }
}

public class TestGrandChildEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
