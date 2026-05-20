using System.Collections.ObjectModel;
using AutoMapper;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Extensions;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests.Extensions;

/// <summary>
/// Тесты для методов расширения <see cref="Profile"/>, конфигурирующих маппинг коллекций.
/// </summary>
public sealed class ProfileExtensionsTests
{
    private sealed class SrcEntity : IEntity
    {
        public object Id { get; set; } = default!;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DestEntity : IEntity
    {
        public object Id { get; set; } = default!;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CollectionTestProfile : Profile
    {
        public CollectionTestProfile()
        {
            CreateMap<SrcEntity, DestEntity>();
            this.ConfigureCollection<SrcEntity, DestEntity>();
        }
    }

    /// <summary>
    /// Проверяет, что <see cref="MapperConfiguration"/>
    /// создаёт валидный маппинг между исходным и целевым типами.
    /// </summary>
    [Fact]
    public void ConfigureCollection_WithSourceAndDest_CreatesMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTestProfile>());

        // Act
        var act = () => config.AssertConfigurationIsValid();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что при слиянии коллекций (Merge) новые элементы из источника
    /// добавляются в целевую коллекцию (вызывается AddItem).
    /// </summary>
    [Fact]
    public void ConfigureCollection_MergeAdd_CallsAddItem()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTestProfile>());
        var mapper = config.CreateMapper();
        var dest = new Collection<DestEntity>
        {
            new() { Id = 1, Name = "Existing" },
        };
        var src = new Collection<SrcEntity>
        {
            new() { Id = 1, Name = "Existing" },
            new() { Id = 2, Name = "NewOne" },
        };

        // Act
        mapper.Map(src, dest);

        // Assert
        dest.Should().HaveCount(2);
        dest.Should().ContainSingle(x => x.Id.Equals(1));
        dest.Should().ContainSingle(x => x.Id.Equals(2));
        dest.Single(x => x.Id.Equals(2)).Name.Should().Be("NewOne");
    }

    /// <summary>
    /// Проверяет, что при слиянии коллекций (Merge) существующие элементы
    /// обновляются данными из источника (вызывается UpdateItem).
    /// </summary>
    [Fact]
    public void ConfigureCollection_MergeUpdate_CallsUpdateItem()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTestProfile>());
        var mapper = config.CreateMapper();
        var dest = new Collection<DestEntity>
        {
            new() { Id = 1, Name = "OldName" },
        };
        var src = new Collection<SrcEntity>
        {
            new() { Id = 1, Name = "NewName" },
        };

        // Act
        mapper.Map(src, dest);

        // Assert
        dest.Should().HaveCount(1);
        dest.Single().Name.Should().Be("NewName");
    }

    /// <summary>
    /// Проверяет, что при слиянии коллекций (Merge) элементы, отсутствующие в источнике,
    /// удаляются из целевой коллекции.
    /// </summary>
    [Fact]
    public void ConfigureCollection_MergeRemove()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTestProfile>());
        var mapper = config.CreateMapper();
        var dest = new Collection<DestEntity>
        {
            new() { Id = 1, Name = "Keep" },
            new() { Id = 2, Name = "RemoveMe" },
        };
        var src = new Collection<SrcEntity>
        {
            new() { Id = 1, Name = "Keep" },
        };

        // Act
        mapper.Map(src, dest);

        // Assert
        dest.Should().HaveCount(1);
        dest.Single().Id.Should().Be(1);
    }
}
