// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.ConvertersAndHooks.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты <c>ConvertUsing</c> (лямбда + <see cref="ITypeConverter{TSource, TDestination}"/>),
/// <c>BeforeMap</c>/<c>AfterMap</c>, <c>ReverseMap</c> и <c>IncludeBase</c>.
/// </summary>
public abstract partial class ProfileFeaturesTestBase
{
    // ---- 4. AfterMap / BeforeMap ----

    private sealed class Source4
    {
        public int Value { get; set; }
    }

    private sealed class Dest4
    {
        public int Value { get; set; }

        public int BeforeDelta { get; set; }

        public int AfterDelta { get; set; }
    }

    private sealed class BeforeAfterMapProfile
        : MappingProfileBase
    {
        public BeforeAfterMapProfile()
        {
            CreateMap<Source4, Dest4>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .BeforeMap((src, dest) => dest.BeforeDelta = src.Value * 10)
                .AfterMap((_, dest) => dest.AfterDelta = dest.Value + 100);
        }
    }

    /// <summary>
    /// Коллбэки <c>BeforeMap</c> и <c>AfterMap</c> вызываются соответственно до и после преобразования (mapping).
    /// </summary>
    [Fact]
    public void BeforeMap_AndAfterMap_AreInvoked()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Source4, Dest4>(new Source4 { Value = 3 });

        result.Value.Should().Be(3);
        result.BeforeDelta.Should().Be(30);
        result.AfterDelta.Should().Be(103);
    }

    // ---- 6. ConvertUsing с ITypeConverter ----

    private sealed class Source5
    {
        public int A { get; set; }

        public int B { get; set; }
    }

    private sealed class Dest5
    {
        public int Sum { get; set; }
    }

    private sealed class SumConverter
        : ITypeConverter<Source5, Dest5>
    {
        public Dest5 Convert(
            Source5 source,
            Dest5 destination,
            ResolutionContext context)
        {
            return new Dest5 { Sum = source.A + source.B };
        }
    }

    private sealed class ConverterProfile
        : MappingProfileBase
    {
        public ConverterProfile()
        {
            CreateMap<Source5, Dest5>().ConvertUsing(new SumConverter());
        }
    }

    /// <summary>
    /// <c>ConvertUsing</c> применяет кастомную реализацию <see cref="ITypeConverter{TInput, TOutput}"/>.
    /// </summary>
    [Fact]
    public void ConvertUsing_AppliesCustomITypeConverter()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Source5, Dest5>(new Source5 { A = 4, B = 6 });

        result.Sum.Should().Be(10);
    }

    // ---- 7. ConvertUsing с лямбдой ----

    private sealed class Source6
    {
        public int X { get; set; }
    }

    private sealed class Dest6
    {
        public int Y { get; set; }
    }

    private sealed class LambdaConvertProfile
        : MappingProfileBase
    {
        public LambdaConvertProfile()
        {
            CreateMap<Source6, Dest6>().ConvertUsing(src => new Dest6 { Y = src.X * 2 });
        }
    }

    /// <summary>
    /// <c>ConvertUsing</c> применяет лямбда-выражение для создания экземпляра назначения.
    /// </summary>
    [Fact]
    public void ConvertUsing_AppliesLambda()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Source6, Dest6>(new Source6 { X = 5 });

        result.Y.Should().Be(10);
    }

    // ---- 8. ReverseMap ----

    private sealed class Source7
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class Dest7
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class ReverseMapProfile
        : MappingProfileBase
    {
        public ReverseMapProfile()
        {
            CreateMap<Source7, Dest7>().ReverseMap();
        }
    }

    /// <summary>
    /// <c>ReverseMap</c> обеспечивает двунаправленное преобразование (mapping) между типами.
    /// </summary>
    [Fact]
    public void ReverseMap_AllowsBidirectionalMapping()
    {
        var mapper = CreateMapper();

        var forward = mapper.Map<Source7, Dest7>(new Source7 { Id = 1, Name = "F" });
        forward.Id.Should().Be(1);
        forward.Name.Should().Be("F");

        var backward = mapper.Map<Dest7, Source7>(new Dest7 { Id = 2, Name = "B" });
        backward.Id.Should().Be(2);
        backward.Name.Should().Be("B");
    }

    // ---- 8b. ReverseMap с собственными правилами на обратном направлении ----

    private sealed class FwdSrc
    {
        public int Value { get; set; }
    }

    private sealed class FwdDest
    {
        public int Value { get; set; }

        public int DoubledValue { get; set; }
    }

    private sealed class ReverseWithOwnRulesProfile
        : MappingProfileBase
    {
        public ReverseWithOwnRulesProfile()
        {
            CreateMap<FwdSrc, FwdDest>()
                .ForMember(d => d.DoubledValue, opt => opt.MapFrom(s => s.Value * 2))
                .ReverseMap()
                .ForMember(s => s.Value, opt => opt.MapFrom(d => d.DoubledValue));
        }
    }

    /// <summary>
    /// <c>ReverseMap</c> применяет собственные правила преобразования (mapping) в обратном направлении.
    /// </summary>
    [Fact]
    public void ReverseMap_AppliesOwnRulesOnReverseDirection()
    {
        var mapper = CreateMapper();

        var forward = mapper.Map<FwdSrc, FwdDest>(new FwdSrc { Value = 5 });
        forward.DoubledValue.Should().Be(10);

        var reverse = mapper.Map<FwdDest, FwdSrc>(new FwdDest { Value = 100, DoubledValue = 30 });
        reverse.Value.Should().Be(30);
    }

    // ---- 9. IncludeBase ----

    private class BaseSource
    {
        public int Id { get; set; }
    }

    private class BaseDest
    {
        public int Id { get; set; }
    }

    private class DerivedSource
        : BaseSource
    {
        public string Extra { get; set; } = string.Empty;
    }

    private class DerivedDest
        : BaseDest
    {
        public string Extra { get; set; } = string.Empty;
    }

    private sealed class IncludeBaseProfile
        : MappingProfileBase
    {
        public IncludeBaseProfile()
        {
            CreateMap<BaseSource, BaseDest>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id + 1));

            CreateMap<DerivedSource, DerivedDest>()
                .IncludeBase<BaseSource, BaseDest>()
                .ForMember(dest => dest.Extra, opt => opt.MapFrom(src => src.Extra + " new"));
        }
    }

    /// <summary>
    /// <c>IncludeBase</c> наследует базовое преобразование (mapping) для производных типов.
    /// </summary>
    [Fact]
    public void IncludeBase_InheritsBaseMapping()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<DerivedSource, DerivedDest>(new DerivedSource { Id = 7, Extra = "Hello" });

        result.Id.Should().Be(8);
        result.Extra.Should().Be("Hello new");
    }

    // ---- 10. ITypeConverter c вложенным Map ----

    private sealed class Inner
    {
        public int Value { get; set; }
    }

    private sealed class OuterSource
    {
        public int OuterId { get; set; }

        public Inner Inner { get; set; } = new();
    }

    private sealed class InnerDest
    {
        public int Value { get; set; }
    }

    private sealed class OuterDest
    {
        public int OuterId { get; set; }

        public InnerDest Inner { get; set; } = new();
    }

    private sealed class OuterConverter
        : ITypeConverter<OuterSource, OuterDest>
    {
        public OuterDest Convert(
            OuterSource source,
            OuterDest destination,
            ResolutionContext context)
        {
            return new OuterDest
            {
                OuterId = source.OuterId,
                Inner = context.Map<InnerDest>(source.Inner),
            };
        }
    }

    private sealed class OuterMappingProfile
        : MappingProfileBase
    {
        public OuterMappingProfile()
        {
            CreateMap<Inner, InnerDest>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value + 1));
            CreateMap<OuterSource, OuterDest>().ConvertUsing(new OuterConverter());
        }
    }

    /// <summary>
    /// <see cref="ITypeConverter{TInput, TOutput}"/> может использовать <c>ResolutionContext.Map</c> для вложенного преобразования (mapping).
    /// </summary>
    [Fact]
    public void ITypeConverter_CanUseContextMapForNestedMapping()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<OuterSource, OuterDest>(
            new OuterSource { OuterId = 10, Inner = new Inner { Value = 99 } });

        result.OuterId.Should().Be(10);
        result.Inner.Value.Should().Be(100);
    }
}
