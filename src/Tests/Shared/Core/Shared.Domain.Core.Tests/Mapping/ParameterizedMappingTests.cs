// ----------------------------------------------------------------------------------------------
// <copyright file="ParameterizedMappingTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Expression.Interfaces;

namespace Shared.Domain.Core.Tests.Mapping;

/// <summary>
/// Тесты параметризованной перегрузки
/// <see cref="IMemberConfigurationExpression{TSource,TDestination,TMember}"/>.MapFrom,
/// принимающей <see cref="IDictionary{TKey, TValue}"/> параметров.
/// </summary>
public sealed class ParameterizedMappingTests
{
    private sealed class ParamSource
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class ParamDest
    {
        public int Id { get; set; }

        public string Computed { get; set; } = string.Empty;
    }

    private sealed class ParameterizedProfile : MappingProfileBase
    {
        public ParameterizedProfile()
        {
            CreateMap<ParamSource, ParamDest>()
                .ForMember(
                    dest => dest.Computed,
                    opt => opt.MapFrom((src, @params) => $"{@params["prefix"]}-{src.Id}"));
        }
    }

    /// <summary>
    /// Проверяет, что параметризованная перегрузка <c>MapFrom</c> сохраняет выражение в <c>ParameterizedSourceExpression</c>.
    /// </summary>
    [Fact]
    public void MapFrom_ParameterizedOverload_StoresExpressionInDescription()
    {
        var profile = new ParameterizedProfile();
        var descriptions = profile.GetMapDescriptions();

        var description = descriptions.Single();
        description.SourceType.Should().Be(typeof(ParamSource));
        description.DestinationType.Should().Be(typeof(ParamDest));

        var member = description.Members.Single();
        member.DestinationMemberName.Should().Be(nameof(ParamDest.Computed));
        member.SourceExpression.Should().BeNull();
        member.ParameterizedSourceExpression.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что параметризованное выражение <c>MapFrom</c> компилируется и вычисляется с переданными параметрами.
    /// </summary>
    [Fact]
    public void MapFrom_ParameterizedExpression_CompilesAndEvaluatesWithParameters()
    {
        var profile = new ParameterizedProfile();
        var member = profile.GetMapDescriptions().Single().Members.Single();
        var expr = member.ParameterizedSourceExpression!;

        var compiled = (Func<ParamSource, IDictionary<string, object?>, string>)
            expr.Compile();

        var source = new ParamSource { Id = 42, Name = "Test" };
        var parameters = new Dictionary<string, object?> { ["prefix"] = "item" };

        var result = compiled(source, parameters);

        result.Should().Be("item-42");
    }

    /// <summary>
    /// Проверяет, что обычная перегрузка <c>MapFrom</c> сохраняет выражение только в <c>SourceExpression</c>.
    /// </summary>
    [Fact]
    public void MapFrom_RegularOverload_StoresExpressionSeparately()
    {
        var profile = new RegularMapFromProfile();
        var member = profile.GetMapDescriptions().Single().Members.Single();

        member.SourceExpression.Should().NotBeNull();
        member.ParameterizedSourceExpression.Should().BeNull();
    }

    private sealed class RegularMapFromProfile : MappingProfileBase
    {
        public RegularMapFromProfile()
        {
            CreateMap<ParamSource, ParamDest>()
                .ForMember(
                    dest => dest.Computed,
                    opt => opt.MapFrom(src => src.Name));
        }
    }

    /// <summary>
    /// Проверяет, что параметризованная перегрузка <c>MapFrom</c> перекрывает обычное выражение для того же свойства.
    /// </summary>
    [Fact]
    public void MapFrom_ParameterizedOverload_OverridesRegularExpression()
    {
        var profile = new MixedMapFromProfile();

        var descriptions = profile.GetMapDescriptions().ToList();
        descriptions.Should().HaveCount(2);

        var first = descriptions[0].Members.Single();
        first.SourceExpression.Should().NotBeNull();
        first.ParameterizedSourceExpression.Should().BeNull();

        var second = descriptions[1].Members.Single();
        second.SourceExpression.Should().BeNull();
        second.ParameterizedSourceExpression.Should().NotBeNull();
    }

    private sealed class MixedMapFromProfile : MappingProfileBase
    {
        public MixedMapFromProfile()
        {
            CreateMap<ParamSource, ParamDest>()
                .ForMember(
                    dest => dest.Computed,
                    opt => opt.MapFrom(src => src.Name));

            CreateMap<ParamSource, ParamDest>()
                .ForMember(
                    dest => dest.Computed,
                    opt => opt.MapFrom((src, @params) => $"{@params["prefix"]}-{src.Id}"));
        }
    }
}
