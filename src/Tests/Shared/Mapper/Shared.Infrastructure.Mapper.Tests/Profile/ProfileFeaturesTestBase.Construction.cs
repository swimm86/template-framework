// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.Construction.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты <c>ConstructUsing</c> (конструктор + статическая фабрика) и <c>ForCtorParam</c>.
/// </summary>
public abstract partial class ProfileFeaturesTestBase
{
    // ---- 3. ConstructUsing ----

    private sealed class Source3
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }

    private sealed class Dest3
    {
        public string FullName { get; set; } = string.Empty;

        public Dest3(string fullName)
        {
            FullName = fullName;
        }
    }

    private sealed class ConstructUsingProfile
        : MappingProfileBase
    {
        public ConstructUsingProfile()
        {
            CreateMap<Source3, Dest3>()
                .ConstructUsing(src => new Dest3($"{src.FirstName} {src.LastName}"));
        }
    }

    /// <summary>
    /// <c>ConstructUsing</c> применяет фабрику для создания экземпляра назначения.
    /// </summary>
    [Fact]
    public void ConstructUsing_InvokesCustomFactory()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Source3, Dest3>(new Source3 { FirstName = "Alice", LastName = "Smith" });

        result.FullName.Should().Be("Alice Smith");
    }

    // ---- 3a. ConstructUsing с фабричным статическим методом (private set свойства) ----

    private sealed class FactorySource
    {
        public string Name { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;
    }

    /// <summary>
    /// Имитация доменной сущности с <c>private set</c>-свойствами, заполняемыми через фабричный метод.
    /// Воспроизводит сценарий <c>Person.Create(src.Name, src.Email)</c> из
    /// <c>Template.Setter.Application.Mapping.MapperProfile</c>.
    /// </summary>
    private sealed class FactoryEntity
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public static FactoryEntity Create(string name, string email) =>
            new() { Id = Guid.NewGuid(), Name = name, Email = email };
    }

    private sealed class FactoryConstructUsingProfile
        : MappingProfileBase
    {
        public FactoryConstructUsingProfile()
        {
            CreateMap<FactorySource, FactoryEntity>()
                .ConstructUsing(src => FactoryEntity.Create(src.Name, src.Email));
        }
    }

    /// <summary>
    /// <c>ConstructUsing</c> со статической фабрикой корректно создаёт сущность с <c>private set</c>-свойствами
    /// и НЕ перезаписывает их значения после фабрики (важно для AutoMapper/Mapster-конвенций,
    /// иначе будет <see cref="InvalidOperationException"/> при попытке записи в <c>private set</c>).
    /// </summary>
    [Fact]
    public void ConstructUsing_StaticFactory_PreservesPrivateSetProperties()
    {
        var mapper = CreateMapper();
        var source = new FactorySource { Name = "John", Email = "john@example.com" };

        var result = mapper.Map<FactorySource, FactoryEntity>(source);

        result.Name.Should().Be("John");
        result.Email.Should().Be("john@example.com");
        result.Id.Should().NotBe(Guid.Empty);
    }

    // ---- 5. ForCtorParam ----

    private sealed class CtorParamSource
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }
    }

    private sealed class CtorParamDest(
        int yearParam,
        int monthParam,
        int dayParam)
    {
        public int Year { get; set; } = yearParam;

        public int Month { get; set; } = monthParam;

        public int Day { get; set; } = dayParam;
    }

    private sealed class CtorParamPartialDest(
        int yearParam,
        int monthParam,
        int dayParam)
    {
        public int Year { get; set; } = yearParam;

        public int Month { get; set; } = monthParam;

        public int Day { get; set; } = dayParam;

        public string Tag { get; set; } = string.Empty;
    }

    private sealed class CtorParamProfile
        : MappingProfileBase
    {
        public CtorParamProfile()
        {
            CreateMap<CtorParamSource, CtorParamDest>()
                .ForCtorParam("yearParam", opt => opt.MapFrom(src => src.Year + 2000))
                .ForCtorParam("monthParam", opt => opt.MapFrom(src => src.Month + 1))
                .ForCtorParam("dayParam", opt => opt.MapFrom(src => src.Day + 2))
                .ForMember(dest => dest.Year, opt => opt.Ignore())
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.Day, opt => opt.Ignore());
        }
    }

    /// <summary>
    /// <c>ForCtorParam</c> с <c>MapFrom</c> применяет выражение к параметрам конструктора назначения.
    /// </summary>
    [Fact]
    public void ForCtorParam_AppliesMapFromForConstructorParameters()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<CtorParamSource, CtorParamDest>(
            new CtorParamSource { Year = 26, Month = 8, Day = 15 });

        result.Year.Should().Be(2026);
        result.Month.Should().Be(9);
        result.Day.Should().Be(17);
    }

    private sealed class CtorParamPartialProfile
        : MappingProfileBase
    {
        public CtorParamPartialProfile()
        {
            CreateMap<CtorParamSource, CtorParamPartialDest>()
                .ForMember(dest => dest.Tag, opt => opt.MapFrom(src => "Tag"))
                .ForCtorParam("yearParam", opt => opt.MapFrom(src => src.Year))
                .ForCtorParam("monthParam", opt => opt.MapFrom(src => src.Month))
                .ForCtorParam("dayParam", opt => opt.MapFrom(src => src.Day))
                .ForMember(dest => dest.Year, opt => opt.Ignore())
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.Day, opt => opt.Ignore());
        }
    }

    /// <summary>
    /// <c>ForCtorParam</c> в комбинации с <c>ForMember</c> корректно маппит параметры конструктора и свойства.
    /// </summary>
    [Fact]
    public void ForCtorParam_CombinedWithForMember()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<CtorParamSource, CtorParamPartialDest>(
            new CtorParamSource { Year = 2026, Month = 8, Day = 15 });

        result.Year.Should().Be(2026);
        result.Month.Should().Be(8);
        result.Day.Should().Be(15);
        result.Tag.Should().Be("Tag");
    }
}
