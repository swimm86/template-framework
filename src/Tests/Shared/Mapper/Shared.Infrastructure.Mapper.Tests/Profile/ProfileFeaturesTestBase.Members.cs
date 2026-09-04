// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.Members.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты <c>ForMember</c>+<c>MapFrom</c> и <c>Ignore</c>.
/// </summary>
public abstract partial class ProfileFeaturesTestBase
{
    // ---- 1. ForMember + MapFrom ----

    private sealed class Source1
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }

    private sealed class Dest1
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
    }

    private sealed class ForMemberProfile
        : MappingProfileBase
    {
        public ForMemberProfile()
        {
            CreateMap<Source1, Dest1>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
        }
    }

    /// <summary>
    /// <c>ForMember</c> с <c>MapFrom</c> применяет выражение к полям источника.
    /// </summary>
    [Fact]
    public void ForMember_MapFrom_AppliesConcatenation()
    {
        var mapper = CreateMapper();
        var source = new Source1 { Id = 1, FirstName = "John", LastName = "Doe" };

        var result = mapper.Map<Source1, Dest1>(source);

        result.Id.Should().Be(1);
        result.FullName.Should().Be("John Doe");
    }

    // ---- 2. Ignore ----

    private sealed class Source2
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Ignored { get; set; } = string.Empty;
    }

    private sealed class Dest2
    {
        public int Id { get; set; }

        public string Name { get; set; } = "Default";

        public string Ignored { get; set; } = "ShouldRemain";
    }

    private sealed class IgnoreProfile
        : MappingProfileBase
    {
        public IgnoreProfile()
        {
            CreateMap<Source2, Dest2>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Ignored, opt => opt.Ignore());
        }
    }

    /// <summary>
    /// <c>ForMember.Ignore</c> сохраняет исходное значение свойства назначения.
    /// </summary>
    [Fact]
    public void Ignore_DoesNotOverwriteDestinationProperty()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Source2, Dest2>(new Source2 { Id = 5, Name = "X", Ignored = "NotToSet" });

        result.Id.Should().Be(5);
        result.Name.Should().Be("X");
        result.Ignored.Should().Be("ShouldRemain");
    }
}
