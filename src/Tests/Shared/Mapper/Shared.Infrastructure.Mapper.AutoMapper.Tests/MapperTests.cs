using AutoMapper;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

public sealed class MapperTests
{
    private sealed class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestProfile : Profile
    {
        public TestProfile()
        {
            CreateMap<Source, Destination>();
        }
    }

    [Fact]
    public void Map_SimpleTypes_DelegatesToAutoMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Source, Destination>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void Map_InPlace_DelegatesToAutoMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var source = new Source { Id = 2, Name = "Updated" };
        var destination = new Destination { Id = 0, Name = "Original" };

        mapper.Map(source, destination);

        destination.Id.Should().Be(2);
        destination.Name.Should().Be("Updated");
    }

    [Fact]
    public void ProjectTo_DelegatesToAutoMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var sources = new[]
        {
            new Source { Id = 1, Name = "A" },
            new Source { Id = 2, Name = "B" },
        }.AsQueryable();

        var result = mapper.ProjectTo<Destination>(sources).ToArray();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("A");
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("B");
    }

    [Fact]
    public void Constructor_WithNullAutoMapper_DoesNotThrow()
    {
        var act = () => new Mapper(null!);

        act.Should().NotThrow();
    }
}
