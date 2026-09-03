// ----------------------------------------------------------------------------------------------
// <copyright file="ProjectToIntegrationTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentAssertions;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Xunit;

namespace Shared.Infrastructure.Mapper.Tests.Integration;

/// <summary>
/// Базовый класс интеграционных тестов <see cref="IMapper.ProjectTo{TResult}"/>
/// с реальной SQLite-БД. Общий для всех реализаций мапперов.
/// </summary>
/// <remarks>
/// Наследники обязаны реализовать <see cref="CreateMapper"/>, который возвращает
/// <see cref="IMapper"/> для конкретного провайдера. Общая логика (entity, DTO, seeding,
/// assertions) находится здесь и не дублируется между тестовыми проектами мапперов.
/// </remarks>
[Trait("Category", "Integration")]
public abstract class ProjectToIntegrationTestBase : SqliteIntegrationTestBase
{
    /// <summary>
    /// Проверяет, что ProjectTo без параметров выполняет SQL-запрос и возвращает проекцию всех записей.
    /// </summary>
    [Fact]
    public void ProjectTo_WithoutParameters_ReturnsAllRecords()
    {
        // Arrange
        using var context = CreateContext();
        Seed(context, 3);
        var queryable = context.Entities.AsQueryable();
        var mapper = CreateMapper(new ProjectToProfile());

        // Act
        var result = mapper.ProjectTo<ProjectToDest>(queryable).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().BeEquivalentTo("entity-00", "entity-01", "entity-02");
    }

    /// <summary>
    /// Проверяет, что ProjectTo с параметрами (словарём) выполняет SQL-запрос и применяет параметризованный MapFrom.
    /// </summary>
    [Fact]
    public void ProjectTo_WithDictionaryParameters_AppliesParameterizedMapFrom()
    {
        // Arrange
        using var context = CreateContext();
        Seed(context, 2);
        var queryable = context.Entities.AsQueryable();
        var mapper = CreateMapper(new ParameterizedProfile());
        var parameters = new Dictionary<string, object?> { ["prefix"] = "Hello" };

        // Act
        var result = mapper.ProjectTo<ParameterizedProjectToDest>(queryable, parameters).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Label.Should().StartWith("Hello-"));
    }

    /// <summary>
    /// Проверяет, что ProjectTo с анонимными параметрами выполняет SQL-запрос.
    /// </summary>
    [Fact]
    public void ProjectTo_WithAnonymousParameters_AppliesParameterizedMapFrom()
    {
        // Arrange
        using var context = CreateContext();
        Seed(context, 2);
        var queryable = context.Entities.AsQueryable();
        var mapper = CreateMapper(new ParameterizedProfile());

        // Act
        var result = mapper.ProjectTo<ParameterizedProjectToDest>(queryable, new { prefix = "anon" }).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Label.Should().StartWith("anon-"));
    }

    /// <summary>
    /// Проверяет, что ProjectTo с null-параметрами не бросает исключение.
    /// </summary>
    [Fact]
    public void ProjectTo_WithNullParameters_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        Seed(context, 1);
        var queryable = context.Entities.AsQueryable();
        var mapper = CreateMapper(new ParameterizedProfile());

        // Act
        var act = () => mapper.ProjectTo<ParameterizedProjectToDest>(queryable, parameters: null).ToList();

        // Assert — не должно бросать исключение
        act.Should().NotThrow();
        act().Should().HaveCount(1);
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="IMapper"/> для тестируемого провайдера.
    /// </summary>
    /// <param name="additionalProfile">
    /// Дополнительный провайдеро-независимый профиль маппинга для регистрации в маппере.
    /// </param>
    /// <returns>Готовый к использованию <see cref="IMapper"/>.</returns>
    protected abstract IMapper CreateMapper(MappingProfileBase additionalProfile);

    private static ProjectToTestEntity CreateEntity(Guid? id = null, string name = "test")
    {
        return new ProjectToTestEntity
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
        };
    }

    private static void Seed(ProjectToTestDbContext context, int count)
    {
        for (var i = 0; i < count; i++)
        {
            context.Entities.Add(CreateEntity(name: $"entity-{i:D2}"));
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Профиль со стандартным маппингом.
    /// </summary>
    protected sealed class ProjectToProfile : MappingProfileBase
    {
        public ProjectToProfile()
        {
            CreateMap<ProjectToTestEntity, ProjectToDest>();
        }
    }

    /// <summary>
    /// Профиль с параметризованным MapFrom.
    /// </summary>
    protected sealed class ParameterizedProfile : MappingProfileBase
    {
        public ParameterizedProfile()
        {
            CreateMap<ProjectToTestEntity, ParameterizedProjectToDest>()
                .ForMember(dest => dest.Label, opt => opt.MapFrom((src, @params) => $"{@params["prefix"]}-{src.Name}"));
        }
    }

    /// <summary>
    /// Конечный DTO с простыми свойствами.
    /// </summary>
    protected sealed class ProjectToDest
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Конечный DTO с параметризованным полем <see cref="Label"/>,
    /// которое формируется через <c>ForMember.MapFrom((src, @params) =&gt; ...)</c>.
    /// </summary>
    protected sealed class ParameterizedProjectToDest
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }
}
