// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.Projections.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты <c>ProjectTo</c> с параметризованным <c>MapFrom((src, @params) =&gt; ...)</c>
/// и различными источниками параметров (<see cref="Dictionary{TKey, TValue}"/>, анонимный объект, <see langword="null"/>).
/// </summary>
public abstract partial class ProfileFeaturesTestBase
{
    // ---- 14. Parameterized MapFrom -> ProjectTo ----

    private sealed class ParamSource13
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class Dest13
    {
        public int Id { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    private sealed class ParameterizedProjectProfile
        : MappingProfileBase
    {
        public ParameterizedProjectProfile()
        {
            CreateMap<ParamSource13, Dest13>()
                .ForMember(
                    dest => dest.Label,
                    opt => opt.MapFrom((src, @params) => $"{@params["prefix"]}-{src.Name}"));
        }
    }

    /// <summary>
    /// <c>ProjectTo</c> применяет параметризованный <c>MapFrom</c> со словарём параметров.
    /// </summary>
    [Fact]
    public void ProjectTo_ParameterizedMapFrom_AppliesWithDictionaryParameters()
    {
        var mapper = CreateMapper();
        var sources = new[]
        {
            new ParamSource13 { Id = 1, Name = "Alice" },
            new ParamSource13 { Id = 2, Name = "Bob" },
        }.AsQueryable();

        var parameters = new Dictionary<string, object?>
        {
            ["prefix"] = "user",
        };

        var result = mapper.ProjectTo<Dest13>(sources, parameters).ToArray();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Label.Should().Be("user-Alice");
        result[1].Id.Should().Be(2);
        result[1].Label.Should().Be("user-Bob");
    }

    /// <summary>
    /// <c>ProjectTo</c> применяет параметризованный <c>MapFrom</c> с анонимным объектом параметров.
    /// </summary>
    [Fact]
    public void ProjectTo_ParameterizedMapFrom_AppliesWithAnonymousParameters()
    {
        var mapper = CreateMapper();
        var sources = new[]
        {
            new ParamSource13 { Id = 1, Name = "Charlie" },
        }.AsQueryable();

        var result = mapper.ProjectTo<Dest13>(sources, new { prefix = "anon" }).ToArray();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].Label.Should().Be("anon-Charlie");
    }

    /// <summary>
    /// <c>ProjectTo</c> использует <see langword="null"/> в качестве словаря параметров, если параметры не переданы.
    /// </summary>
    [Fact]
    public void ProjectTo_ParameterizedMapFrom_FallsBackToNullWhenNoParameters()
    {
        var mapper = CreateMapper();
        var sources = new[]
        {
            new ParamSource13 { Id = 1, Name = "NullTest" },
        }.AsQueryable();

        var result = mapper.ProjectTo<Dest13>(sources, parameters: null).ToArray();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].Label.Should().Be("-NullTest");
    }
}
