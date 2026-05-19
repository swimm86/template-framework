using System.Collections.ObjectModel;
using AutoMapper;
using Shared.Domain.Core.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.Extensions;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests.Extensions;

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

    [Fact]
    public void ConfigureCollection_WithSourceAndDest_CreatesMapping()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTestProfile>());

        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void ConfigureCollection_MergeAdd_CallsAddItem()
    {
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

        mapper.Map(src, dest);

        dest.Should().HaveCount(2);
        dest.Should().ContainSingle(x => x.Id.Equals(1));
        dest.Should().ContainSingle(x => x.Id.Equals(2));
        dest.Single(x => x.Id.Equals(2)).Name.Should().Be("NewOne");
    }

    [Fact]
    public void ConfigureCollection_MergeUpdate_CallsUpdateItem()
    {
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

        mapper.Map(src, dest);

        dest.Should().HaveCount(1);
        dest.Single().Name.Should().Be("NewName");
    }

    [Fact]
    public void ConfigureCollection_MergeRemove()
    {
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

        mapper.Map(src, dest);

        dest.Should().HaveCount(1);
        dest.Single().Id.Should().Be(1);
    }
}
