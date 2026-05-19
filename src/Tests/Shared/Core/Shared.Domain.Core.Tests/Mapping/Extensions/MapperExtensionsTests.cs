using Shared.Domain.Core.Mapping.Extensions;
using Shared.Testing.Doubles.Mapping;

namespace Shared.Domain.Core.Tests.Mapping.Extensions;

public sealed class MapperExtensionsTests
{
    private readonly FakeMapper _mapper = new();

    [Fact]
    public void Map_SourceExtension_CallsMapperMap()
    {
        _mapper.RegisterMap<string, int>(s => s.Length);
        var source = "hello";

        var result = source.Map<string, int>(_mapper);

        result.Should().Be(5);
        _mapper.MapCallCount.Should().Be(1);
    }

    [Fact]
    public void MapInPlace_SourceExtension_CallsMapperMap()
    {
        var source = "hello";
        var target = string.Empty;

        source.Map(target, _mapper);

        _mapper.MapInPlaceCallCount.Should().Be(1);
    }

    [Fact]
    public void ProjectTo_Extension_CallsMapperProjectTo()
    {
        var source = new List<string>().AsQueryable();

        var result = source.ProjectTo<string>(_mapper);

        _mapper.ProjectToCallCount.Should().Be(1);
    }
}
