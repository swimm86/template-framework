using Shared.Domain.Core.Mapping.Extensions;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Domain.Core.Tests.Mapping.Extensions;

/// <summary>
/// Тесты для методов-расширений маппера, проверяющие вызовы Map, MapInPlace и ProjectTo.
/// </summary>
public sealed class MapperExtensionsTests
{
    private readonly FakeMapper _mapper = new();

    /// <summary>
    /// Проверяет, что метод-расширение Map вызывает метод Map маппера.
    /// </summary>
    [Fact]
    public void Map_SourceExtension_CallsMapperMap()
    {
        // Arrange
        _mapper.RegisterMap<string, int>(s => s.Length);
        var source = "hello";

        // Act
        var result = source.Map<string, int>(_mapper);

        // Assert
        result.Should().Be(5);
        _mapper.MapCallCount.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что метод-расширение MapInPlace вызывает соответствующий метод маппера.
    /// </summary>
    [Fact]
    public void MapInPlace_SourceExtension_CallsMapperMap()
    {
        // Arrange
        var source = "hello";
        var target = string.Empty;

        // Act
        source.Map(target, _mapper);

        // Assert
        _mapper.MapInPlaceCallCount.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что метод-расширение ProjectTo вызывает метод ProjectTo маппера.
    /// </summary>
    [Fact]
    public void ProjectTo_Extension_CallsMapperProjectTo()
    {
        // Arrange
        var source = new List<string>().AsQueryable();

        // Act
        var result = source.ProjectTo<string>(_mapper);

        // Assert
        _mapper.ProjectToCallCount.Should().Be(1);
    }
}
