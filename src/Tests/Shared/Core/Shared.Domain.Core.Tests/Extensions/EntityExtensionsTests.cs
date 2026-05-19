using Shared.Domain.Core.Extensions;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Extensions;

public sealed class EntityExtensionsTests
{
    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToAdd()
    {
        var src = new List<TestEntity> { new() { Name = "New" } };
        var dest = new List<TestEntity>();

        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        itemsToAdd.Should().ContainSingle().Which.Name.Should().Be("New");
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().BeEmpty();
    }

    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToRemove()
    {
        var existing = new TestEntity();
        var dest = new List<TestEntity> { existing };
        var src = new List<TestEntity>();

        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().ContainSingle().Which.Should().BeSameAs(existing);
        itemsToUpdate.Should().BeEmpty();
    }

    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToUpdate()
    {
        var srcEntity = new TestEntity();
        var destEntity = new TestEntity { Id = srcEntity.Id };
        var src = new List<TestEntity> { srcEntity };
        var dest = new List<TestEntity> { destEntity };

        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().ContainSingle()
            .Which.Should().Be((srcEntity, destEntity));
    }

    [Fact]
    public void GetDifferenceForMerge_EmptyCollections_ReturnsEmptyResults()
    {
        var src = Array.Empty<TestEntity>();
        var dest = Array.Empty<TestEntity>();

        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().BeEmpty();
    }

    [Fact]
    public void GetDifferenceForMerge_WithCustomSelectors()
    {
        var src = new List<TestEntity> { new() { Name = "A" } };
        var dest = new List<TestEntity> { new() { Name = "A" } };

        var (itemsToAdd, itemsToRemove, itemsToUpdate) =
            src.GetDifferenceForMerge(dest, sourceSelector: e => e.Name, destinationSelector: e => e.Name);

        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().ContainSingle();
    }
}
