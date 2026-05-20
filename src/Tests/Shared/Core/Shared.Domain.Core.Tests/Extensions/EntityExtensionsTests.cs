using Shared.Domain.Core.Extensions;
using Shared.Testing.Entities;

namespace Shared.Domain.Core.Tests.Extensions;

/// <summary>
/// Тесты для метода расширения <see cref="EntityExtensions.GetDifferenceForMerge{TSource, TDestination}(IEnumerable{TSource}, IEnumerable{TDestination})"/> — определение разницы между source и destination коллекциями сущностей.
/// </summary>
public sealed class EntityExtensionsTests
{
    /// <summary>
    /// Проверяет, что сущности, присутствующие только в source, определяются как элементы для добавления (itemsToAdd).
    /// </summary>
    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToAdd()
    {
        // Arrange
        var src = new List<TestEntity> { new() { Name = "New" } };
        var dest = new List<TestEntity>();

        // Act
        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        // Assert
        itemsToAdd.Should().ContainSingle().Which.Name.Should().Be("New");
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что сущности, присутствующие только в destination, определяются как элементы для удаления (itemsToRemove).
    /// </summary>
    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToRemove()
    {
        // Arrange
        var existing = new TestEntity();
        var dest = new List<TestEntity> { existing };
        var src = new List<TestEntity>();

        // Act
        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        // Assert
        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().ContainSingle().Which.Should().BeSameAs(existing);
        itemsToUpdate.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что сущности с одинаковым Id, присутствующие в обеих коллекциях, определяются как элементы для обновления (itemsToUpdate).
    /// </summary>
    [Fact]
    public void GetDifferenceForMerge_IdentifiesItemsToUpdate()
    {
        // Arrange
        var srcEntity = new TestEntity();
        var destEntity = new TestEntity { Id = srcEntity.Id };
        var src = new List<TestEntity> { srcEntity };
        var dest = new List<TestEntity> { destEntity };

        // Act
        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        // Assert
        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().ContainSingle()
            .Which.Should().Be((srcEntity, destEntity));
    }

    /// <summary>
    /// Проверяет, что при пустых source и destination коллекциях все три результата — пустые.
    /// </summary>
    [Fact]
    public void GetDifferenceForMerge_EmptyCollections_ReturnsEmptyResults()
    {
        // Arrange
        var src = Array.Empty<TestEntity>();
        var dest = Array.Empty<TestEntity>();

        // Act
        var (itemsToAdd, itemsToRemove, itemsToUpdate) = src.GetDifferenceForMerge(dest);

        // Assert
        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что при указании кастомных селекторов <c>sourceSelector</c> и <c>destinationSelector</c> метод корректно сопоставляет сущности по заданным полям.
    /// </summary>
    [Fact]
    public void GetDifferenceForMerge_WithCustomSelectors()
    {
        // Arrange
        var src = new List<TestEntity> { new() { Name = "A" } };
        var dest = new List<TestEntity> { new() { Name = "A" } };

        // Act
        var (itemsToAdd, itemsToRemove, itemsToUpdate) =
            src.GetDifferenceForMerge(dest, sourceSelector: e => e.Name, destinationSelector: e => e.Name);

        // Assert
        itemsToAdd.Should().BeEmpty();
        itemsToRemove.Should().BeEmpty();
        itemsToUpdate.Should().ContainSingle();
    }
}
