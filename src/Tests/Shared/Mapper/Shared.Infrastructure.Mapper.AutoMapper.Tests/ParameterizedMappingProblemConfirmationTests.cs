// ----------------------------------------------------------------------------------------------
// <copyright file="ParameterizedMappingProblemConfirmationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Expression.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.AutoMapper.DependencyInjection;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

/// <summary>
/// Тесты, подтверждающие проблемы параметризованного <c>MapFrom((src, @params) =&gt; ...)</c>:
/// <list type="number">
///   <item>Для in-memory <see cref="IMapper.Map{TSource, TResult}(TSource)"/> поведение <c>@params</c> недокументировано
///   и фактически даёт пустой словарь — результат «маппинга» выглядит как ошибка без диагностики.</item>
///   <item>Доступ к отсутствующему ключу <c>@params["missing"]</c> возвращает <c>null</c> — без явного уведомления,
///   что приводит к скрытым NullReferenceException в SQL или неверным результатам.</item>
///   <item>Использование одинаковых параметризованных выражений в двух разных профилях приводит к рассинхронизации
///   реестра <c>ParameterKeyRegistry</c> (только Mapster): последний зарегистрированный профиль перезатирает ключи первого.</item>
/// </list>
/// <para>
/// Тесты намеренно ожидают сценарий, который должен быть исправлен или явно задокументирован:
/// сейчас они <b>падают</b>, что подтверждает наличие пробелов в текущей реализации.
/// </para>
/// </summary>
public sealed class ParameterizedMappingProblemConfirmationTests
{
    private sealed class ParamSource
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    private sealed class ParamDest
    {
        public int Id { get; init; }

        public string Computed { get; init; } = string.Empty;
    }

    /// <summary>
    /// Профиль с параметризованным <c>MapFrom</c>, использующим ключ <c>"prefix"</c>.
    /// </summary>
    private sealed class ProfileWithPrefix
        : MappingProfileBase
    {
        public ProfileWithPrefix()
        {
            CreateMap<ParamSource, ParamDest>()
                .ForMember(
                    dest => dest.Computed,
                    opt => opt.MapFrom((src, @params) => $"{@params["prefix"]}-{src.Name}"));
        }
    }

    /// <summary>
    /// Документированное (или ожидаемое) поведение: при in-memory <c>Map</c> без явных параметров
    /// параметризованный <c>MapFrom</c> должен либо:
    /// <list type="bullet">
    ///   <item>бросать <see cref="InvalidOperationException"/> с понятным сообщением,</item>
    ///   <item>либо явно документировать, что <c>@params</c> приходит пустым.</item>
    /// </list>
    /// Сейчас — пустой словарь, <c>@params["prefix"]</c> = <c>null</c>, результат начинается с <c>"-"</c>
    /// (silent degradation без диагностики).
    /// </summary>
    [Fact]
    public void InMemoryMap_WithoutParameters_ProducesAmbiguousResult_NotDocumentedBehavior()
    {
        var mapper = CreateMapper();
        var source = new ParamSource { Id = 1, Name = "Alice" };

        var result = mapper.Map<ParamSource, ParamDest>(source);

        // Текущее поведение: prefix = null → результат начинается с "-"
        result.Computed.Should().Be("Alice");
        result.Computed.Should().NotStartWith("-",
            "in-memory Map без параметров должен либо упасть, либо вернуть только значение src, "
            + "но не деградировать silent с null-prefix");
    }

    [Fact]
    public void MissingKey_Access_ReturnsNull_WithoutDiagnostic()
    {
        var mapper = CreateMapper();
        var source = new ParamSource { Id = 2, Name = "Bob" };
        var parameters = new Dictionary<string, object?>
        {
            ["otherKey"] = "value",
            // "prefix" — отсутствует
        };

        var query = new[] { source }.AsQueryable();
        var result = mapper.ProjectTo<ParamDest>(query, parameters).ToArray();

        // Сейчас: @params["prefix"] = null → результат "-Bob"
        // Ожидаем: явное исключение или документированная семантика
        result.Should().HaveCount(1);
        result[0].Computed.Should().NotStartWith("-",
            "обращение к отсутствующему ключу должно быть диагностируемым — "
            + "сейчас silent null приводит к некорректному результату в SQL-проекции");
    }

    [Fact]
    public void ParameterizedExpression_RequiresExplicitProjectToUsage_DocumentedInXmlDoc()
    {
        var assembly = typeof(IMemberConfigurationExpression<,,>).Assembly;
        var xmlFile = assembly.GetName().Name + ".xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        File.Exists(xmlPath).Should().BeTrue(
            $"XML-doc файл {xmlFile} должен существовать (GenerateDocumentationFile=true)");

        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.Load(xmlPath);

        var nodes = xmlDoc.SelectNodes("/doc/members/member[contains(@name, 'IMemberConfigurationExpression')]/summary");
        nodes.Should().NotBeNull();
        nodes!.Count.Should().BeGreaterThan(0);

        var foundProjectToMention = false;
        foreach (System.Xml.XmlNode node in nodes)
        {
            if (node.InnerText.Contains("ProjectTo", StringComparison.OrdinalIgnoreCase)
                || node.InnerXml.Contains("ProjectTo", StringComparison.OrdinalIgnoreCase))
            {
                foundProjectToMention = true;
                break;
            }
        }

        foundProjectToMention.Should().BeTrue(
            "XML-doc параметризованного MapFrom должен явно упоминать ProjectTo — иначе использование "
            + "в in-memory сценарии silent degradation без диагностики");
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        injector.Inject(services);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
