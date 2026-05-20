using AutoMapper;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

/// <summary>
/// Тесты для <see cref="Mapper"/> — обёртки над AutoMapper, реализующей <see cref="IMapper"/>.
/// </summary>
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

    /// <summary>
    /// Проверяет, что <see cref="Mapper.Map{TSource, TDestination}(TSource)"/> корректно
    /// делегирует маппинг простых типов в AutoMapper.
    /// </summary>
    [Fact]
    public void Map_SimpleTypes_DelegatesToAutoMapper()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var source = new Source { Id = 1, Name = "Test" };

        // Act
        var result = mapper.Map<Source, Destination>(source);

        // Assert
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    /// <summary>
    /// Проверяет, что <see cref="Mapper.Map{TSource, TDestination}(TSource, TDestination)"/>
    /// корректно маппит источник в существующий целевой объект.
    /// </summary>
    [Fact]
    public void Map_InPlace_DelegatesToAutoMapper()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var source = new Source { Id = 2, Name = "Updated" };
        var destination = new Destination { Id = 0, Name = "Original" };

        // Act
        mapper.Map(source, destination);

        // Assert
        destination.Id.Should().Be(2);
        destination.Name.Should().Be("Updated");
    }

    /// <summary>
    /// Проверяет, что <see cref="Mapper.ProjectTo{TDestination}(System.Linq.IQueryable)"/>
    /// корректно делегирует проекцию <see cref="IQueryable"/> в AutoMapper.
    /// </summary>
    [Fact]
    public void ProjectTo_DelegatesToAutoMapper()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var autoMapper = config.CreateMapper();
        var mapper = new Mapper(autoMapper);
        var sources = new[]
        {
            new Source { Id = 1, Name = "A" },
            new Source { Id = 2, Name = "B" },
        }.AsQueryable();

        // Act
        var result = mapper.ProjectTo<Destination>(sources).ToArray();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("A");
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("B");
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="Mapper"/> не выбрасывает исключение
    /// при передаче <c>null</c> вместо <see cref="AutoMapper.IMapper"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAutoMapper_DoesNotThrow()
    {
        // Arrange
        var act = () => new Mapper(null!);

        // Act & Assert
        act.Should().NotThrow();
    }
}
